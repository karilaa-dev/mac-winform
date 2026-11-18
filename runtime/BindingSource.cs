using System.Collections;
using System.ComponentModel;

namespace System.Windows.Forms;

/// <summary>Adapts a list and maintains the current item shared by bound controls.</summary>
public class BindingSource : Component, IBindingListView, ITypedList, ISupportInitialize, ICancelAddNew
{
    private object? source;
    private string member = string.Empty;
    private IList list = new BindingList<object>();
    private int position = -1;
    private int pendingNew = -1;
    private bool initializing;
    private bool? allowNew;
    private BindingSource? parentSource;

    public BindingSource() { Connect(list); }
    public BindingSource(IContainer container) : this() { container.Add(this); }
    public BindingSource(object? dataSource, string dataMember)
    {
        source = dataSource;
        member = dataMember ?? string.Empty;
        Connect(ResolveList());
    }

    public event ListChangedEventHandler? ListChanged;
    public event AddingNewEventHandler? AddingNew;
    public event EventHandler? DataSourceChanged, DataMemberChanged, CurrentChanged, CurrentItemChanged, PositionChanged;
    public object? DataSource
    {
        get => source;
        set
        {
            if (ReferenceEquals(value, source)) return;
            for (object? parent = value; parent is BindingSource ancestor; parent = ancestor.DataSource)
                if (ReferenceEquals(ancestor, this)) throw new ArgumentException("A binding source cannot reference itself.");
            source = value;
            if (!initializing) Connect(ResolveList());
            DataSourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public string DataMember
    {
        get => member;
        set
        {
            value ??= string.Empty;
            if (member == value) return;
            member = value;
            if (!initializing) Connect(ResolveList());
            DataMemberChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private IList ResolveList()
    {
        object? data = source;
        if (member.Length != 0)
        {
            if (data is BindingSource parent) data = parent.Current;
            foreach (string segment in member.Split('.'))
            {
                if (data == null) break;
                if (data is System.Data.DataSet tables)
                    data = tables.Tables[segment] ?? throw new ArgumentException($"Unknown table '{segment}'.");
                else
                {
                    var property = TypeDescriptor.GetProperties(data).Find(segment,true)
                        ?? throw new ArgumentException($"Unknown list member '{segment}'.");
                    data = property.GetValue(data);
                }
            }
        }
        if (data is IListSource lists) data = lists.GetList();
        if (data is IList items) return items;
        if (data is Type itemType)
            return (IList)Activator.CreateInstance(typeof(BindingList<>).MakeGenericType(itemType))!;
        if (data is IEnumerable sequence and not string) return sequence.Cast<object>().ToList();
        return data == null ? new BindingList<object>() : new BindingList<object> { data };
    }
    private void ParentCurrentChanged(object? sender,EventArgs e) => Connect(ResolveList());
    private void Connect(IList items)
    {
        if (parentSource != null) parentSource.CurrentChanged -= ParentCurrentChanged;
        parentSource = member.Length > 0 ? source as BindingSource : null;
        if (parentSource != null) parentSource.CurrentChanged += ParentCurrentChanged;
        if (list is IBindingList previous) previous.ListChanged -= UnderlyingChanged;
        list = items;
        if (list is IBindingList binding) binding.ListChanged += UnderlyingChanged;
        pendingNew = -1;
        position = Count > 0 ? 0 : -1;
        ResetBindings(true);
        PositionChanged?.Invoke(this, EventArgs.Empty);
        CurrentChanged?.Invoke(this, EventArgs.Empty);
    }
    private CurrencyManager? manager;
    public string? Sort
    {
        get
        {
            if (SupportsAdvancedSorting)
                return string.Join(", ",SortDescriptions.Cast<ListSortDescription>().Select(s => s.PropertyDescriptor?.Name + (s.SortDirection == ListSortDirection.Descending ? " DESC" : " ASC")));
            return IsSorted ? SortProperty?.Name + (SortDirection == ListSortDirection.Descending ? " DESC" : " ASC") : null;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value)) { if (IsSorted) RemoveSort(); return; }
            var properties = GetItemProperties(null);
            var sorts = value.Split(',').Select(part =>
            {
                string field = part.Trim();
                bool desc = field.EndsWith(" DESC",StringComparison.OrdinalIgnoreCase);
                if (desc || field.EndsWith(" ASC",StringComparison.OrdinalIgnoreCase))
                    field = field[..field.LastIndexOf(' ')].TrimEnd();
                if (field.StartsWith('[') && field.EndsWith(']')) field = field[1..^1];
                var property = properties.Find(field,true) ?? throw new ArgumentException($"Unknown sort property '{field}'.");
                return new ListSortDescription(property,desc ? ListSortDirection.Descending : ListSortDirection.Ascending);
            }).ToArray();
            if (SupportsAdvancedSorting) ApplySort(new ListSortDescriptionCollection(sorts));
            else if (sorts.Length == 1) ApplySort(sorts[0].PropertyDescriptor!,sorts[0].SortDirection);
            else throw new NotSupportedException("The list does not support sorting by multiple columns.");
        }
    }
    public CurrencyManager CurrencyManager => manager ??= new CurrencyManager(this);
    public bool IsBindingSuspended => CurrencyManager.IsBindingSuspended;
    public void SuspendBinding() => CurrencyManager.SuspendBinding();
    public void ResumeBinding() => CurrencyManager.ResumeBinding();
    public IList List => list;
    public object? Current => position >= 0 && position < Count ? list[position] : null;
    public int Position
    {
        get => position;
        set
        {
            int next = Count == 0 ? -1 : Math.Clamp(value, 0, Count - 1);
            if (next == position || manager?.CommitPending() == false) return;
            EndEdit();
            position = next;
            PositionChanged?.Invoke(this, EventArgs.Empty);
            CurrentChanged?.Invoke(this, EventArgs.Empty);
            CurrentItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public void MoveFirst() => Position = 0;
    public void MoveLast() => Position = Count - 1;
    public void MovePrevious() => Position--;
    public void MoveNext() => Position++;
    public bool RaiseListChangedEvents { get; set; } = true;
    private void UnderlyingChanged(object? sender, ListChangedEventArgs e)
    {
        int oldPosition = position;
        if (e.ListChangedType == ListChangedType.ItemAdded && e.NewIndex <= position) position++;
        if (e.ListChangedType == ListChangedType.ItemDeleted && e.NewIndex < position) position--;
        position = Count == 0 ? -1 : Math.Clamp(position, 0, Count - 1);
        if (RaiseListChangedEvents) ListChanged?.Invoke(this,e);
        if (position != oldPosition) PositionChanged?.Invoke(this,EventArgs.Empty);
        if (position != oldPosition || (e.ListChangedType == ListChangedType.ItemDeleted && e.NewIndex == position))
            CurrentChanged?.Invoke(this,EventArgs.Empty);
        if (position != oldPosition || e.NewIndex == position || e.ListChangedType == ListChangedType.Reset)
            CurrentItemChanged?.Invoke(this,EventArgs.Empty);
    }
    private void NotifyForPlainList(ListChangedType kind, int index)
    {
        if (list is not IBindingList { SupportsChangeNotification: true })
            UnderlyingChanged(list, new ListChangedEventArgs(kind, index));
    }
    public void ResetBindings(bool metadataChanged)
    {
        if (metadataChanged) UnderlyingChanged(this, new(ListChangedType.PropertyDescriptorChanged, -1));
        UnderlyingChanged(this, new(ListChangedType.Reset, -1));
    }
    public void ResetItem(int index) => UnderlyingChanged(this, new(ListChangedType.ItemChanged, index));
    public void ResetCurrentItem() { if (Position >= 0) ResetItem(Position); }

    private Type? ItemType => list.GetType().GetInterfaces()
        .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>))?.GetGenericArguments()[0];
    public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[]? listAccessors)
    {
        if (list is ITypedList typed) return typed.GetItemProperties(listAccessors);
        if (listAccessors?.Length > 0) return TypeDescriptor.GetProperties(listAccessors[^1].PropertyType);
        return Count > 0 && list[0] is object sample ? TypeDescriptor.GetProperties(sample)
            : TypeDescriptor.GetProperties(ItemType ?? typeof(object));
    }
    public string GetListName(PropertyDescriptor[]? listAccessors) => list is ITypedList typed
        ? typed.GetListName(listAccessors) : (ItemType?.Name ?? string.Empty);

