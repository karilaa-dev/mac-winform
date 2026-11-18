using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public sealed class PaintEventArgs(Graphics graphics,Rectangle clip) : EventArgs
{
    public Graphics Graphics { get; } = graphics;
    public Rectangle ClipRectangle { get; } = clip;
}
[Flags] public enum MouseButtons { None=0, Left=0x100000, Right=0x200000, Middle=0x400000, XButton1=0x800000, XButton2=0x1000000 }
public sealed class MouseEventArgs(MouseButtons button,int clicks,int x,int y,int delta) : EventArgs
{
    public MouseButtons Button { get; } = button;
    public int Clicks { get; } = clicks;
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Delta { get; } = delta;
    public Point Location => new(X,Y);
}
public partial class Control
{
    [DllImport("MacForms")] private static extern void mf_invalidate(IntPtr h);
    [DllImport("MacForms")] private static extern void mf_mouse_position(IntPtr h,out int x,out int y);
    private Color backColor;
    public virtual Color BackColor { get => backColor; set { backColor = value; Invalidate(); } }
    public Rectangle ClientRectangle => new(Point.Empty,ClientSize);
    public event PaintEventHandler? Paint;
    public event MouseEventHandler? MouseDown, MouseMove, MouseUp;
    public void Invalidate() { if (NativeHandle != 0) mf_invalidate(NativeHandle); }
    protected virtual void OnPaint(PaintEventArgs e) => Paint?.Invoke(this,e);
    protected virtual void OnMouseDown(MouseEventArgs e) => MouseDown?.Invoke(this,e);
    protected virtual void OnMouseMove(MouseEventArgs e) => MouseMove?.Invoke(this,e);
    protected virtual void OnMouseUp(MouseEventArgs e) => MouseUp?.Invoke(this,e);
    private void DispatchCanvas(int ev)
    {
        if (ev == 8)
        {
            using var graphics = new Graphics(ClientRectangle);
            if (!backColor.IsEmpty) graphics.Clear(backColor);
            OnPaint(new(graphics,ClientRectangle));
            return;
        }
        mf_mouse_position(NativeHandle,out int x,out int y);
        var args = new MouseEventArgs(MouseButtons.Left,1,x,y,0);
        if (ev == 9) OnMouseDown(args);
        if (ev == 10) OnMouseMove(args);
        if (ev == 11) OnMouseUp(args);
    }
}
public class UserControl : ContainerControl { }
public enum PictureBoxSizeMode { Normal, StretchImage, AutoSize, CenterImage, Zoom }
public class PictureBox : Control, System.ComponentModel.ISupportInitialize
{
    private Image? image;
    private PictureBoxSizeMode sizeMode;
    public Image? Image { get => image; set { image = value; UpdateImage(); } }
    public PictureBoxSizeMode SizeMode { get => sizeMode; set { sizeMode = value; UpdateImage(); } }
    private void UpdateImage() { if (SizeMode == PictureBoxSizeMode.AutoSize && image != null) Size = image.Size; Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (image != null)
        {
            Size fitted = image.Size;
            if (SizeMode == PictureBoxSizeMode.StretchImage) fitted = ClientSize;
            if (SizeMode == PictureBoxSizeMode.Zoom)
            {
                double scale = Math.Min((double)Width/image.Width,(double)Height/image.Height);
                fitted = new((int)(image.Width*scale),(int)(image.Height*scale));
            }
            bool center = SizeMode is PictureBoxSizeMode.CenterImage or PictureBoxSizeMode.Zoom;
            e.Graphics.DrawImage(image,center ? (Width-fitted.Width)/2 : 0,center ? (Height-fitted.Height)/2 : 0,fitted.Width,fitted.Height);
        }
        base.OnPaint(e);
    }
    public void BeginInit() {}
    public void EndInit() => UpdateImage();
}
