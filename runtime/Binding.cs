using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace System.Windows.Forms;

public class Binding
{
    private PropertyDescriptor? targetProperty, sourceProperty;
    private object? sourceOwner;
    private bool transferring, dirty;
    private object? previousCurrent;
    private readonly List<Action> unsubscribePath = new();

    public Binding(string propertyName, object dataSource, string dataMember) : this(propertyName,dataSource,dataMember,false) {}
    public Binding(string propertyName, object dataSource, string dataMember,bool formattingEnabled)
        : this(propertyName,dataSource,dataMember,formattingEnabled,DataSourceUpdateMode.OnValidation) {}
    public Binding(string propertyName, object dataSource, string dataMember,bool formattingEnabled,DataSourceUpdateMode dataSourceUpdateMode)
        : this(propertyName,dataSource,dataMember,formattingEnabled,dataSourceUpdateMode,null) {}
    public Binding(string propertyName, object dataSource, string dataMember,bool formattingEnabled,DataSourceUpdateMode dataSourceUpdateMode,object? nullValue)
        : this(propertyName,dataSource,dataMember,formattingEnabled,dataSourceUpdateMode,nullValue,string.Empty) {}
    public Binding(string propertyName, object dataSource, string dataMember,bool formattingEnabled,DataSourceUpdateMode dataSourceUpdateMode,object? nullValue,string formatString)
        : this(propertyName,dataSource,dataMember,formattingEnabled,dataSourceUpdateMode,nullValue,formatString,null) {}
    public Binding(string propertyName,object dataSource,string dataMember,bool formattingEnabled,DataSourceUpdateMode dataSourceUpdateMode,object? nullValue,string formatString,IFormatProvider? formatInfo)
    {
        PropertyName = propertyName;
        DataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        BindingMemberInfo = new BindingMemberInfo(dataMember);
        FormattingEnabled = formattingEnabled;
        DataSourceUpdateMode = dataSourceUpdateMode;
        NullValue = nullValue;
        FormatString = formatString ?? string.Empty;
        FormatInfo = formatInfo;
    }

    public string PropertyName { get; }
    public object DataSource { get; }
    public BindingMemberInfo BindingMemberInfo { get; }
    public IBindableComponent? BindableComponent { get; private set; }
    public Control? Control => BindableComponent as Control;
    public BindingManagerBase? BindingManagerBase { get; private set; }
    public bool FormattingEnabled { get; set; }
    public IFormatProvider? FormatInfo { get; set; }
    public string FormatString { get; set; }
    public object? NullValue { get; set; }
    public object? DataSourceNullValue { get; set; } = DBNull.Value;
    public DataSourceUpdateMode DataSourceUpdateMode { get; set; }
    public ControlUpdateMode ControlUpdateMode { get; set; }
    public bool IsBinding => BindableComponent != null && BindingManagerBase is { IsBindingSuspended:false, Count:>0 };
    public event ConvertEventHandler? Format, Parse;
    public event BindingCompleteEventHandler? BindingComplete;
    protected virtual void OnFormat(ConvertEventArgs e) => Format?.Invoke(this,e);
    protected virtual void OnParse(ConvertEventArgs e) => Parse?.Invoke(this,e);
    protected virtual void OnBindingComplete(BindingCompleteEventArgs e) => BindingComplete?.Invoke(this,e);