    public int Count => list.Count;
    public bool IsFixedSize => list.IsFixedSize;
    public bool IsReadOnly => list.IsReadOnly;
    public bool IsSynchronized => list.IsSynchronized;
    public object SyncRoot => list.SyncRoot;
    public bool AllowEdit => list is IBindingList b ? b.AllowEdit : true;
    public bool AllowRemove => list is IBindingList b ? b.AllowRemove : !IsReadOnly && !IsFixedSize;
    public bool AllowNew
    {
        get => allowNew ?? (list is IBindingList b ? b.AllowNew : !IsReadOnly && !IsFixedSize && ItemType?.GetConstructor(Type.EmptyTypes) != null);
        set => allowNew = value;
    }
    public void ResetAllowNew() => allowNew = null;
    public object? this[int index]
    {
        get => list[index];
        set { list[index] = value; NotifyForPlainList(ListChangedType.ItemChanged,index); }
    }
    public int Add(object? value)
    {
        int index = list.Add(value);
        NotifyForPlainList(ListChangedType.ItemAdded,index);
        return index;
    }
    public object AddNew()
    {
        EndEdit();
        var e = new AddingNewEventArgs();
        AddingNew?.Invoke(this,e);
        object result;
        if (e.NewObject != null) { result = e.NewObject; pendingNew = Add(result); }
        else if (!AllowNew) throw new InvalidOperationException("This binding list does not allow new items.");
        else if (list is IBindingList underlying)
        {
            result = underlying.AddNew()!;
            pendingNew = list.IndexOf(result);
        }
        else
        {
            result = Activator.CreateInstance(ItemType ?? typeof(object))!;
            pendingNew = Add(result);
        }
        if (position != pendingNew)
        {
            position = pendingNew;
            PositionChanged?.Invoke(this,EventArgs.Empty);
            CurrentChanged?.Invoke(this,EventArgs.Empty);
        }
        return result;
    }
    public void EndEdit()
    {
        if (Current is IEditableObject editable) editable.EndEdit();
        if (pendingNew >= 0) ((ICancelAddNew)this).EndNew(pendingNew);
    }
    public void CancelEdit()
    {
        if (Current is IEditableObject editable) editable.CancelEdit();
        if (pendingNew >= 0) ((ICancelAddNew)this).CancelNew(pendingNew);
        else ResetCurrentItem();
    }
    void ICancelAddNew.CancelNew(int index)
    {
        if (index != pendingNew || index < 0) return;
        pendingNew = -1;
        if (list is ICancelAddNew pending) pending.CancelNew(index);
        else RemoveAt(index);
    }
    void ICancelAddNew.EndNew(int index)
    {
        if (index != pendingNew || index < 0) return;
        if (list is ICancelAddNew pending) pending.EndNew(index);
        pendingNew = -1;
    }
    public void Clear() { list.Clear(); NotifyForPlainList(ListChangedType.Reset,-1); }
    public bool Contains(object? value) => list.Contains(value);
    public int IndexOf(object? value) => list.IndexOf(value);
    public void Insert(int index,object? value) { list.Insert(index,value); NotifyForPlainList(ListChangedType.ItemAdded,index); }
    public void Remove(object? value) { int i = IndexOf(value); if (i >= 0) RemoveAt(i); }
    public void RemoveAt(int index) { list.RemoveAt(index); NotifyForPlainList(ListChangedType.ItemDeleted,index); }
    public void RemoveCurrent() { if (Position >= 0) RemoveAt(Position); }
    public void CopyTo(Array array,int index) => list.CopyTo(array,index);
    public IEnumerator GetEnumerator() => list.GetEnumerator();

