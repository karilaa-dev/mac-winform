namespace System.Windows.Forms;

public class Splitter : Control
{
    internal override int Kind=>35;
    internal override void Arrange(System.Drawing.Size delta)
    {
        if(NativeHandle!=0) SplitContainer.mf_split_divider(NativeHandle,0,0,Width,Height,Vertical ? 1 : 0);
    }
    private int? dragOrigin;
    private int originalSize;
    private int minimum=25,extra=25;
    public Splitter() { Dock=DockStyle.Left; Width=3; TabStop=false; }
    public int MinSize { get=>minimum; set=>minimum=Math.Max(0,value); }
    public int MinExtra { get=>extra; set=>extra=Math.Max(0,value); }
    private bool Vertical=>Dock is DockStyle.Left or DockStyle.Right;
    private Control? Neighbor()
    {
        if(Parent==null) return null;
        return Parent.Controls.FirstOrDefault(c=>c!=this && c.Visible && (Dock switch {
            DockStyle.Left=>c.Right==Left && c.Top<Bottom && c.Bottom>Top,
            DockStyle.Right=>c.Left==Right && c.Top<Bottom && c.Bottom>Top,
            DockStyle.Top=>c.Bottom==Top && c.Left<Right && c.Right>Left,
            DockStyle.Bottom=>c.Top==Bottom && c.Left<Right && c.Right>Left,
            _=>false }));
    }
    public int SplitPosition
    {
        get { var neighbor=Neighbor(); return neighbor==null ? -1 : Vertical ? neighbor.Width : neighbor.Height; }
        set
        {
            var neighbor=Neighbor();
            if(neighbor==null || Parent==null) return;
            int available=Vertical ? Parent.ClientSize.Width-Width : Parent.ClientSize.Height-Height;
            int max=Math.Max(0,available-extra);
            int next=Math.Clamp(value,Math.Min(minimum,max),max);
            if(next==SplitPosition) return;
            if(Vertical) neighbor.Width=next; else neighbor.Height=next;
            Parent.PerformLayout(); SplitterMoved?.Invoke(this,new(Left,Top,Left,Top));
        }
    }
    public event SplitterEventHandler? SplitterMoving, SplitterMoved;
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        originalSize=SplitPosition;
        if(originalSize>=0) dragOrigin=Vertical ? Left+e.X : Top+e.Y;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if(!dragOrigin.HasValue) return;
        int x=Left+e.X,y=Top+e.Y;
        var args=new SplitterEventArgs(x,y,x,y);
        SplitterMoving?.Invoke(this,args);
        int delta=(Vertical ? args.SplitX : args.SplitY)-dragOrigin.Value;
        SplitPosition=originalSize+(Dock is DockStyle.Right or DockStyle.Bottom ? -delta : delta);
    }
    protected override void OnMouseUp(MouseEventArgs e) { dragOrigin=null; base.OnMouseUp(e); }
}
