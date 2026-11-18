using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public class ScrollableControl : Control
{
    [DllImport("MacForms")] private static extern void mf_scroll_config(IntPtr handle,int enabled,int w,int h);
    [DllImport("MacForms")] private static extern void mf_scroll_position(IntPtr handle,int set,ref int x,ref int y);
    private bool autoScroll;
    private Size minSize, margin;
    private Point requested;
    public virtual bool AutoScroll
    {
        get => autoScroll;
        set { if (autoScroll != value) { autoScroll = value; SyncScroll(); } }
    }
    public Size AutoScrollMinSize
    {
        get => minSize;
        set { minSize = value; AutoScroll = true; SyncScroll(); }
    }
    public Size AutoScrollMargin { get => margin; set { margin = value; SyncScroll(); } }
    public void SetAutoScrollMargin(int x,int y) => AutoScrollMargin = new(Math.Max(0,x),Math.Max(0,y));
    public Point AutoScrollPosition
    {
        get
        {
            if (NativeHandle == 0) return Point.Empty;
            int x=0,y=0; mf_scroll_position(NativeHandle,0,ref x,ref y); return new(-x,-y);
        }
        set
        {
            requested = value;
            if (NativeHandle == 0) return;
            int x=Math.Max(0,value.X),y=Math.Max(0,value.Y); mf_scroll_position(NativeHandle,1,ref x,ref y);
        }
    }
    internal void SyncScroll()
    {
        if (NativeHandle == 0 || Disposing) return;
        int width = minSize.Width, height = minSize.Height;
        if (autoScroll)
            foreach (var child in Controls)
            {
                width = Math.Max(width,child.Right + margin.Width);
                height = Math.Max(height,child.Bottom + margin.Height);
            }
        mf_scroll_config(NativeHandle,autoScroll ? 1 : 0,width,height);
    }
    public void ScrollControlIntoView(Control activeControl)
    {
        if (!Contains(activeControl) || !AutoScroll) return;
        var pos = PointToClient(activeControl.PointToScreen(Point.Empty));
        var scroll = AutoScrollPosition;
        int dx = pos.X < 0 ? pos.X : Math.Max(0,pos.X+activeControl.Width-Width);
        int dy = pos.Y < 0 ? pos.Y : Math.Max(0,pos.Y+activeControl.Height-Height);
        AutoScrollPosition = new(-scroll.X+dx,-scroll.Y+dy);
    }
    internal override void ApplyNative() { base.ApplyNative(); SyncScroll(); AutoScrollPosition = requested; }
}
public partial class ContainerControl : ScrollableControl
{
    public AutoScaleMode AutoScaleMode { get; set; }
    public SizeF AutoScaleDimensions { get; set; }
}
public partial class Control
{
    [DllImport("MacForms")] private static extern void mf_coordinate(IntPtr h,int direction,ref int x,ref int y);
    private Point ConvertPoint(Point p,int direction)
    {
        int x = p.X,y = p.Y; mf_coordinate(Handle,direction,ref x,ref y); return new(x,y);
    }
    public Point PointToClient(Point p) => ConvertPoint(p,0);
    public Point PointToScreen(Point p) => ConvertPoint(p,1);
    public Rectangle RectangleToClient(Rectangle r) => new(PointToClient(r.Location),r.Size);
    public Rectangle RectangleToScreen(Rectangle r) => new(PointToScreen(r.Location),r.Size);
    public static Control? FromHandle(IntPtr handle) => peers.GetValueOrDefault(handle);
}
