using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms;

public enum FixedPanel { None, Panel1, Panel2 }
public delegate void SplitterEventHandler(object? sender,SplitterEventArgs e);
public delegate void SplitterCancelEventHandler(object? sender,SplitterCancelEventArgs e);
public class SplitterEventArgs(int x,int y,int splitX,int splitY) : EventArgs
{
    public int X { get; }=x;
    public int Y { get; }=y;
    public int SplitX { get; set; }=splitX;
    public int SplitY { get; set; }=splitY;
}
public class SplitterCancelEventArgs(int x,int y,int splitX,int splitY) : CancelEventArgs
{
    public int X { get; }=x;
    public int Y { get; }=y;
    public int SplitX { get; set; }=splitX;
    public int SplitY { get; set; }=splitY;
}
public sealed class SplitterPanel : Panel
{
    public SplitterPanel(SplitContainer owner) { ArgumentNullException.ThrowIfNull(owner); }
}

public class SplitContainer : ContainerControl, ISupportInitialize
{
    internal override int Kind=>35;
    [System.Runtime.InteropServices.DllImport("MacForms")] internal static extern void mf_split_divider(IntPtr h,int x,int y,int w,int height,int vertical);
    private Orientation orientation = Orientation.Vertical;
    private int distance=50, splitterWidth=4, min1=25, min2=25, increment=1;
    private bool collapsed1, collapsed2, initializing;
    private int? dragOffset;
    public SplitContainer()
    {
        Size = new(150,100);
        Panel1 = new(this); Panel2=new(this);
        Controls.AddRange(new Control[]{Panel1,Panel2});
        Arrange(Size.Empty);
    }
    public SplitterPanel Panel1 { get; }
    public SplitterPanel Panel2 { get; }
    public Orientation Orientation { get => orientation; set { orientation=value; Arrange(Size.Empty); } }
    public FixedPanel FixedPanel { get; set; }
    private bool splitterFixed;
    public bool IsSplitterFixed { get=>splitterFixed; set { splitterFixed=value; Arrange(Size.Empty); } }
    public int Panel1MinSize { get=>min1; set { ArgumentOutOfRangeException.ThrowIfNegative(value); min1=value; Arrange(Size.Empty); } }
    public int Panel2MinSize { get=>min2; set { ArgumentOutOfRangeException.ThrowIfNegative(value); min2=value; Arrange(Size.Empty); } }
    public bool Panel1Collapsed { get=>collapsed1; set { collapsed1=value; if (value) collapsed2=false; Arrange(Size.Empty); } }
    public bool Panel2Collapsed { get=>collapsed2; set { collapsed2=value; if (value) collapsed1=false; Arrange(Size.Empty); } }
    public int SplitterWidth { get=>splitterWidth; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); splitterWidth=value; Arrange(Size.Empty); } }
    public int SplitterIncrement { get=>increment; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); increment=value; } }
    public int SplitterDistance
    {
        get=>distance;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            int next=initializing ? value : ClampDistance(value);
            if (distance == next) return;
            distance=next; Arrange(Size.Empty);
            SplitterMoved?.Invoke(this,new(0,0,Vertical ? distance : 0,Vertical ? 0 : distance));
        }
    }
    public Rectangle SplitterRectangle => collapsed1 || collapsed2 ? Rectangle.Empty :
        Vertical ? new(distance,0,splitterWidth,Height) : new(0,distance,Width,splitterWidth);
    public event SplitterCancelEventHandler? SplitterMoving;
    public event SplitterEventHandler? SplitterMoved;
    private bool Vertical => orientation == Orientation.Vertical;
    private int Length => Vertical ? Width : Height;
    private int ClampDistance(int candidate)
    {
        int available=Math.Max(0,Length-splitterWidth);
        return Math.Clamp(candidate,Math.Min(min1,available),Math.Max(Math.Min(min1,available),available-min2));
    }
    internal override void Arrange(Size delta)
    {
        if (initializing) return;
        Panel1.Visible=!collapsed1; Panel2.Visible=!collapsed2;
        if (collapsed1 || collapsed2)
        {
            if(NativeHandle!=0) mf_split_divider(NativeHandle,0,0,0,0,Vertical ? 1 : 0);
            (collapsed1 ? Panel2 : Panel1).Bounds = ClientRectangle;
            return;
        }
        int change=Vertical ? delta.Width : delta.Height;
        if (change != 0)
        {
            int old=Length-change-splitterWidth;
            if (FixedPanel == FixedPanel.Panel2) distance+=change;
            else if (FixedPanel == FixedPanel.None && old > 0)
                distance=(int)Math.Round((double)distance*(Length-splitterWidth)/old);
        }
        distance=ClampDistance(distance);
        int remainder=Math.Max(0,Length-distance-splitterWidth);
        Panel1.Bounds=Vertical ? new(0,0,distance,Height) : new(0,0,Width,distance);
        Panel2.Bounds=Vertical ? new(distance+splitterWidth,0,remainder,Height) : new(0,distance+splitterWidth,Width,remainder);
        var r=splitterFixed ? Rectangle.Empty : SplitterRectangle;
        if(NativeHandle!=0) mf_split_divider(NativeHandle,r.X,r.Y,r.Width,r.Height,Vertical ? 1 : 0);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(e.Handled || IsSplitterFixed || Panel1Collapsed || Panel2Collapsed) return;
        int step=e.KeyCode==(Vertical ? Keys.Left : Keys.Up) ? -increment : e.KeyCode==(Vertical ? Keys.Right : Keys.Down) ? increment : 0;
        if(step!=0) { SplitterDistance=ClampDistance(distance+step); e.Handled=true; }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!IsSplitterFixed && SplitterRectangle.Contains(e.Location))
            dragOffset=(Vertical ? e.X : e.Y)-distance;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragOffset == null || IsSplitterFixed) return;
        int requested=(Vertical ? e.X : e.Y)-dragOffset.Value;
        int next=ClampDistance(distance + (requested-distance)/increment*increment);
        if (next == distance) return;
        var args=new SplitterCancelEventArgs(e.X,e.Y,Vertical ? next : 0,Vertical ? 0 : next);
        SplitterMoving?.Invoke(this,args);
        if (!args.Cancel) SplitterDistance=next;
    }
    protected override void OnMouseUp(MouseEventArgs e) { dragOffset=null; base.OnMouseUp(e); }
    public void BeginInit() => initializing=true;
    public void EndInit() { initializing=false; Arrange(Size.Empty); }
}
