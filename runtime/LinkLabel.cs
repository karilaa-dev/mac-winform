using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

public enum LinkBehavior { SystemDefault, AlwaysUnderline, HoverUnderline, NeverUnderline }
public struct LinkArea(int start,int length)
{
    public int Start { get; set; }=start;
    public int Length { get; set; }=length;
    public readonly bool IsEmpty=>Start==0 && Length==0;
}
public delegate void LinkLabelLinkClickedEventHandler(object? sender,LinkLabelLinkClickedEventArgs e);
public class LinkLabelLinkClickedEventArgs(LinkLabel.Link? link,MouseButtons button) : EventArgs
{
    public LinkLabelLinkClickedEventArgs(LinkLabel.Link? link) : this(link,MouseButtons.Left) {}
    public LinkLabel.Link? Link { get; }=link;
    public MouseButtons Button { get; }=button;
}
public class LinkLabel : Label, IButtonControl
{
    [DllImport("MacForms")] private static extern void mf_link_style(IntPtr h,string json);
    [DllImport("MacForms")] private static extern int mf_link_index(IntPtr h);
    internal override int Kind=>34;
    private Color linkColor=Color.Blue,visitedColor=Color.Purple,disabledColor=Color.Gray;
    private LinkBehavior behavior;
    private int focusedLink;
    public LinkLabel() { Links=new(this); Links.SetDefault(); TabStop=true; }
    public LinkCollection Links { get; }
    public LinkArea LinkArea
    {
        get=>Links.Count==0 ? new LinkArea() : new(Links[0].Start,Links[0].Length);
        set { Links.Clear(); if(!value.IsEmpty) Links.Add(value.Start,value.Length); }
    }
    public bool LinkVisited { get=>Links.Count>0 && Links[0].Visited; set { if(Links.Count>0) Links[0].Visited=value; } }
    public Color LinkColor { get=>linkColor; set { linkColor=value; RenderLinks(); } }
    public Color VisitedLinkColor { get=>visitedColor; set { visitedColor=value; RenderLinks(); } }
    public Color DisabledLinkColor { get=>disabledColor; set { disabledColor=value; RenderLinks(); } }
    public LinkBehavior LinkBehavior { get=>behavior; set { behavior=value; RenderLinks(); } }
    public DialogResult DialogResult { get; set; }
    public event LinkLabelLinkClickedEventHandler? LinkClicked;
    protected virtual void OnLinkClicked(LinkLabelLinkClickedEventArgs e)=>LinkClicked?.Invoke(this,e);
    void IButtonControl.NotifyDefault(bool value) { }
    public void PerformClick() => Activate(focusedLink,MouseButtons.None);
    private void Activate(int index,MouseButtons button)
    {
        if(!Enabled || (uint)index>=(uint)Links.Count || !Links[index].Enabled) return;
        focusedLink=index;
        OnClick(EventArgs.Empty);
        if (DialogResult!=DialogResult.None && FindForm() is {} form) form.DialogResult=DialogResult;
        OnLinkClicked(new(Links[index],button));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e); if(e.Handled || !Enabled || Links.Count==0) return;
        if(e.KeyCode is Keys.Enter or Keys.Space) { PerformClick(); e.Handled=true; return; }
        if(e.KeyCode is Keys.Left or Keys.Right)
        {
            int delta=e.KeyCode==Keys.Right ? 1 : -1;
            for(int i=1;i<=Links.Count;i++)
            {
                int next=(focusedLink+delta*i+Links.Count)%Links.Count;
                if (!Links[next].Enabled) continue;
                focusedLink=next;
                TextNative.mf_edit_select(Handle,Links[next].Start,Links[next].Length<0 ? Text.Length-Links[next].Start : Links[next].Length);
                break;
            }
            e.Handled=true;
        }
    }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); RenderLinks(); }
    internal override void ApplyNative()=>RenderLinks();
    internal override void OnNative(int ev)
    {
        if(ev==37) Activate(mf_link_index(NativeHandle),MouseButtons.Left);
        else base.OnNative(ev);
    }
    private void RenderLinks()
    {
        if(NativeHandle==0) return;
        var spans=Links.Select((item,index)=>new { index,start=item.Start,length=item.Length,
            enabled=item.Enabled,underline=behavior!=LinkBehavior.NeverUnderline && behavior!=LinkBehavior.HoverUnderline,
            hoverUnderline=behavior==LinkBehavior.HoverUnderline, color=unchecked((uint)(item.Enabled ? item.Visited ? visitedColor : linkColor : disabledColor).ToArgb()) });
        mf_link_style(NativeHandle,JsonSerializer.Serialize(spans));
    }

    public class Link
    {
        private int start,length;
        private bool visited,enabled=true;
        internal LinkLabel? Owner;
        public Link() {}
        public Link(int start,int length) : this(start,length,null) {}
        public Link(int start,int length,object? linkData) { Start=start; Length=length; LinkData=linkData; }
        public int Start { get=>start; set { ArgumentOutOfRangeException.ThrowIfNegative(value); start=value; Owner?.RenderLinks(); } }
        public int Length { get=>length; set { if(value< -1)throw new ArgumentOutOfRangeException(nameof(value)); length=value; Owner?.RenderLinks(); } }
        public bool Enabled { get=>enabled; set { enabled=value; Owner?.RenderLinks(); } }
        public bool Visited { get=>visited; set { visited=value; Owner?.RenderLinks(); } }
        public string Name { get; set; }="";
        public object? Tag { get; set; }
        public object? LinkData { get; set; }
    }
    public class LinkCollection : Collection<Link>
    {
        private readonly LinkLabel owner;
        private bool implicitLink;
        internal LinkCollection(LinkLabel owner)=>this.owner=owner;
        internal void SetDefault() { Add(new Link(0,-1)); implicitLink=true; }
        public Link Add(int start,int length)=>Add(start,length,null);
        public Link Add(int start,int length,object? data) { var link=new Link(start,length,data); Add(link); return link; }
        public new int Add(Link value) { base.Add(value); return IndexOf(value); }
        public Link? this[string name]=>this.FirstOrDefault(item=>string.Equals(item.Name,name,StringComparison.OrdinalIgnoreCase));
        protected override void InsertItem(int index,Link item)
        {
            if(item.Owner!=null) throw new ArgumentException("The link already has an owner.");
            if(implicitLink) { Clear(); index=0; }
            base.InsertItem(index,item); item.Owner=owner; owner.RenderLinks();
        }
        protected override void RemoveItem(int index)
        {
            this[index].Owner=null; implicitLink=false; base.RemoveItem(index); owner.RenderLinks();
        }
        protected override void ClearItems()
        {
            foreach(var link in this) link.Owner=null; implicitLink=false; base.ClearItems(); owner.RenderLinks();
        }
        protected override void SetItem(int index,Link item)
        {
            if(item.Owner!=null) throw new ArgumentException("The link already has an owner.");
            this[index].Owner=null; item.Owner=owner; implicitLink=false; base.SetItem(index,item); owner.RenderLinks();
        }
    }
}
