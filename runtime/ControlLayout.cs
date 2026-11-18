using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public partial class Control
{
    private bool performingLayout;
    public event LayoutEventHandler? Layout;
    public event EventHandler? ClientSizeChanged, SizeChanged, LocationChanged, Move, EnabledChanged, VisibleChanged;
    protected virtual void OnLayout(LayoutEventArgs e) => Layout?.Invoke(this,e);
    protected virtual void OnResize(EventArgs e) => Resize?.Invoke(this,e);
    protected virtual void OnClientSizeChanged(EventArgs e) => ClientSizeChanged?.Invoke(this,e);
    protected virtual void OnSizeChanged(EventArgs e) { OnResize(e); SizeChanged?.Invoke(this,e); }
    protected virtual void OnLocationChanged(EventArgs e) { OnMove(e); LocationChanged?.Invoke(this,e); }
    protected virtual void OnMove(EventArgs e) => Move?.Invoke(this,e);
    protected virtual void OnEnabledChanged(EventArgs e) => EnabledChanged?.Invoke(this,e);
    protected virtual void OnVisibleChanged(EventArgs e) => VisibleChanged?.Invoke(this,e);
    public void PerformLayout(Control? affectedControl,string? affectedProperty)
    {
        if (NativeHandle == 0 || layoutDepth != 0 || performingLayout || Disposing) return;
        performingLayout = true;
        try
        {
            OnLayout(new(affectedControl,affectedProperty));
            Arrange(Size.Empty);
            if (this is ScrollableControl scroll && scroll.AutoScroll) scroll.SyncScroll();
        }
        finally { performingLayout = false; }
    }
    public void SetBounds(int x,int y,int width,int height,BoundsSpecified specified)
    {
        SetBounds(specified.HasFlag(BoundsSpecified.X) ? x : Left,specified.HasFlag(BoundsSpecified.Y) ? y : Top,
            specified.HasFlag(BoundsSpecified.Width) ? width : Width,specified.HasFlag(BoundsSpecified.Height) ? height : Height);
    }
    [DllImport("MacForms")] private static extern IntPtr mf_view_bitmap(IntPtr view,out int size);
    [DllImport("MacForms",EntryPoint="mf_free")] private static extern void FreeBitmap(IntPtr ptr);
    public void DrawToBitmap(Bitmap bitmap,Rectangle targetBounds)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (targetBounds.X < 0 || targetBounds.Y < 0 || targetBounds.Width < 1 || targetBounds.Height < 1)
            throw new ArgumentException("The target rectangle must have a positive size and a nonnegative origin.",nameof(targetBounds));
        var ptr = mf_view_bitmap(ViewHandle,out int size);
        if (ptr == IntPtr.Zero) return;
        try
        {
            var bytes = new byte[size];
            Marshal.Copy(ptr,bytes,0,size);
            using var stream = new MemoryStream(bytes);
            using var image = new Bitmap(stream);
            using var g = Graphics.FromImage(bitmap);
            g.SetClip(targetBounds);
            g.DrawImage(image,new Rectangle(targetBounds.Location,Size));
        }
        finally { FreeBitmap(ptr); }
    }
}
