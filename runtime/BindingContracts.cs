// Adapted from dotnet/winforms v8.0.20. Licensed by the .NET Foundation under the MIT license.
using System.Collections;
using System.ComponentModel;

namespace System.Windows.Forms;

public class BaseCollection : MarshalByRefObject, ICollection
{

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public virtual int Count => List!.Count;

    public void CopyTo(Array ar, int index) => List!.CopyTo(ar, index);

    public IEnumerator GetEnumerator() => List!.GetEnumerator();

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public bool IsReadOnly => false;

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public bool IsSynchronized => false;

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public object SyncRoot => this;

    protected virtual ArrayList? List => null;
}

public readonly struct BindingMemberInfo : IEquatable<BindingMemberInfo>
{
    private readonly string? _dataList;
    private readonly string? _dataField;

    public BindingMemberInfo(string? dataMember)
    {
        dataMember ??= string.Empty;

        int lastDot = dataMember.LastIndexOf('.');
        if (lastDot != -1)
        {
            _dataList = dataMember.Substring(0, lastDot);
            _dataField = dataMember.Substring(lastDot + 1);
        }
        else
        {
            _dataList = string.Empty;
            _dataField = dataMember;
        }
    }

    public string BindingPath => _dataList ?? string.Empty;

    public string BindingField => _dataField ?? string.Empty;

    public string BindingMember
        => BindingPath.Length > 0
            ? $"{BindingPath}.{BindingField}"
            : BindingField;

    public override bool Equals(object? otherObject)
    {
        if (otherObject is not BindingMemberInfo otherMember)
        {
            return false;
        }

        return Equals(otherMember);
    }

    public bool Equals(BindingMemberInfo other)
        => string.Equals(BindingMember, other.BindingMember, StringComparison.OrdinalIgnoreCase);

    public static bool operator ==(BindingMemberInfo a, BindingMemberInfo b) => a.Equals(b);

    public static bool operator !=(BindingMemberInfo a, BindingMemberInfo b) => !a.Equals(b);

    public override int GetHashCode() => base.GetHashCode();
}

public class ConvertEventArgs : EventArgs
{
    public ConvertEventArgs(object? value, Type? desiredType)
    {
        Value = value;
        DesiredType = desiredType;
    }

    public object? Value { get; set; }

    public Type? DesiredType { get; }
}

public delegate void ConvertEventHandler(object? sender, ConvertEventArgs e);

public enum BindingCompleteContext
{

    ControlUpdate = 0,

    DataSourceUpdate = 1,
}

public enum BindingCompleteState
{

    Success = 0,

    DataError = 1,

    Exception = 2,
}

public class BindingCompleteEventArgs : CancelEventArgs
{

    public BindingCompleteEventArgs(
        Binding? binding,
        BindingCompleteState state,
        BindingCompleteContext context,
        string? errorText,
        Exception? exception,
        bool cancel)
        : base(cancel)
    {
        Binding = binding;
        BindingCompleteState = state;
        BindingCompleteContext = context;
        ErrorText = errorText ?? string.Empty;
        Exception = exception;
    }

    public BindingCompleteEventArgs(
        Binding? binding,
        BindingCompleteState state,
        BindingCompleteContext context,
        string? errorText,
        Exception? exception)
        : this(binding, state, context, errorText, exception, true)
    {
    }

    public BindingCompleteEventArgs(
        Binding? binding,
        BindingCompleteState state,
        BindingCompleteContext context,
        string? errorText)
        : this(binding, state, context, errorText, null, true)
    {
    }

    public BindingCompleteEventArgs(
        Binding? binding,
        BindingCompleteState state,
        BindingCompleteContext context)
        : this(binding, state, context, string.Empty, null, false)
    {
    }

    public Binding? Binding { get; }

    public BindingCompleteState BindingCompleteState { get; }

    public BindingCompleteContext BindingCompleteContext { get; }

    public string ErrorText { get; }

    public Exception? Exception { get; }
}

public delegate void BindingCompleteEventHandler(object? sender, BindingCompleteEventArgs e);

public enum ControlUpdateMode
{

    OnPropertyChanged = 0,

    Never = 1,
}

public enum DataSourceUpdateMode
{

    OnValidation = 0,

    OnPropertyChanged = 1,

    Never = 2,
}

public interface IBindableComponent : IComponent
{
    ControlBindingsCollection DataBindings { get; }
    BindingContext? BindingContext { get; set; }
}

[DefaultEvent(nameof(CollectionChanged))]
public class BindingsCollection : BaseCollection
{
    private List<Binding> _list = new();
    private CollectionChangeEventHandler? _onCollectionChanging;
    private CollectionChangeEventHandler? _onCollectionChanged;

    internal BindingsCollection()
    {
    }

    public override int Count => _list.Count;

    protected override ArrayList List => ArrayList.Adapter(_list);

    public Binding this[int index] => _list[index]!;

    protected internal void Add(Binding binding)
    {
        CollectionChangeEventArgs eventArgs = new(CollectionChangeAction.Add, binding);
        OnCollectionChanging(eventArgs);
        AddCore(binding);
        OnCollectionChanged(eventArgs);
    }

    protected virtual void AddCore(Binding dataBinding)
    {
        ArgumentNullException.ThrowIfNull(dataBinding);
        _list.Add(dataBinding);
    }