    private IBindingList Sortable => list as IBindingList ?? throw new NotSupportedException("The underlying list must support this operation.");
    private IBindingListView SortableView => list as IBindingListView ?? throw new NotSupportedException("The underlying list must support filtering or advanced sorting.");
    public bool SupportsChangeNotification => true;
    public bool SupportsSearching => list is IBindingList { SupportsSearching:true };
    public bool SupportsSorting => list is IBindingList { SupportsSorting:true };
    public bool SupportsAdvancedSorting => list is IBindingListView { SupportsAdvancedSorting:true };
    public bool SupportsFiltering => list is IBindingListView { SupportsFiltering:true };
    public bool IsSorted => list is IBindingList { IsSorted:true };
    public PropertyDescriptor? SortProperty => (list as IBindingList)?.SortProperty;
    public ListSortDirection SortDirection => (list as IBindingList)?.SortDirection ?? ListSortDirection.Ascending;
    public ListSortDescriptionCollection SortDescriptions => (list as IBindingListView)?.SortDescriptions ?? new();
    public string? Filter { get => (list as IBindingListView)?.Filter; set => SortableView.Filter = value; }
    public void ApplySort(PropertyDescriptor property,ListSortDirection direction) => Sortable.ApplySort(property,direction);
    public void ApplySort(ListSortDescriptionCollection sorts) => SortableView.ApplySort(sorts);
    public void RemoveSort() => Sortable.RemoveSort();
    public void RemoveFilter() => SortableView.RemoveFilter();
    public int Find(PropertyDescriptor property,object key) => Sortable.Find(property,key);
    public int Find(string propertyName,object key) => Find(GetItemProperties(null).Find(propertyName,true)
        ?? throw new ArgumentException($"Unknown property '{propertyName}'."),key);
    void IBindingList.AddIndex(PropertyDescriptor property) => (list as IBindingList)?.AddIndex(property);
    void IBindingList.RemoveIndex(PropertyDescriptor property) => (list as IBindingList)?.RemoveIndex(property);
    void ISupportInitialize.BeginInit() => initializing = true;
    void ISupportInitialize.EndInit() { initializing = false; Connect(ResolveList()); }
    protected override void Dispose(bool disposing)
    {
        if (disposing && list is IBindingList binding) binding.ListChanged -= UnderlyingChanged;
        if (disposing && parentSource != null) parentSource.CurrentChanged -= ParentCurrentChanged;
        base.Dispose(disposing);
    }
}
