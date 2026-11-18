using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

internal static class ListNative
{
    [DllImport("MacForms")] internal static extern void mf_list_render(IntPtr h,string json,IntPtr[] images,int count);
    [DllImport("MacForms")] internal static extern void mf_list_select(IntPtr h,int[] indices,int count);
    [DllImport("MacForms")] private static extern int mf_list_selection(IntPtr h,[Out] int[] buffer,int capacity);
    [DllImport("MacForms")] internal static extern int mf_list_action_index(IntPtr h,int ev);
    [DllImport("MacForms")] internal static extern void mf_list_ensure_visible(IntPtr h,int index);
    internal static int[] Selected(IntPtr h,int capacity)
    {
        int[] buffer = new int[capacity];
        int length = mf_list_selection(h,buffer,capacity);
        return buffer[..length];
    }
}

public enum View { LargeIcon, Details, SmallIcon, List, Tile }
public enum HorizontalAlignment { Left, Right, Center }
public enum ColumnHeaderStyle { None, Nonclickable, Clickable }
public enum SortOrder { None, Ascending, Descending }

public delegate void ColumnClickEventHandler(object? sender,ColumnClickEventArgs e);
public class ColumnClickEventArgs(int column) : EventArgs { public int Column { get; } = column; }
public delegate void ItemCheckEventHandler(object? sender,ItemCheckEventArgs e);
public class ItemCheckEventArgs(int index,CheckState newValue,CheckState currentValue) : EventArgs
{
    public int Index { get; } = index;
    public CheckState CurrentValue { get; } = currentValue;
    public CheckState NewValue { get; set; } = newValue;
}
public delegate void ItemCheckedEventHandler(object? sender,ItemCheckedEventArgs e);
public class ItemCheckedEventArgs(ListViewItem item) : EventArgs { public ListViewItem Item { get; } = item; }
public delegate void ListViewItemSelectionChangedEventHandler(object? sender,ListViewItemSelectionChangedEventArgs e);
public class ListViewItemSelectionChangedEventArgs(ListViewItem item,int itemIndex,bool isSelected) : EventArgs
{
    public ListViewItem Item { get; } = item;
    public int ItemIndex { get; } = itemIndex;
    public bool IsSelected { get; } = isSelected;
}

