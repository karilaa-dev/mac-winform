using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public partial class Form
{
    [DllImport("MacForms")] private static extern int mf_form_state(IntPtr handle,int state);
    [DllImport("MacForms")] private static extern void mf_form_appearance(IntPtr handle,int topMost,double opacity,int border,int controlBox,int minimize,int maximize,int minWidth,int minHeight);
    private FormWindowState state;
    private bool topMost, controlBox = true, minimize = true, maximize = true;
    public bool MinimizeBox { get => minimize; set { minimize = value; UpdateAppearance(); } }
    public bool MaximizeBox { get => maximize; set { maximize = value; UpdateAppearance(); } }
    private double opacity = 1.0;
    private FormBorderStyle borderStyle = FormBorderStyle.Sizable;
    private Rectangle restore;
    public bool TopMost { get => topMost; set { topMost = value; UpdateAppearance(); } }
    public double Opacity { get => opacity; set { opacity = Math.Clamp(value,0,1); UpdateAppearance(); } }
    public bool ControlBox { get => controlBox; set { controlBox = value; UpdateAppearance(); } }
    public FormBorderStyle FormBorderStyle { get => borderStyle; set { borderStyle = value; UpdateAppearance(); SetWindowIcon(); } }
    public Rectangle RestoreBounds => WindowState == FormWindowState.Normal ? Bounds : restore;
    public FormWindowState WindowState
    {
        get => NativeHandle == 0 || IsMdiChild ? state : (FormWindowState)mf_form_state(NativeHandle,-1);
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (WindowState == value) return;
            if (WindowState == FormWindowState.Normal) restore = Bounds;
            state = value;
            if (loaded) ApplyWindowState();
        }
    }
    private void UpdateAppearance()
    {
        if (NativeHandle != 0) mf_form_appearance(NativeHandle,topMost ? 1 : 0,opacity,(int)borderStyle,controlBox ? 1 : 0,minimize ? 1 : 0,maximize ? 1 : 0,MinimumSize.Width,MinimumSize.Height);
    }
    private void ApplyWindowState()
    {
        if (!IsMdiChild) { mf_form_state(NativeHandle,(int)state); return; }
        if (state == FormWindowState.Maximized && Parent != null) Bounds = Parent.ClientRectangle;
        else if (state == FormWindowState.Minimized && Parent != null)
            Bounds = new(Left,Math.Max(0,Parent.Height-24),Math.Min(156,Parent.Width),24);
        else if (!restore.IsEmpty) Bounds = restore;
    }
    public void CenterToScreen() { if (!IsMdiChild) { CreateControl(); Native.mf_center(NativeHandle); } }
}
