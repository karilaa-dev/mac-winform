using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Drawing;

namespace System.Windows.Forms;

[Flags] public enum PropertySort { NoSort=0, Alphabetical=1, Categorized=2, CategorizedAlphabetical=3 }
public enum GridItemType { Property, Category, ArrayValue, Root }
public sealed class GridItemCollection : ICollection
{
    public static readonly GridItemCollection Empty = new(Array.Empty<GridItem>());
    private readonly GridItem[] items;
    internal GridItemCollection(IEnumerable<GridItem> children) => items=children.ToArray();
    public int Count => items.Length;
    public GridItem this[int index] => items[index];
    public GridItem? this[string label] => Array.Find(items,i=>i.Label==label);
    public IEnumerator GetEnumerator() => items.GetEnumerator();
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => items;
    void ICollection.CopyTo(Array array,int index) => items.CopyTo(array,index);
}
public abstract class GridItem
{
    public abstract GridItemCollection GridItems { get; }
    public abstract GridItemType GridItemType { get; }
    public abstract string Label { get; }
    public abstract GridItem? Parent { get; }
    public abstract PropertyDescriptor? PropertyDescriptor { get; }
    public abstract object? Value { get; }
    public virtual bool Expandable => false;
    public virtual bool Expanded { get => false; set { if (value) throw new NotSupportedException(); } }
    public object? Tag { get; set; }
    public abstract bool Select();
}
public sealed class SelectedGridItemChangedEventArgs(GridItem? oldSel,GridItem? newSel) : EventArgs
{
    public GridItem? OldSelection { get; }=oldSel;
    public GridItem? NewSelection { get; }=newSel;
}
public class PropertyValueChangedEventArgs(GridItem? changedItem,object? oldValue) : EventArgs
{
    public GridItem? ChangedItem { get; }=changedItem;
    public object? OldValue { get; }=oldValue;
}
public delegate void SelectedGridItemChangedEventHandler(object? sender,SelectedGridItemChangedEventArgs e);
public delegate void PropertyValueChangedEventHandler(object? s,PropertyValueChangedEventArgs e);

public class PropertyGrid : ContainerControl
{
    private sealed class DescriptorContext(object owner, PropertyDescriptor descriptor) : ITypeDescriptorContext
    {
        public object Instance => owner;
        public PropertyDescriptor PropertyDescriptor => descriptor;
        public IContainer? Container => (owner as IComponent)?.Site?.Container;
        public object? GetService(Type serviceType) => (owner as IComponent)?.Site?.GetService(serviceType);
        public bool OnComponentChanging() => true;
        public void OnComponentChanged() { }
    }

    private sealed class Entry : GridItem
    {
        internal required PropertyGrid Grid;
        internal required GridItemType Kind;
        internal required string Caption;
        internal Entry? Ancestor;
        internal object[] Owners=[];
        internal PropertyDescriptor[] Descriptors=[];
        internal bool Open;
        private List<Entry>? children;
        internal List<Entry> Children
        {
            get => children ??= Grid.NestedProperties(this);
            set => children = value;
        }
        internal DescriptorContext Context(int index) => new(Owners[index], Descriptors[index]);
        internal bool CanWrite => Descriptors.Length > 0 && Descriptors.Select((d,i) =>
            !d.IsReadOnly && d.Converter.CanConvertFrom(Context(i),typeof(string))).All(can => can);
        public override GridItemCollection GridItems => Children.Count==0 ? GridItemCollection.Empty : new(Children);
        public override GridItemType GridItemType => Kind;
        public override string Label => Caption;
        public override GridItem? Parent => Ancestor;
        public override PropertyDescriptor? PropertyDescriptor => Descriptors.FirstOrDefault();
        public override object? Value
        {
            get
            {
                if (Descriptors.Length==0) return Kind==GridItemType.Root ? Grid.SelectedObject : null;
                var value=Descriptors[0].GetValue(Owners[0]);
                for (int i=1;i<Descriptors.Length;i++) if (!Equals(value,Descriptors[i].GetValue(Owners[i]))) return null;
                return value;
            }
        }
        public override bool Expandable => Children.Count>0;
        public override bool Expanded
        {
            get => Open;
            set
            {
                if (value==Open || !Expandable) return;
                Open=value;
                Grid.ShowEntries(Grid.selected ?? this);
            }
        }
        public override bool Select() => Grid.Choose(this);