    public event CollectionChangeEventHandler? CollectionChanging
    {
        add => _onCollectionChanging += value;
        remove => _onCollectionChanging -= value;
    }

    public event CollectionChangeEventHandler? CollectionChanged
    {
        add => _onCollectionChanged += value;
        remove => _onCollectionChanged -= value;
    }

    protected internal void Clear()
    {
        CollectionChangeEventArgs eventArgs = new(CollectionChangeAction.Refresh, null);
        OnCollectionChanging(eventArgs);
        ClearCore();
        OnCollectionChanged(eventArgs);
    }

    protected virtual void ClearCore() => _list.Clear();

    protected virtual void OnCollectionChanging(CollectionChangeEventArgs e)
    {
        _onCollectionChanging?.Invoke(this, e);
    }

    protected virtual void OnCollectionChanged(CollectionChangeEventArgs ccevent)
    {
        _onCollectionChanged?.Invoke(this, ccevent);
    }

    protected internal void Remove(Binding binding)
    {
        CollectionChangeEventArgs eventArgs = new(CollectionChangeAction.Remove, binding);
        OnCollectionChanging(eventArgs);
        RemoveCore(binding);
        OnCollectionChanged(eventArgs);
    }

    protected internal void RemoveAt(int index) => Remove(this[index]);

    protected virtual void RemoveCore(Binding dataBinding) => _list.Remove(dataBinding);

    protected internal bool ShouldSerializeMyAll() => Count > 0;
}

[DefaultEvent(nameof(CollectionChanged))]

public class ControlBindingsCollection : BindingsCollection
{
    private readonly IBindableComponent _control;

    public ControlBindingsCollection(IBindableComponent control)
    {
        _control = control;
    }

    public IBindableComponent BindableComponent => _control;

    public Control? Control => _control as Control;

    public Binding? this[string propertyName]
    {
        get
        {
            foreach (Binding binding in this)
            {
                if (string.Equals(binding.PropertyName, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return binding;
                }
            }

            return null;
        }
    }

    public new void Add(Binding binding) => base.Add(binding);

    public Binding Add(string propertyName, object dataSource, string dataMember)
    {
        return Add(propertyName, dataSource, dataMember, false, DefaultDataSourceUpdateMode, null, string.Empty, null);
    }

    public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled)
    {
        return Add(propertyName, dataSource, dataMember, formattingEnabled, DefaultDataSourceUpdateMode, null, string.Empty, null);
    }

    public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled, DataSourceUpdateMode updateMode)
    {
        return Add(propertyName, dataSource, dataMember, formattingEnabled, updateMode, null, string.Empty, null);
    }

    public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled, DataSourceUpdateMode updateMode, object? nullValue)
    {
        return Add(propertyName, dataSource, dataMember, formattingEnabled, updateMode, nullValue, string.Empty, null);
    }

    public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled, DataSourceUpdateMode updateMode, object? nullValue, string formatString)
    {
        return Add(propertyName, dataSource, dataMember, formattingEnabled, updateMode, nullValue, formatString, null);
    }

    public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled, DataSourceUpdateMode updateMode, object? nullValue, string formatString, IFormatProvider? formatInfo)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        Binding binding = new(propertyName, dataSource, dataMember, formattingEnabled, updateMode, nullValue, formatString, formatInfo);
        Add(binding);
        return binding;
    }

    protected override void AddCore(Binding dataBinding)
    {
        ArgumentNullException.ThrowIfNull(dataBinding);

        if (dataBinding.BindableComponent == _control)
        {
            throw new ArgumentException("This binding is already in the collection.", nameof(dataBinding));
        }

        if (dataBinding.BindableComponent is not null)
        {
            throw new ArgumentException("The binding belongs to another component.", nameof(dataBinding));
        }

        // important to set prop first for error checking.
        CheckDuplicates(dataBinding);
        dataBinding.SetBindableComponent(_control);

        base.AddCore(dataBinding);
    }

    internal void CheckDuplicates(Binding binding)
    {
        System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(binding.PropertyName), "The caller should check for this.");

        for (int i = 0; i < Count; i++)
        {
            Binding current = this[i];
            if (binding != current && !string.IsNullOrEmpty(current.PropertyName) &&
                string.Equals(binding.PropertyName, current.PropertyName, StringComparison.InvariantCulture))
            {
                throw new ArgumentException("This property already has a binding.", nameof(binding));
            }
        }
    }

    public new void Clear() => base.Clear();

    protected override void ClearCore()
    {
        int numLinks = Count;
        for (int i = 0; i < numLinks; i++)
        {
            Binding dataBinding = this[i];
            dataBinding.SetBindableComponent(null);
        }

        base.ClearCore();
    }

    public DataSourceUpdateMode DefaultDataSourceUpdateMode { get; set; } = DataSourceUpdateMode.OnValidation;

    public new void Remove(Binding binding) => base.Remove(binding);

    public new void RemoveAt(int index) => base.RemoveAt(index);

    protected override void RemoveCore(Binding dataBinding)
    {
        ArgumentNullException.ThrowIfNull(dataBinding);

        if (dataBinding.BindableComponent != _control)
        {
            throw new ArgumentException("This binding belongs to another collection.", nameof(dataBinding));
        }

        dataBinding.SetBindableComponent(null);
        base.RemoveCore(dataBinding);
    }
}