public sealed class ImageList : Component
{
    public ImageList() { Images = new(this); }
    public ImageList(IContainer container) : this() { container.Add(this); }
    public ImageCollection Images { get; }
    public Size ImageSize { get; set; } = new(16,16);
    public ImageListStreamer? ImageStream
    {
        get => Images.Count == 0 ? null : new ImageListStreamer(this);
        set
        {
            Images.Clear();
            if (value == null) return;
            ImageSize = value.ImageSize;
            foreach (var frame in value.Frames) Images.Add(new Bitmap(frame));
        }
    }
    internal event EventHandler? Changed;
    public sealed class ImageCollection : IReadOnlyList<Image>
    {
        private readonly ImageList owner;
        private readonly List<(string Key,Image Image)> entries = new();
        internal ImageCollection(ImageList owner) { this.owner = owner; }
        public int Count => entries.Count;
        public Image this[int index] => entries[index].Image;
        public Image? this[string key] => entries.FirstOrDefault(e => string.Equals(e.Key,key,StringComparison.OrdinalIgnoreCase)).Image;
        public int IndexOfKey(string key) => entries.FindIndex(e => string.Equals(e.Key,key,StringComparison.OrdinalIgnoreCase));
        public void Add(Image image) => Add("",image);
        public void Add(string key,Image image)
        {
            ArgumentNullException.ThrowIfNull(image); entries.Add((key,image)); owner.Changed?.Invoke(owner,EventArgs.Empty);
        }
        public void AddRange(Image[] images) { foreach (var image in images) Add(image); }
        public void RemoveAt(int index) { entries.RemoveAt(index); owner.Changed?.Invoke(owner,EventArgs.Empty); }
        public void Clear() { entries.Clear(); owner.Changed?.Invoke(owner,EventArgs.Empty); }
        public void SetKeyName(int index,string name) { entries[index] = (name,this[index]); owner.Changed?.Invoke(owner,EventArgs.Empty); }
        public IEnumerator<Image> GetEnumerator() => entries.Select(e => e.Image).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

public class ColumnHeader : Component
{
    private string text = "ColumnHeader";
    private int width = 60;
    private HorizontalAlignment alignment;
    public ListView? ListView { get; internal set; }
    public int Index => ListView?.Columns.IndexOf(this) ?? -1;
    public string Name { get; set; } = "";
    public object? Tag { get; set; }
    public string Text { get => text; set { text = value ?? ""; ListView?.Render(); } }
    public int Width { get => width; set { width = Math.Max(-2,value); ListView?.Render(); } }
    public HorizontalAlignment TextAlign { get => alignment; set { alignment = value; ListView?.Render(); } }
}

public class ListViewItem
{
    public ListViewItem() : this("") {}
    public ListViewItem(string? text) : this(new[] { text ?? "" }) {}
    public ListViewItem(string? text,int imageIndex) : this(text) { ImageIndex = imageIndex; }
    public ListViewItem(string? text,string imageKey) : this(text) { ImageKey = imageKey; }
    public ListViewItem(string[] items)
    {
        SubItems = new(this);
        SubItems.AddRange(items.Length == 0 ? new[]{""} : items);
    }
    public ListViewItem(string[] items,int imageIndex) : this(items) { ImageIndex = imageIndex; }
    public ListViewItem(string[] items,string imageKey) : this(items) { ImageKey = imageKey; }
    public ListView? ListView { get; internal set; }
    public int Index => ListView?.Items.IndexOf(this) ?? -1;
    public string Name { get; set; } = "";
    public object? Tag { get; set; }
    public string Text
    {
        get => SubItems.Count == 0 ? "" : SubItems[0].Text;
        set { if (SubItems.Count == 0) SubItems.Add(value); else SubItems[0].Text = value; }
    }
    public ListViewSubItemCollection SubItems { get; }
    internal bool SelectedState, CheckedState;
    private string imageKey = "";
    private int imageIndex = -1;
    public bool Checked { get => CheckedState; set { if (ListView is {} list) list.CheckItem(this,value); else CheckedState = value; } }
    public bool Selected { get => SelectedState; set { if (ListView is {} list) list.SelectItem(this,value); else SelectedState = value; } }
    public int ImageIndex { get => imageIndex; set { imageIndex = value; imageKey = ""; ListView?.Render(); } }
    public string ImageKey { get => imageKey; set { imageKey = value ?? ""; imageIndex = -1; ListView?.Render(); } }
    public void Remove() => ListView?.Items.Remove(this);
    public void EnsureVisible() => ListView?.EnsureVisible(Index);
    public override string ToString() => $"ListViewItem: {{{Text}}}";

