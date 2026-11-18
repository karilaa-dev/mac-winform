using System.Collections;
using System.Text.Json;

namespace System.Windows.Forms;

public class CheckedListBox : ListBox
{
    internal override int Kind => 25;
    internal override ListControl.ObjectCollection CreateItems() => new ObjectCollection(this);
    public new ObjectCollection Items => (ObjectCollection)base.Items;
    public event ItemCheckEventHandler? ItemCheck;
    private bool checkOnClick;
    private readonly List<CheckState> states = new();
    private object? previousSource;
    public CheckedListBox() { CheckedItems=new(this); CheckedIndices=new(this); Size = new(120,96); }
    public bool CheckOnClick { get => checkOnClick; set { checkOnClick=value; RefreshItems(); } }
    internal override void SetBoundsCore(System.Drawing.Rectangle value,bool updateNative)
    {
        if (IntegralHeight && value.Height > 4)
        {
            int rowHeight = (int)Math.Round(18 * Font.Size / 9);
            value.Height = 4 + Math.Max(1,(value.Height-4)/Math.Max(1,rowHeight))*rowHeight;
        }
        base.SetBoundsCore(value,updateNative);
    }
    public CheckedItemCollection CheckedItems { get; }
    public CheckedIndexCollection CheckedIndices { get; }
    private void SyncStates()
    {
        if (!ReferenceEquals(previousSource,DataSource)) { states.Clear(); previousSource=DataSource; }
        var count = DataSource == null ? Items.Count : Values().Count;
        while (states.Count < count) states.Add(CheckState.Unchecked);
        if (states.Count > count) states.RemoveRange(count,states.Count-count);
    }
    public CheckState GetItemCheckState(int index) { SyncStates(); return states[index]; }
    public bool GetItemChecked(int index) => GetItemCheckState(index) != CheckState.Unchecked;
    public void SetItemChecked(int index,bool value) => SetItemCheckState(index,value ? CheckState.Checked : CheckState.Unchecked);
    public void SetItemCheckState(int index,CheckState value)
    {
        if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
        var previous=GetItemCheckState(index);
        if (previous == value) return;
        var args=new ItemCheckEventArgs(index,value,previous);
        ItemCheck?.Invoke(this,args);
        states[index]=args.NewValue;
        RefreshItems();
    }
    internal override void RefreshItems()
    {
        SyncStates();
        if (NativeHandle == 0) return;
        var values=Values();
        var model=new { view=1, headers=false, multi=false, checks=true, checkedList=true,
            checkOnClick, rowHeight=(int)Math.Round(18 * Font.Size / 9), rows=values.Select((v,i)=>new{values=new[]{Display(v)},state=(int)states[i]}) };
        ListNative.mf_list_render(NativeHandle,JsonSerializer.Serialize(model),new IntPtr[values.Count],values.Count);
        if (SelectedIndex >= values.Count) SelectedIndex=-1;
        SelectNative(SelectedIndex);
    }
    internal override void SelectNative(int index)
    {
        var indices = index < 0 ? Array.Empty<int>() : new[]{index};
        ListNative.mf_list_select(NativeHandle,indices,indices.Length);
    }
    internal override void OnNative(int ev)
    {
        if (ev == 24) SelectedIndex = ListNative.Selected(NativeHandle,1).FirstOrDefault(-1);
        else if (ev == 25)
        {
            int index=ListNative.mf_list_action_index(NativeHandle,ev);
            SetItemChecked(index,!GetItemChecked(index));
        }
        else base.OnNative(ev);
    }

    public new class ObjectCollection : ListControl.ObjectCollection
    {
        private readonly CheckedListBox owner;
        internal ObjectCollection(CheckedListBox owner) : base(owner) { this.owner=owner; }
        public int Add(object item,bool isChecked) => Add(item,isChecked ? CheckState.Checked : CheckState.Unchecked);
        public int Add(object item,CheckState check)
        {
            int index=Add(item);
            owner.SetItemCheckState(index,check);
            return index;
        }
        protected override void InsertItem(int index,object item) { owner.states.Insert(index,CheckState.Unchecked); base.InsertItem(index,item); }
        protected override void RemoveItem(int index) { owner.states.RemoveAt(index); base.RemoveItem(index); }
        protected override void ClearItems() { owner.states.Clear(); base.ClearItems(); }
    }
    public class CheckedIndexCollection : IReadOnlyList<int>
    {
        private readonly CheckedListBox owner;
        internal CheckedIndexCollection(CheckedListBox owner) { this.owner=owner; }
        public int Count { get { owner.SyncStates(); return owner.states.Count(s=>s != CheckState.Unchecked); } }
        public int this[int index] => this.ElementAtByEnumeration(index);
        public IEnumerator<int> GetEnumerator()
        {
            owner.SyncStates();
            return Enumerable.Range(0,owner.states.Count).Where(i=>owner.states[i] != CheckState.Unchecked).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class CheckedItemCollection : IReadOnlyList<object>
    {
        private readonly CheckedListBox owner;
        internal CheckedItemCollection(CheckedListBox owner) { this.owner=owner; }
        public int Count => owner.CheckedIndices.Count;
        public object this[int index] => owner.Values()[owner.CheckedIndices[index]];
        public IEnumerator<object> GetEnumerator()
        {
            var values=owner.Values();
            return owner.CheckedIndices.Select(i=>values[i]).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
