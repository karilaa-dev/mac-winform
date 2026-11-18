using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Windows.Forms;

public class BindingContext : ICollection
{
    // Underlying lists are identity keys. Two lists with equal contents have independent currency.
    private readonly ConditionalWeakTable<object, Dictionary<string,BindingManagerBase>> managers = new();
    public BindingManagerBase this[object dataSource] => this[dataSource,string.Empty];
    public BindingManagerBase this[object dataSource,string? dataMember]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(dataSource);
            dataMember ??= string.Empty;
            var members = managers.GetOrCreateValue(dataSource);
            if (members.TryGetValue(dataMember,out var manager)) return manager;
            if (dataSource is BindingSource source && dataMember.Length == 0) manager = source.CurrencyManager;
            else manager = new CurrencyManager(new BindingSource(dataSource,dataMember));
            members.Add(dataMember,manager);
            return manager;
        }
    }
    public bool Contains(object dataSource) => Contains(dataSource,string.Empty);
    public bool Contains(object dataSource,string? dataMember) =>
        managers.TryGetValue(dataSource,out var members) && members.ContainsKey(dataMember ?? string.Empty);
    private BindingManagerBase[] Snapshot() => managers.SelectMany(pair => pair.Value.Values).ToArray();
    public bool IsReadOnly => false;
    int ICollection.Count => Snapshot().Length;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    void ICollection.CopyTo(Array array,int index) => Snapshot().CopyTo(array,index);
    IEnumerator IEnumerable.GetEnumerator() => Snapshot().GetEnumerator();
}

public abstract class BindingManagerBase
{
    public BindingsCollection Bindings { get; } = new();
    public abstract object? Current { get; }
    public abstract int Position { get; set; }
    public abstract int Count { get; }
    public abstract void AddNew();
    public abstract void RemoveAt(int index);
    public abstract void EndCurrentEdit();
    public abstract void CancelCurrentEdit();
    public virtual PropertyDescriptorCollection GetItemProperties() =>
        Current is object item ? TypeDescriptor.GetProperties(item) : new(null);
    public event EventHandler? PositionChanged, CurrentChanged, CurrentItemChanged;
    protected void RaisePositionChanged() => PositionChanged?.Invoke(this,EventArgs.Empty);
    protected void RaiseCurrentChanged() => CurrentChanged?.Invoke(this,EventArgs.Empty);
    protected void RaiseItemChanged() => CurrentItemChanged?.Invoke(this,EventArgs.Empty);
    public bool IsBindingSuspended { get; private set; }
    public virtual void SuspendBinding() => IsBindingSuspended = true;
    public virtual void ResumeBinding() { IsBindingSuspended = false; RaiseCurrentChanged(); }
}

public class CurrencyManager : BindingManagerBase
{
    readonly BindingSource source;
    internal CurrencyManager(BindingSource source)
    {
        this.source = source;
        source.PositionChanged += (_,_) => RaisePositionChanged();
        source.CurrentChanged += (_,_) => RaiseCurrentChanged();
        source.CurrentItemChanged += (_,_) => RaiseItemChanged();
    }
    public override object? Current => source.Current;
    public override int Position { get => source.Position; set => source.Position = value; }
    public override int Count => source.Count;
    public IList List => source.List;
    public override void AddNew() => source.AddNew();
    public override void RemoveAt(int index) => source.RemoveAt(index);
    public override void EndCurrentEdit() => source.EndEdit();
    internal bool CommitPending()
    {
        foreach (Binding binding in Bindings)
            if (!binding.CommitPending()) return false;
        return true;
    }
    public override void CancelCurrentEdit() => source.CancelEdit();
    public override PropertyDescriptorCollection GetItemProperties() => source.GetItemProperties(null);
    public void Refresh() => source.ResetBindings(false);
    public event ListChangedEventHandler? ListChanged
    {
        add => source.ListChanged += value;
        remove => source.ListChanged -= value;
    }
}

public partial class Control : IBindableComponent
{
    private ControlBindingsCollection? bindings;
    private BindingContext? localContext, fallbackContext;
    public event EventHandler? BindingContextChanged;
    public ControlBindingsCollection DataBindings
    {
        get
        {
            if (bindings != null) return bindings;
            bindings = new ControlBindingsCollection(this);
            Disposed += (_,_) => bindings.Clear();
            return bindings;
        }
    }
    public virtual BindingContext? BindingContext
    {
        get => localContext ?? Parent?.BindingContext ?? (fallbackContext ??= new BindingContext());
        set { if (value != localContext) { localContext = value; NotifyBindingContext(); } }
    }
    internal void NotifyBindingContext()
    {
        if (bindings != null)
            foreach (Binding binding in bindings) binding.AttachManager();
        OnBindingContextChanged(EventArgs.Empty);
        foreach (var child in Controls)
            if (child.localContext == null) child.NotifyBindingContext();
    }
    protected virtual void OnBindingContextChanged(EventArgs e) => BindingContextChanged?.Invoke(this,e);
}