    public class ListViewSubItem
    {
        private string text;
        internal ListViewItem? Owner;
        public ListViewSubItem() : this(null,"") {}
        public ListViewSubItem(ListViewItem? owner,string? text) { Owner = owner; this.text = text ?? ""; }
        public string Text { get => text; set { text = value ?? ""; Owner?.ListView?.Render(); } }
        public object? Tag { get; set; }
        public string Name { get; set; } = "";
    }
    public class ListViewSubItemCollection : Collection<ListViewSubItem>
    {
        private readonly ListViewItem owner;
        internal ListViewSubItemCollection(ListViewItem owner) { this.owner = owner; }
        public ListViewSubItem Add(string text) => Add(new ListViewSubItem(owner,text));
        public new ListViewSubItem Add(ListViewSubItem item) { base.Add(item); return item; }
        public void AddRange(string[] items) { foreach (var item in items) Add(item); }
        public void AddRange(ListViewSubItem[] items) { foreach (var item in items) Add(item); }
        protected override void InsertItem(int index,ListViewSubItem item) { item.Owner=owner; base.InsertItem(index,item); owner.ListView?.Render(); }
        protected override void RemoveItem(int index) { this[index].Owner=null; base.RemoveItem(index); owner.ListView?.Render(); }
        protected override void SetItem(int index,ListViewSubItem item) { this[index].Owner=null; item.Owner=owner; base.SetItem(index,item); owner.ListView?.Render(); }
        protected override void ClearItems() { foreach (var item in this) item.Owner=null; base.ClearItems(); owner.ListView?.Render(); }
    }
}

public class ListView : Control
{
    internal override int Kind => 24;
    private View view;
    private bool checkBoxes, multiSelect = true, gridLines;
    private ColumnHeaderStyle header = ColumnHeaderStyle.Clickable;
    private int updating;
    private SortOrder order;
    private IComparer? sorter;
    private ImageList? largeImages,smallImages;
    private Size tileSize = new(160,50);
    public ListView()
    {
        Items = new(this); Columns = new(this);
        SelectedItems = new(this); SelectedIndices = new(this);
        CheckedItems = new(this); CheckedIndices = new(this);
        Size = new(121,97);
    }
    public ListViewItemCollection Items { get; }
    public ColumnHeaderCollection Columns { get; }
    public SelectedListViewItemCollection SelectedItems { get; }
    public SelectedIndexCollection SelectedIndices { get; }
    public CheckedListViewItemCollection CheckedItems { get; }
    public CheckedIndexCollection CheckedIndices { get; }
    public View View { get => view; set { view=value; Render(); } }
    public Size TileSize { get => tileSize; set { if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value)); tileSize=value; Render(); } }
    public bool CheckBoxes { get => checkBoxes; set { checkBoxes=value; Render(); } }
    private bool fullRowSelect, hideSelection;
    private BorderStyle borderStyle = BorderStyle.Fixed3D;
    public bool HideSelection { get => hideSelection; set { hideSelection=value; Render(); } }
    public BorderStyle BorderStyle { get => borderStyle; set { borderStyle=value; Render(); } }
    public bool FullRowSelect { get => fullRowSelect; set { fullRowSelect=value; Render(); } }
    public bool GridLines { get => gridLines; set { gridLines=value; Render(); } }
    public ColumnHeaderStyle HeaderStyle { get => header; set { header=value; Render(); } }
    public bool MultiSelect
    {
        get => multiSelect;
        set
        {
            multiSelect=value;
            if (!value && SelectedItems.Count > 1) SelectItem(SelectedItems[0],true);
            Render();
        }
    }
    public ImageList? LargeImageList { get => largeImages; set => SetImages(ref largeImages,value); }
    public ImageList? SmallImageList { get => smallImages; set => SetImages(ref smallImages,value); }
    private void SetImages(ref ImageList? previous, ImageList? next)
    {
        if (previous != null) previous.Changed -= ImagesChanged;
        previous = next; if (next != null) next.Changed += ImagesChanged; Render();
    }
    private void ImagesChanged(object? sender,EventArgs e) => Render();
    public IComparer? ListViewItemSorter { get => sorter; set { sorter=value; Sort(); } }
    public SortOrder Sorting { get => order; set { order=value; Sort(); } }
    public event EventHandler? ItemActivate, SelectedIndexChanged;
    public event ColumnClickEventHandler? ColumnClick;
    public event ItemCheckEventHandler? ItemCheck;
    public event ItemCheckedEventHandler? ItemChecked;
    public event ListViewItemSelectionChangedEventHandler? ItemSelectionChanged;
    public void BeginUpdate() => updating++;
    public void EndUpdate() { if (updating > 0 && --updating == 0) Render(); }
    public void Clear() { BeginUpdate(); try { Items.Clear(); Columns.Clear(); } finally { EndUpdate(); } }
    public void Sort()
    {
        if (sorter != null || order != SortOrder.None) Items.SortItems((a,b) =>
            sorter?.Compare(a,b) ?? (order == SortOrder.Descending ? string.Compare(b.Text,a.Text,StringComparison.CurrentCulture) : string.Compare(a.Text,b.Text,StringComparison.CurrentCulture)));
        Render();
    }
    public void EnsureVisible(int index)
    {
        if ((uint)index >= (uint)Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (NativeHandle != 0) ListNative.mf_list_ensure_visible(NativeHandle,index);
    }
    internal void CheckItem(ListViewItem item,bool value)
    {
        if (item.CheckedState == value) return;
        var args = new ItemCheckEventArgs(item.Index,value ? CheckState.Checked : CheckState.Unchecked,item.Checked ? CheckState.Checked : CheckState.Unchecked);
        ItemCheck?.Invoke(this,args);
        bool next = args.NewValue != CheckState.Unchecked;
        if (next != item.CheckedState) { item.CheckedState=next; ItemChecked?.Invoke(this,new(item)); }
        Render();
    }
    internal void SelectItem(ListViewItem item,bool value)
    {
        var selected = Items.Where(i => i.SelectedState).ToHashSet();
        if (value) { if (!multiSelect) selected.Clear(); selected.Add(item); } else selected.Remove(item);
        UpdateSelection(selected);
        SyncSelection();
    }
    private void UpdateSelection(HashSet<ListViewItem> selected)
    {
        var changes = Items.Where(i => i.SelectedState != selected.Contains(i)).ToArray();
        foreach (var i in changes) i.SelectedState = selected.Contains(i);
        foreach (var i in changes) ItemSelectionChanged?.Invoke(this,new(i,i.Index,i.Selected));
        if (changes.Length > 0) SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
    }
    private void SyncSelection()
    {
        if (NativeHandle == 0 || updating > 0) return;
        var selected = SelectedIndices.ToArray(); ListNative.mf_list_select(NativeHandle,selected,selected.Length);
    }
    internal void Render()
    {
        if (updating != 0 || NativeHandle == 0) return;
        var model = new {
            view=(int)view, fullRowSelect, hideSelection, borderStyle=(int)borderStyle, checks=checkBoxes, grid=gridLines, multi=multiSelect,
            headers=header != ColumnHeaderStyle.None, tileSize=new[]{tileSize.Width,tileSize.Height},
            columns=Columns.Select(c=>new { title=c.Text,width=c.Width,alignment=(int)c.TextAlign }),
            rows=Items.Select(i=>new { values=i.SubItems.Select(s=>s.Text),state=i.Checked ? 1 : 0 })
        };
        var images = (view is View.LargeIcon or View.Tile ? largeImages : smallImages)?.Images;
        Image? GetImage(ListViewItem item) => images == null ? null : item.ImageKey != "" ? images[item.ImageKey] :
            (uint)item.ImageIndex < (uint)images.Count ? images[item.ImageIndex] : null;
        var handles = Items.Select(i=>GetImage(i)?.Handle.DangerousGetHandle() ?? IntPtr.Zero).ToArray();
        ListNative.mf_list_render(NativeHandle,JsonSerializer.Serialize(model),handles,handles.Length);
        SyncSelection();
    }
    internal override void ApplyNative() => Render();
    internal override void OnNative(int ev)
    {
        switch (ev)
        {
            case 24: UpdateSelection(ListNative.Selected(NativeHandle,Items.Count).Select(i=>Items[i]).ToHashSet()); break;
            case 25:
                var item = Items[ListNative.mf_list_action_index(NativeHandle,ev)]; item.Checked = !item.Checked; break;
            case 26: ItemActivate?.Invoke(this,EventArgs.Empty); break;
            case 27: if (header == ColumnHeaderStyle.Clickable) ColumnClick?.Invoke(this,new(ListNative.mf_list_action_index(NativeHandle,ev))); break;
            default: base.OnNative(ev); break;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (largeImages != null) largeImages.Changed -= ImagesChanged; if (smallImages != null) smallImages.Changed -= ImagesChanged; }
        base.Dispose(disposing);
    }
    public class ListViewItemCollection : Collection<ListViewItem>
    {
        private readonly ListView owner;
        internal ListViewItemCollection(ListView owner) { this.owner=owner; }
        public ListViewItem? this[string key] => this.FirstOrDefault(i=>string.Equals(i.Name,key,StringComparison.OrdinalIgnoreCase));
        public new ListViewItem Add(ListViewItem item) { base.Add(item); return item; }
        public ListViewItem Add(string text) => Add(new ListViewItem(text));
        public ListViewItem Add(string text,int imageIndex) => Add(new ListViewItem(text,imageIndex));
        public ListViewItem Add(string text,string imageKey) => Add(new ListViewItem(text,imageKey));
        public ListViewItem Add(string key,string text,int imageIndex) => Add(new ListViewItem(text,imageIndex){Name=key});
        public ListViewItem Add(string key,string text,string imageKey) => Add(new ListViewItem(text,imageKey){Name=key});
        public void AddRange(ListViewItem[] items) { owner.BeginUpdate(); try { foreach(var item in items) Add(item); } finally { owner.EndUpdate(); } }
        protected override void InsertItem(int index,ListViewItem item)
        {
            if (item.ListView != null) throw new ArgumentException("Item already belongs to a ListView.");
            base.InsertItem(index,item); item.ListView=owner; owner.Sort();
        }
        protected override void SetItem(int index,ListViewItem item)
        {
            if (item.ListView != null) throw new ArgumentException("Item already belongs to a ListView.");
            this[index].ListView=null; base.SetItem(index,item); item.ListView=owner; owner.Sort();
        }
        protected override void RemoveItem(int index) { this[index].ListView=null; base.RemoveItem(index); owner.Render(); }
        protected override void ClearItems() { foreach(var item in this) item.ListView=null; base.ClearItems(); owner.Render(); }
        internal void SortItems(Comparison<ListViewItem> compare) => ((List<ListViewItem>)Items).Sort(compare);
    }
    public class ColumnHeaderCollection : Collection<ColumnHeader>
    {
        private readonly ListView owner;
        internal ColumnHeaderCollection(ListView owner) { this.owner=owner; }
        public ColumnHeader Add(string text) => Add(text,60,HorizontalAlignment.Left);
        public ColumnHeader Add(string text,int width) => Add(text,width,HorizontalAlignment.Left);
        public ColumnHeader Add(string text,int width,HorizontalAlignment alignment)
        {
            var col=new ColumnHeader{Text=text,Width=width,TextAlign=alignment}; Add(col); return col;
        }
        public new int Add(ColumnHeader column) { base.Add(column); return IndexOf(column); }
        public void AddRange(ColumnHeader[] cols) { owner.BeginUpdate(); try { foreach(var col in cols) Add(col); } finally { owner.EndUpdate(); } }
        protected override void InsertItem(int index,ColumnHeader item)
        {
            if (item.ListView != null) throw new ArgumentException("Column already belongs to a ListView.");
            base.InsertItem(index,item); item.ListView=owner; owner.Render();
        }
        protected override void SetItem(int index,ColumnHeader item)
        {
            if (item.ListView != null) throw new ArgumentException("Column already belongs to a ListView.");
            this[index].ListView=null; base.SetItem(index,item); item.ListView=owner; owner.Render();
        }
        protected override void RemoveItem(int index) { this[index].ListView=null; base.RemoveItem(index); owner.Render(); }
        protected override void ClearItems() { foreach(var col in this) col.ListView=null; base.ClearItems(); owner.Render(); }
    }

    public class SelectedListViewItemCollection : IReadOnlyList<ListViewItem>
    {
        private readonly ListView owner;
        internal SelectedListViewItemCollection(ListView owner) { this.owner=owner; }
        public int Count => owner.Items.Count(i=>i.Selected);
        public ListViewItem this[int index] => owner.Items.Where(i=>i.Selected).ElementAt(index);
        public void Clear() => owner.SelectedIndices.Clear();
        public IEnumerator<ListViewItem> GetEnumerator() => owner.Items.Where(i=>i.Selected).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class SelectedIndexCollection : IReadOnlyList<int>
    {
        private readonly ListView owner;
        internal SelectedIndexCollection(ListView owner) { this.owner=owner; }
        public int Count => owner.SelectedItems.Count;
        public int this[int index] => this.ElementAtByEnumeration(index);
        public int Add(int itemIndex) { owner.Items[itemIndex].Selected=true; return Count; }
        public void Remove(int itemIndex) { owner.Items[itemIndex].Selected=false; }
        public void Clear() { owner.UpdateSelection(new()); owner.SyncSelection(); }
        public IEnumerator<int> GetEnumerator() => Enumerable.Range(0,owner.Items.Count).Where(i=>owner.Items[i].Selected).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class CheckedListViewItemCollection : IReadOnlyList<ListViewItem>
    {
        private readonly ListView owner;
        internal CheckedListViewItemCollection(ListView owner) { this.owner=owner; }
        public int Count => owner.Items.Count(i=>i.Checked);
        public ListViewItem this[int index] => owner.Items.Where(i=>i.Checked).ElementAt(index);
        public IEnumerator<ListViewItem> GetEnumerator() => owner.Items.Where(i=>i.Checked).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class CheckedIndexCollection : IReadOnlyList<int>
    {
        private readonly ListView owner;
        internal CheckedIndexCollection(ListView owner) { this.owner=owner; }
        public int Count => owner.CheckedItems.Count;
        public int this[int index] => this.ElementAtByEnumeration(index);
        public IEnumerator<int> GetEnumerator() => Enumerable.Range(0,owner.Items.Count).Where(i=>owner.Items[i].Checked).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
internal static class ListIndexAccess
{
    internal static T ElementAtByEnumeration<T>(this IEnumerable<T> items,int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        foreach(var item in items) if(index-- == 0) return item;
        throw new ArgumentOutOfRangeException(nameof(index));
    }
}