        internal string Formatted()
        {
            if (PropertyDescriptor is not { } descriptor || Value is not { } value) return "";
            return descriptor.Converter.ConvertToString(Context(0),CultureInfo.CurrentCulture,value) ?? "";
        }
        internal void Commit(string? text,bool reset)
        {
            var old=Descriptors.Select((p,i)=>p.GetValue(Owners[i])).ToArray();
            var converted=new object?[Descriptors.Length];
            if (!reset)
                for (int i=0;i<Descriptors.Length;i++)
                    converted[i]=Descriptors[i].Converter.ConvertFromString(Context(i),CultureInfo.CurrentCulture,text ?? "");
            int changed=0;
            try
            {
                for (;changed<Descriptors.Length;changed++)
                {
                    if (reset) Descriptors[changed].ResetValue(Owners[changed]);
                    else Descriptors[changed].SetValue(Owners[changed],converted[changed]);
                }
            }
            catch
            {
                // Multi-selection is one edit. Restore earlier objects if a later setter rejected it.
                for (int i=changed-1;i>=0;i--) Descriptors[i].SetValue(Owners[i],old[i]);
                throw;
            }
            if (Descriptors.Where((p,i)=>!Equals(p.GetValue(Owners[i]),old[i])).Any())
                Grid.OnPropertyValueChanged(new(this,old[0]));
        }
    }

