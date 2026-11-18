using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public class TabPage : Panel
{
    internal override int Kind => 14;
    public TabPage() {}
    public TabPage(string text) { Text=text; }
    private string imageKey="",tip="";
    private int imageIndex=-1;
    public bool UseVisualStyleBackColor { get; set; }=true;
    public string ToolTipText { get=>tip; set { tip=value??""; Parent?.PerformLayout(); } }
    public string ImageKey { get=>imageKey; set { imageKey=value??""; imageIndex=-1; Parent?.PerformLayout(); } }
    public int ImageIndex { get=>imageIndex; set { if(value < -1) throw new ArgumentOutOfRangeException(nameof(value)); imageIndex=value; imageKey=""; Parent?.PerformLayout(); } }
    public static TabPage? GetTabPageOfComponent(object? component)
    {
        for(var c=component as Control;c!=null;c=c.Parent) if(c is TabPage page) return page;
        return null;
    }
}

public class TabControl : Control
{
    [DllImport("MacForms")] private static extern int mf_tab_requested(IntPtr h);
    [DllImport("MacForms")] private static extern void mf_tab_choose(IntPtr h,int index);
    [DllImport("MacForms")] private static extern void mf_tab_position(IntPtr h,IntPtr page,int index,IntPtr image,string tip);
    [DllImport("MacForms")] private static extern void mf_tab_rect(IntPtr h,int index,out int x,out int y,out int width,out int height);
    private ImageList? images;
    private TabPage? selected;
    private int pendingIndex=-1;
    private bool initialized,tips;
    internal override int Kind=>13;
    public TabControl() => TabPages=new(this);
    public TabPageCollection TabPages { get; }
    public int TabCount => Controls.Count;
    public bool ShowToolTips { get=>tips; set { tips=value; PerformLayout(); } }
    public ImageList? ImageList
    {
        get=>images;
        set
        {
            if(images!=null) images.Changed-=ImagesChanged;
            images=value;
            if(images!=null) images.Changed+=ImagesChanged;
            PerformLayout();
        }
    }
    private void ImagesChanged(object? sender,EventArgs e) => PerformLayout();
    public int SelectedIndex
    {
        get=>initialized ? selected==null ? -1:Controls.IndexOf(selected) : pendingIndex;
        set
        {
            if(value< -1) throw new ArgumentOutOfRangeException(nameof(value));
            if(!initialized) { pendingIndex=value; return; }
            Change(value>=0 && value<TabCount ? TabPages[value] : null);
        }
    }
    public TabPage? SelectedTab
    {
        get=>initialized ? selected : (uint)pendingIndex<(uint)TabCount ? TabPages[pendingIndex]:null;
        set=>SelectedIndex=value==null ? -1 : Controls.IndexOf(value);
    }
    public event TabControlCancelEventHandler? Deselecting,Selecting;
    public event TabControlEventHandler? Deselected,Selected;
    public event EventHandler? SelectedIndexChanged;
    protected virtual void OnDeselecting(TabControlCancelEventArgs e)=>Deselecting?.Invoke(this,e);
    protected virtual void OnDeselected(TabControlEventArgs e)=>Deselected?.Invoke(this,e);
    protected virtual void OnSelecting(TabControlCancelEventArgs e)=>Selecting?.Invoke(this,e);
    protected virtual void OnSelected(TabControlEventArgs e)=>Selected?.Invoke(this,e);
    protected virtual void OnSelectedIndexChanged(EventArgs e)=>SelectedIndexChanged?.Invoke(this,e);
    private void Change(TabPage? next)
    {
        if(selected==next) return;
        TabPage? old=selected; int before=SelectedIndex;
        var leaving=new TabControlCancelEventArgs(old,before,false,TabControlAction.Deselecting);
        OnDeselecting(leaving);
        if (leaving.Cancel || selected!=old || IsDisposed) return;
        OnDeselected(new(old,before,TabControlAction.Deselected));
        if ((next!=null && next.Parent!=this) || selected!=old || IsDisposed) return;
        selected=next;
        mf_tab_choose(NativeHandle,SelectedIndex);
        var entering=new TabControlCancelEventArgs(next,SelectedIndex,false,TabControlAction.Selecting);
        OnSelecting(entering);
        if (selected!=next || IsDisposed) return;
        if (entering.Cancel)
        {
            selected=old?.Parent==this ? old:null; mf_tab_choose(NativeHandle,SelectedIndex); return;
        }
        OnSelected(new(next,SelectedIndex,TabControlAction.Selected)); OnSelectedIndexChanged(EventArgs.Empty);
    }
    public void SelectTab(int index)=>SelectTab(TabPages[index]);
    public void SelectTab(string tabPageName)=>SelectTab(TabPages[tabPageName]??throw new ArgumentException("No tab page has that name.",nameof(tabPageName)));
    public void SelectTab(TabPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if(page.Parent!=this) throw new ArgumentOutOfRangeException(nameof(page));
        SelectedTab=page;
    }
    public void DeselectTab(TabPage page) { if(SelectedTab==page && TabCount>1) SelectedIndex=(SelectedIndex+1)%TabCount; }
    public void DeselectTab(int index)=>DeselectTab(TabPages[index]);
    public void DeselectTab(string tabPageName)=>DeselectTab(TabPages[tabPageName]??throw new ArgumentException("No tab page has that name.",nameof(tabPageName)));
    public Rectangle GetTabRect(int index)
    {
        _=TabPages[index]; mf_tab_rect(Handle,index,out int x,out int y,out int width,out int height); return new(x,y,width,height);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e); if(e.Handled || TabCount<2) return;
        if (e.Control && e.KeyCode is Keys.Tab or Keys.PageDown or Keys.PageUp)
        {
            int step=e.Shift || e.KeyCode==Keys.PageUp ? -1:1;
            SelectedIndex=(SelectedIndex+step+TabCount)%TabCount; e.Handled=true;
        }
    }
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        if(initialized && !Disposing && selected==e.Control) { selected=null; if(TabCount>0) Change(TabPages[0]); else OnSelectedIndexChanged(EventArgs.Empty); }
        base.OnControlRemoved(e);
    }
    protected override void OnControlAdded(ControlEventArgs e)
    {
        if(initialized && TabCount==1) selected=(TabPage)e.Control!;
        base.OnControlAdded(e);
    }
    internal override void ApplyNative()
    {
        if (!initialized) selected=TabCount==0 ? null:TabPages[(uint)pendingIndex<(uint)TabCount ? pendingIndex:0];
        initialized=true; SyncHeaders();
    }
    internal override void OnNative(int ev)
    {
        if(ev==38) SelectedIndex=mf_tab_requested(NativeHandle);
        else if(ev!=4) base.OnNative(ev);
    }
    internal override void Arrange(Size delta)
    {
        foreach(var page in Controls) page.Size=new(Math.Max(0,Width-8),Math.Max(0,Height-30));
        if(initialized) SyncHeaders();
    }
    private void SyncHeaders()
    {
        for(int i=0;i<TabCount;i++)
        {
            var page=TabPages[i]; if(!page.IsHandleCreated) continue;
            var art=images?.Images;
            Image? image=art==null ? null:page.ImageKey!="" ? art[page.ImageKey]:(uint)page.ImageIndex<(uint)art.Count ? art[page.ImageIndex]:null;
            mf_tab_position(NativeHandle,page.NativeHandle,i,image?.Handle.DangerousGetHandle()??0,tips?page.ToolTipText:"");
        }
        mf_tab_choose(NativeHandle,SelectedIndex);
    }
    protected override void Dispose(bool disposing)
    {
        if(disposing && images!=null) images.Changed-=ImagesChanged;
        base.Dispose(disposing);
    }
    public class TabPageCollection : IList
    {
        private readonly TabControl owner;
        public TabPageCollection(TabControl owner) => this.owner=owner??throw new ArgumentNullException(nameof(owner));
        public int Count=>owner.Controls.Count;
        public bool IsReadOnly=>false;
        bool IList.IsFixedSize=>false;
        object ICollection.SyncRoot=>this;
        bool ICollection.IsSynchronized=>false;
        public virtual TabPage this[int index]
        {
            get=>(TabPage)owner.Controls[index];
            set { ArgumentNullException.ThrowIfNull(value); if(this[index]==value) return; RemoveAt(index); Insert(index,value); }
        }
        public virtual TabPage? this[string? key]=>string.IsNullOrEmpty(key)?null:owner.Controls[key] as TabPage;
        public void Add(TabPage value)=>owner.Controls.Add(value);
        public void Add(string text)=>Add(new TabPage(text));
        public void Add(string key,string text)=>Add(new TabPage(text){Name=key});
        public void Add(string key,string text,int imageIndex)=>Add(new TabPage(text){Name=key,ImageIndex=imageIndex});
        public void Add(string key,string text,string imageKey)=>Add(new TabPage(text){Name=key,ImageKey=imageKey});
        public void AddRange(TabPage[] pages) { foreach(var page in pages) Add(page); }
        public void Clear()=>owner.Controls.Clear();
        public bool Contains(TabPage page)=>owner.Controls.Contains(page);
        public bool ContainsKey(string? key)=>this[key]!=null;
        public int IndexOf(TabPage page)=>owner.Controls.IndexOf(page);
        public int IndexOfKey(string? key)=>this[key] is {} page?IndexOf(page):-1;
        public void Insert(int index,TabPage page)=>owner.Controls.Insert(index,page);
        public void Insert(int index,string text)=>Insert(index,new TabPage(text));
        public void Insert(int index,string key,string text)=>Insert(index,new TabPage(text){Name=key});
        public void Insert(int index,string key,string text,int imageIndex)=>Insert(index,new TabPage(text){Name=key,ImageIndex=imageIndex});
        public void Insert(int index,string key,string text,string imageKey)=>Insert(index,new TabPage(text){Name=key,ImageKey=imageKey});
        public void Remove(TabPage page)=>owner.Controls.Remove(page);
        public void RemoveAt(int index)=>owner.Controls.RemoveAt(index);
        public void RemoveByKey(string? key) { if(this[key] is {} page) Remove(page); }
        public IEnumerator GetEnumerator()=>owner.Controls.GetEnumerator();
        void ICollection.CopyTo(Array array,int index)=>((ICollection)owner.Controls).CopyTo(array,index);
        private static TabPage Page(object? value)=>value as TabPage??throw new ArgumentException("A TabPage is required.");
        object? IList.this[int i] { get=>this[i]; set=>this[i]=Page(value); }
        int IList.Add(object? value) { var page=Page(value); Add(page); return IndexOf(page); }
        void IList.Insert(int index,object? value)=>Insert(index,Page(value));
        bool IList.Contains(object? value)=>value is TabPage page && Contains(page);
        int IList.IndexOf(object? value)=>value is TabPage page?IndexOf(page):-1;
        void IList.Remove(object? value) { if(value is TabPage page) Remove(page); }
    }
}
