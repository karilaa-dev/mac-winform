using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public sealed class NotifyIcon : Component
{
    private delegate void StatusEvent(IntPtr peer,int phase,int button,int clicks,int x,int y);
    private static readonly StatusEvent callback = Receive;
    // Like the Windows tray, a displayed item survives local variable scope until hidden or disposed.
    private static readonly Dictionary<IntPtr,NotifyIcon> active = new();
    [DllImport("MacForms")] private static extern IntPtr mf_status_create(StatusEvent callback);
    [DllImport("MacForms")] private static extern void mf_status_destroy(IntPtr peer);
    [DllImport("MacForms")] private static extern void mf_status_update(IntPtr peer,ImageHandle image,string text);
    [DllImport("MacForms")] private static extern void mf_status_menu(IntPtr peer,IntPtr menu);
    [DllImport("MacForms")] private static extern void mf_status_balloon(IntPtr peer,int timeout,string title,string text,int icon);

    private IntPtr peer;
    private Icon? icon;
    private string text="", tipText="", tipTitle="";
    private bool visible,disposed;
    private ToolTipIcon tipIcon;
    public NotifyIcon() {}
    public NotifyIcon(IContainer container) { ArgumentNullException.ThrowIfNull(container); container.Add(this); }
    public Icon? Icon { get => icon; set { icon=value; Sync(); } }
    public bool Visible { get => visible; set { visible=value; Sync(); } }
    public string Text
    {
        get => text;
        set
        {
            value ??= "";
            if (value.Length>127) throw new ArgumentOutOfRangeException(nameof(value));
            text=value; Sync();
        }
    }
    public object? Tag { get; set; }
    public ContextMenuStrip? ContextMenuStrip { get; set; }
    public string BalloonTipText { get => tipText; set => tipText=value ?? ""; }
    public string BalloonTipTitle { get => tipTitle; set => tipTitle=value ?? ""; }
    public ToolTipIcon BalloonTipIcon
    {
        get => tipIcon;
        set { ValidateIcon(value); tipIcon=value; }
    }
    private static void ValidateIcon(ToolTipIcon value)
    { if (!Enum.IsDefined(value)) throw new InvalidEnumArgumentException(nameof(value),(int)value,typeof(ToolTipIcon)); }
    public event EventHandler? Click,DoubleClick,BalloonTipClicked,BalloonTipClosed,BalloonTipShown;
    public event MouseEventHandler? MouseClick,MouseDoubleClick,MouseDown,MouseUp,MouseMove;
    private void Sync()
    {
        if (!visible || icon==null || disposed)
        {
            var previous=peer;
            if (previous!=0)
            {
                peer=0;
                active.Remove(previous);
                mf_status_destroy(previous);
            }
            return;
        }
        if (peer==0)
        {
            Application.Initialize();
            peer=mf_status_create(callback);
            if (peer==0) throw new InvalidOperationException("AppKit could not create the status item.");
            active.Add(peer,this);
        }
        mf_status_update(peer,icon.Bitmap.Handle,text);
    }
    public void ShowBalloonTip(int timeout) => ShowBalloonTip(timeout,BalloonTipTitle,BalloonTipText,BalloonTipIcon);
    public void ShowBalloonTip(int timeout,string tipTitle,string tipText,ToolTipIcon tipIcon)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(timeout);
        ArgumentException.ThrowIfNullOrEmpty(tipText);
        ValidateIcon(tipIcon);
        if (peer!=0) mf_status_balloon(peer,timeout,tipTitle ?? "",tipText,(int)tipIcon);
    }
    private static void Receive(IntPtr handle,int phase,int button,int clicks,int x,int y)
    {
        if (!active.TryGetValue(handle,out var item)) return;
        var mouse = new MouseEventArgs(button switch { 1=>MouseButtons.Left,2=>MouseButtons.Right,3=>MouseButtons.Middle,_=>MouseButtons.None },phase==1 ? clicks : 0,x,y,0);
        switch (phase)
        {
            case 1:
                if (clicks==2)
                {
                    item.DoubleClick?.Invoke(item,mouse);
                    item.MouseDoubleClick?.Invoke(item,mouse);
                }
                item.MouseDown?.Invoke(item,mouse);
                break;
            case 2:
                if (button==2 && item.ContextMenuStrip is { IsDisposed:false } menu)
                {
                    menu.SourceControl=null;
                    mf_status_menu(handle,menu.Handle);
                    if (item.peer!=handle) break;
                }
                item.MouseUp?.Invoke(item,mouse);
                if (clicks!=2)
                {
                    item.Click?.Invoke(item,mouse);
                    item.MouseClick?.Invoke(item,mouse);
                }
                break;
            case 3: item.MouseMove?.Invoke(item,mouse); break;
            case 4: item.BalloonTipShown?.Invoke(item,EventArgs.Empty); break;
            case 5: item.BalloonTipClosed?.Invoke(item,EventArgs.Empty); break;
            case 6: item.BalloonTipClicked?.Invoke(item,EventArgs.Empty); break;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !disposed)
        {
            disposed=true;
            visible=false;
            Sync();
            icon=null;
            ContextMenuStrip=null;
        }
        base.Dispose(disposing);
    }
}