    // Grid editing calls Value's setter through TypeDescriptor. The owning business object,
    // its type converter, and its property descriptor stay in control of validation and updates.
    private sealed class DisplayRow(Entry entry)
    {
        internal Entry Entry => entry;
        public string Property
        {
            get
            {
                int depth = 0;
                for (var p = entry.Ancestor; p?.Ancestor != null; p = p.Ancestor) depth++;
                string marker = entry.Expandable ? (entry.Open ? "▾ " : "▸ ") : "";
                return new string(' ', depth * 3) + marker + entry.Label;
            }
        }
        public string Value { get => entry.Formatted(); set => entry.Commit(value,false); }
    }
    private object[] objects=[];
    private PropertySort sort=PropertySort.CategorizedAlphabetical;
    private AttributeCollection browsable=new(BrowsableAttribute.Yes);
    private readonly DataGridView grid = new() { Dock=DockStyle.Fill, AutoGenerateColumns=false,
        AllowUserToAddRows=false, AllowUserToDeleteRows=false, RowHeadersVisible=false,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
    private readonly FlowLayoutPanel toolbar = new() { Dock=DockStyle.Top, Height=32, WrapContents=false };
    private readonly Label help = new() { Dock=DockStyle.Bottom, Height=56, Padding=new Padding(4) };
    private readonly Button reset=new() { Text="Reset", Width=56 };
    private readonly Button expand=new() { Text="Expand", Width=68 };
    private List<DisplayRow> visible=[];
    private Entry? selected, root;
    private bool updating;

    public PropertyGrid()
    {
        Size=new(260,280);
        var category=new Button { Text="Categories", Width=80 };
        var alphabet=new Button { Text="A–Z", Width=44 };
        category.Click+=(_,_) => PropertySort=PropertySort.CategorizedAlphabetical;
        alphabet.Click+=(_,_) => PropertySort=PropertySort.Alphabetical;
        reset.Click+=(_,_) => ResetSelectedProperty();
        expand.Click+=(_,_) => { if (selected?.Expandable==true) selected.Expanded=!selected.Expanded; };
        toolbar.Controls.AddRange([category,alphabet,expand,reset]);
        grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name="Property", HeaderText="Property", DataPropertyName="Property", ReadOnly=true },
            new DataGridViewTextBoxColumn { Name="Value", HeaderText="Value", DataPropertyName="Value" });
        grid.CellBeginEdit+=(_,e) =>
        {
            if (e.ColumnIndex!=1 || e.RowIndex<0 || e.RowIndex>=visible.Count || !visible[e.RowIndex].Entry.CanWrite)
                e.Cancel=true;
        };
        grid.DataError+=(_,e) => help.Text=e.Exception.GetBaseException().Message;
        grid.SelectionChanged+=(_,_) =>
        {
            if (!updating && grid.CurrentRow?.DataBoundItem is DisplayRow row) SetSelected(row.Entry);
        };
        grid.KeyDown+=(_,e) =>
        {
            if (grid.IsCurrentCellInEditMode || selected?.Expandable!=true) return;
            if (e.KeyCode==Keys.Right || e.KeyCode==Keys.Left || e.KeyCode==Keys.Space)
            {
                selected.Expanded=e.KeyCode==Keys.Right || (e.KeyCode==Keys.Space && !selected.Expanded);
                e.Handled=true;
            }
        };
        Controls.AddRange([grid,help,toolbar]);
    }
    public object? SelectedObject { get => objects.FirstOrDefault(); set => SelectedObjects=value==null ? [] : [value]; }
    public object[] SelectedObjects
    {
        get => (object[])objects.Clone();
        set
        {
            value??=[];
            if (value.Any(o=>o==null)) throw new ArgumentException("The selection contains null.",nameof(value));
            objects=(object[])value.Clone();
            Rebuild();
            OnSelectedObjectsChanged(EventArgs.Empty);
        }
    }
    public AttributeCollection BrowsableAttributes
    {
        get => browsable;
        set { browsable=value ?? new(BrowsableAttribute.Yes); Rebuild(); }
    }
    public PropertySort PropertySort
    {
        get => sort;
        set
        {
            if (!Enum.IsDefined(value)) throw new InvalidEnumArgumentException(nameof(value),(int)value,typeof(PropertySort));
            if (value==sort) return;
            sort=value;
            Rebuild();
            OnPropertySortChanged(EventArgs.Empty);
        }
    }
    public GridItem? SelectedGridItem
    {
        get => selected;
        set
        {
            if (value is not Entry item || item.Grid!=this || !Choose(item))
                throw new ArgumentException("The entry must belong to this property grid.",nameof(value));
        }
    }
    public bool HelpVisible
    {
        get => help.Visible;
        set { help.Visible=value; help.Height=value ? 56 : 0; PerformLayout(); }
    }
    public virtual bool ToolbarVisible
    {
        get => toolbar.Visible;
        set { toolbar.Visible=value; toolbar.Height=value ? 32 : 0; PerformLayout(); }
    }
    public event EventHandler? SelectedObjectsChanged, PropertySortChanged;
    public event PropertyValueChangedEventHandler? PropertyValueChanged;
    public event SelectedGridItemChangedEventHandler? SelectedGridItemChanged;
    protected virtual void OnSelectedObjectsChanged(EventArgs e) => SelectedObjectsChanged?.Invoke(this,e);
    protected virtual void OnPropertySortChanged(EventArgs e) => PropertySortChanged?.Invoke(this,e);
    protected virtual void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
    {
        UpdateHelp();
        PropertyValueChanged?.Invoke(this,e);
    }
    protected virtual void OnSelectedGridItemChanged(SelectedGridItemChangedEventArgs e) => SelectedGridItemChanged?.Invoke(this,e);
    public override void Refresh() { Rebuild(); base.Refresh(); }

    private void Rebuild()
    {
        string? keep=selected?.PropertyDescriptor?.Name;
        root=new Entry { Grid=this,Kind=GridItemType.Root,Caption=objects.FirstOrDefault()?.GetType().Name ?? "",Open=true };
        if (objects.Length>0)
        {
            var attributes=browsable.Cast<Attribute>().ToArray();
            var descriptions=objects.Select(o=>TypeDescriptor.GetProperties(o,attributes)).ToArray();
            var entries=new List<Entry>();
            foreach (PropertyDescriptor p in descriptions[0])
            {
                if (CommonEntry(objects, descriptions, p, root) is { } property)
                    entries.Add(property);
            }
            IEnumerable<Entry> ordered=entries;
            if (sort.HasFlag(PropertySort.Alphabetical)) ordered=ordered.OrderBy(e=>e.Caption,StringComparer.CurrentCultureIgnoreCase);
            if (sort.HasFlag(PropertySort.Categorized))
            {
                foreach (var category in ordered.GroupBy(e=>e.PropertyDescriptor!.Category).OrderBy(g=>g.Key,StringComparer.CurrentCultureIgnoreCase))
                {
                    var group=new Entry { Grid=this,Kind=GridItemType.Category,Caption=category.Key,Ancestor=root,Open=true,Children=category.ToList() };
                    foreach (var item in group.Children) item.Ancestor=group;
                    root.Children.Add(group);
                }
            }
            else
            {
                root.Children=ordered.ToList();
                foreach (var item in root.Children) item.Ancestor=root;
            }
            keep ??= TypeDescriptor.GetDefaultProperty(objects[0])?.Name;
            ShowEntries(entries.FirstOrDefault(e=>e.PropertyDescriptor?.Name==keep) ?? ordered.FirstOrDefault());
        }
        else ShowEntries(null);
    }
    private Entry? CommonEntry(object[] values, PropertyDescriptorCollection[] properties, PropertyDescriptor first, Entry parent)
    {
        var descriptors = new PropertyDescriptor[values.Length];
        var owners = new object[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            var p = properties[i][first.Name];
            if (p == null || p.PropertyType != first.PropertyType ||
                (values.Length > 1 && p.Attributes.Contains(MergablePropertyAttribute.No))) return null;
            descriptors[i] = p;
            owners[i] = values[i] is ICustomTypeDescriptor custom ? custom.GetPropertyOwner(p) ?? values[i] : values[i];
        }
        return new Entry { Grid = this, Kind = GridItemType.Property, Caption = first.DisplayName,
            Owners = owners, Descriptors = descriptors, Ancestor = parent };
    }

    private List<Entry> NestedProperties(Entry parent)
    {
        if (parent.Kind != GridItemType.Property) return [];
        var values = new object[parent.Descriptors.Length];
        var descriptors = new PropertyDescriptorCollection[values.Length];
        var attributes = browsable.Cast<Attribute>().ToArray();
        for (int i = 0; i < values.Length; i++)
        {
            var descriptor = parent.Descriptors[i];
            var context = parent.Context(i);
            if (!descriptor.Converter.GetPropertiesSupported(context)) return [];
            var value = descriptor.GetValue(parent.Owners[i]);
            // A property on a boxed struct needs an immutable-value editor and write-back.
            if (value == null || value.GetType().IsValueType) return [];
            for (Entry? item = parent; item != null; item = item.Ancestor)
                if (item.Owners.Any(owner => ReferenceEquals(owner,value))) return [];
            values[i] = value;
            descriptors[i] = descriptor.Converter.GetProperties(context,value,attributes) ?? PropertyDescriptorCollection.Empty;
        }
        var result = new List<Entry>();
        foreach (PropertyDescriptor p in descriptors[0])
            if (CommonEntry(values,descriptors,p,parent) is { } child) result.Add(child);
        if (sort.HasFlag(PropertySort.Alphabetical))
            result.Sort((a,b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Label,b.Label));
        return result;
    }

    private void ShowEntries(Entry? preferred)
    {
        var rows = new List<DisplayRow>();
        void Append(Entry entry)
        {
            rows.Add(new(entry));
            if (entry.Open) foreach (var child in entry.Children) Append(child);
        }
        if (root != null) foreach (var entry in root.Children) Append(entry);
        updating=true;
        try
        {
            visible=rows;
            grid.DataSource=visible;
            int index=rows.FindIndex(r=>r.Entry==preferred);
            for (var p = preferred?.Ancestor; index < 0 && p != null; p = p.Ancestor)
                index=rows.FindIndex(r=>r.Entry==p);
            if (index<0 && rows.Count>0) index=0;
            if (index>=0) grid.CurrentCell=grid.Rows[index].Cells[1];
            SetSelected(index>=0 ? rows[index].Entry : null);
        }
        finally { updating=false; }
    }
    private bool Choose(Entry entry)
    {
        for (Entry? p=entry; p!=null; p=p.Ancestor)
        {
            if (p==root)
            {
                if (entry.Kind==GridItemType.Root) return false;
                for (var parent = entry.Ancestor; parent != null; parent = parent.Ancestor) parent.Open=true;
                ShowEntries(entry);
                return true;
            }
        }
        return false;
    }
    private void SetSelected(Entry? item)
    {
        var previous=selected;
        selected=item;
        UpdateHelp();
        if (previous!=item) OnSelectedGridItemChanged(new(previous,item));
    }
    private void UpdateHelp()
    {
        var d=selected?.PropertyDescriptor;
        help.Text=d==null ? selected?.Label ?? "" : d.DisplayName+Environment.NewLine+d.Description;
        expand.Enabled=selected?.Expandable==true;
        expand.Text=selected?.Open==true ? "Collapse" : "Expand";
        reset.Enabled=selected is { } e && e.Descriptors.Length>0 &&
            e.Descriptors.Select((p,i)=>p.CanResetValue(e.Owners[i])).All(can=>can);
    }
    public void ResetSelectedProperty()
    {
        if (selected==null || !reset.Enabled) return;
        selected.Commit(null,true);
        ShowEntries(selected);
    }
    public void ExpandAllGridItems() => SetGroups(true);
    public void CollapseAllGridItems() => SetGroups(false);
    private void SetGroups(bool expandAll)
    {
        if (root==null) return;
        void Visit(Entry entry,int depth)
        {
            if (!entry.Expandable) return;
            entry.Open = expandAll && depth < 10;
            if (depth < 10) foreach (var child in entry.Children) Visit(child,depth+1);
        }
        foreach (var entry in root.Children) Visit(entry,0);
        ShowEntries(selected);
    }
}