    internal void SetBindableComponent(IBindableComponent? target)
    {
        if (BindableComponent != null)
        {
            targetProperty?.RemoveValueChanged(BindableComponent,TargetChanged);
            if (Control != null)
            {
                Control.Validating -= ValidateTarget;
                Control.ParentChanged -= ParentChanged;
            }
            DetachManager();
        }
        BindableComponent = target;
        if (target == null) return;
        try
        {
            targetProperty = Property(target,PropertyName);
            if (targetProperty.IsReadOnly) throw new ArgumentException($"Control property '{PropertyName}' is read-only.");
            targetProperty.AddValueChanged(target,TargetChanged);
            if (Control != null)
            {
                Control.Validating += ValidateTarget;
                Control.ParentChanged += ParentChanged;
            }
            AttachManager();
        }
        catch { SetBindableComponent(null); throw; }
    }
    private void ParentChanged(object? sender,EventArgs e) => AttachManager();
    internal void AttachManager()
    {
        var next = BindableComponent?.BindingContext?[DataSource];
        if (ReferenceEquals(next,BindingManagerBase)) return;
        DetachManager();
        BindingManagerBase = next;
        if (next == null) return;
        next.Bindings.Add(this);
        next.CurrentChanged += CurrentChanged;
        next.CurrentItemChanged += ItemChanged;
        ResolvePath();
        if (ControlUpdateMode != ControlUpdateMode.Never) ReadValue();
    }
    private void DetachManager()
    {
        if (BindingManagerBase is { } old)
        {
            old.Bindings.Remove(this);
            old.CurrentChanged -= CurrentChanged;
            old.CurrentItemChanged -= ItemChanged;
        }
        DetachPath();
        BindingManagerBase = null;
    }
    private static PropertyDescriptor Property(object target,string name) =>
        TypeDescriptor.GetProperties(target).Find(name,true) ?? throw new ArgumentException($"'{target.GetType().Name}' has no bindable property '{name}'.");
    private void DetachPath()
    {
        foreach (var unsubscribe in unsubscribePath) unsubscribe();
        unsubscribePath.Clear();
        sourceOwner = null;
        sourceProperty = null;
    }
    private void ResolvePath()
    {
        DetachPath();
        object? current = previousCurrent = BindingManagerBase?.Current;
        string path = BindingMemberInfo.BindingMember;
        if (path.Length == 0) { sourceOwner = current; return; }
        foreach (var part in path.Split('.'))
        {
            if (current == null) { sourceOwner = null; sourceProperty = null; return; }
            var owner = current;
            var property = Property(owner,part);
            if (owner is INotifyPropertyChanged notify)
            {
                PropertyChangedEventHandler handler = (s,e) =>
                {
                    if (string.IsNullOrEmpty(e.PropertyName) || string.Equals(e.PropertyName,property.Name,StringComparison.OrdinalIgnoreCase))
                        SourceChanged(s,e);
                };
                notify.PropertyChanged += handler;
                unsubscribePath.Add(() => notify.PropertyChanged -= handler);
            }
            else if (property.SupportsChangeEvents)
            {
                property.AddValueChanged(owner,SourceChanged);
                unsubscribePath.Add(() => property.RemoveValueChanged(owner,SourceChanged));
            }
            sourceOwner = owner;
            sourceProperty = property;
            current = property.GetValue(owner);
        }
    }
    internal bool CommitPending() => !dirty || DataSourceUpdateMode == DataSourceUpdateMode.Never || Transfer(toControl:false);
    private void CurrentChanged(object? sender,EventArgs e)
    {
        if (!ReferenceEquals(previousCurrent,BindingManagerBase?.Current)) dirty = false;
        ItemChanged(sender,e);
    }
    private void ItemChanged(object? sender,EventArgs e)
    {
        if (!dirty) SourceChanged(sender,e);
    }
    private void SourceChanged(object? sender,EventArgs e)
    {
        if (transferring || BindingManagerBase?.IsBindingSuspended == true) return;
        ResolvePath();
        if (ControlUpdateMode == ControlUpdateMode.OnPropertyChanged) ReadValue();
    }
    private void TargetChanged(object? sender,EventArgs e)
    {
        if (transferring || !IsBinding) return;
        dirty = true;
        if (DataSourceUpdateMode == DataSourceUpdateMode.OnPropertyChanged) Transfer(toControl:false);
    }
    private void ValidateTarget(object? sender,CancelEventArgs e)
    {
        if (dirty && DataSourceUpdateMode == DataSourceUpdateMode.OnValidation && !Transfer(toControl:false)) e.Cancel = true;
    }

    public void ReadValue() => Transfer(toControl:true);
    public void WriteValue() => Transfer(toControl:false);
    private bool Transfer(bool toControl)
    {
        if (transferring || BindableComponent == null || targetProperty == null || BindingManagerBase?.IsBindingSuspended == true)
            return true;
        var context = toControl ? BindingCompleteContext.ControlUpdate : BindingCompleteContext.DataSourceUpdate;
        transferring = true;
        try
        {
            if (toControl)
            {
                object? original = sourceProperty == null ? sourceOwner : sourceProperty.GetValue(sourceOwner);
                var args = new ConvertEventArgs(original,targetProperty.PropertyType);
                OnFormat(args);
                var value = args.Value == null || Equals(args.Value,DBNull.Value) ? NullValue : args.Value;
                if (FormattingEnabled && FormatString.Length > 0 && value is IFormattable formatted && targetProperty.PropertyType == typeof(string))
                    value = formatted.ToString(FormatString,FormatInfo);
                targetProperty.SetValue(BindableComponent,ConvertValue(value,targetProperty.PropertyType));
            }
            else if (sourceProperty != null && sourceOwner != null && !sourceProperty.IsReadOnly)
            {
                var args = new ConvertEventArgs(targetProperty.GetValue(BindableComponent),sourceProperty.PropertyType);
                OnParse(args);
                var value = args.Value;
                if (FormattingEnabled && Equals(value,NullValue)) value = DataSourceNullValue;
                sourceProperty.SetValue(sourceOwner,ConvertValue(value,sourceProperty.PropertyType));
            }
            dirty = false;
            if (FormattingEnabled) OnBindingComplete(new(this,BindingCompleteState.Success,context));
            return true;
        }
        catch (Exception error) when (FormattingEnabled)
        {
            var failed = new BindingCompleteEventArgs(this,BindingCompleteState.Exception,context,error.Message,error);
            OnBindingComplete(failed);
            return !failed.Cancel;
        }
        finally { transferring = false; }
    }
    private object? ConvertValue(object? value,Type target)
    {
        if (value != null && target.IsInstanceOfType(value)) return value;
        if (value == null || value == DBNull.Value)
        {
            if (target == typeof(string)) return string.Empty;
            return target.IsValueType && Nullable.GetUnderlyingType(target) == null ? Activator.CreateInstance(target) : null;
        }
        if (target == typeof(string)) return Convert.ToString(value,FormatInfo);
        var converter = TypeDescriptor.GetConverter(Nullable.GetUnderlyingType(target) ?? target);
        if (converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(null,FormatInfo as CultureInfo ?? CultureInfo.CurrentCulture,value);
        return Convert.ChangeType(value,Nullable.GetUnderlyingType(target) ?? target,FormatInfo);
    }
}
