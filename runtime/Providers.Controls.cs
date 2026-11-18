using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

// Unattached controls have no AppKit view yet. Keep extender values across handle recreation.
internal sealed class ProviderPeer : IDisposable
{
    private readonly Control control;
    private readonly Func<IntPtr> attach;
    private readonly Action<IntPtr> release;
    private readonly Action<Control> forget;
    internal IntPtr Pointer { get; private set; }
    internal ProviderPeer(Control control, Func<IntPtr> attach, Action<IntPtr> release, Action<Control> forget)
    {
        this.control=control; this.attach=attach; this.release=release; this.forget=forget;
        control.HandleCreated+=Created; control.ParentChanged+=Reparented;
        control.HandleDestroyed+=Destroyed; control.Disposed+=Disposed;
        if (control.IsHandleCreated) Created(control,EventArgs.Empty);
    }
    private void Created(object? sender,EventArgs e) { if (Pointer==0) Pointer=attach(); }
    private void Destroyed(object? sender,EventArgs e) { if (Pointer!=0) release(Pointer); Pointer=0; }
    private void Reparented(object? sender,EventArgs e)
    {
        Destroyed(sender,e);
        if (control.IsHandleCreated) Created(sender,e);
    }
    private void Disposed(object? sender,EventArgs e) => forget(control);
    public void Dispose()
    {
        control.HandleCreated-=Created; control.ParentChanged-=Reparented;
        control.HandleDestroyed-=Destroyed; control.Disposed-=Disposed; Destroyed(null,EventArgs.Empty);
    }
}

public delegate void PopupEventHandler(object? sender,PopupEventArgs e);
public class PopupEventArgs(IWin32Window associatedWindow,Control? associatedControl,bool isBalloon,Size size) : CancelEventArgs
{
    public IWin32Window AssociatedWindow { get; }=associatedWindow;
    public Control? AssociatedControl { get; }=associatedControl;
    public bool IsBalloon { get; }=isBalloon;
    public Size ToolTipSize { get; set; }=size;
}

[ProvideProperty("ToolTip",typeof(Control))]
public class ToolTip : Component,IExtenderProvider
{
    private delegate int BeforePopup(ref int width,ref int height);
    [DllImport("MacForms")] private static extern IntPtr mf_tip_attach(IntPtr view,BeforePopup before);
    [DllImport("MacForms")] private static extern void mf_tip_config(IntPtr peer,string options);
    [DllImport("MacForms")] private static extern void mf_tip_show(IntPtr peer,string text,int x,int y,int duration);
    [DllImport("MacForms")] private static extern void mf_tip_hide(IntPtr peer);
    [DllImport("MacForms")] private static extern void mf_tip_release(IntPtr peer);
    private sealed class Entry
    {
        internal string Text="";
        internal required BeforePopup Before;
        internal ProviderPeer? Peer;
    }
    private readonly Dictionary<Control,Entry> entries=new();
    private bool active=true,showAlways,strip,balloon,animation=true,fading=true;
    private int delay=500,initial=500,reshow=100,duration=5000;
    private string title="";
    private Color fore=SystemColors.InfoText,back=SystemColors.Info;
    private ToolTipIcon icon;
    public ToolTip() {}
    public ToolTip(IContainer cont) => cont.Add(this);
    public object? Tag { get; set; }
    public event PopupEventHandler? Popup;
    public bool CanExtend(object target) => target is Control;
    public bool Active { get => active; set { active=value; Refresh(); } }
    public bool ShowAlways { get => showAlways; set { showAlways=value; Refresh(); } }
    public bool StripAmpersands { get => strip; set { strip=value; Refresh(); } }
    public bool IsBalloon { get => balloon; set { balloon=value; Refresh(); } }
    public bool UseAnimation { get => animation; set { animation=value; Refresh(); } }
    public bool UseFading { get => fading; set { fading=value; Refresh(); } }
    public int AutomaticDelay
    {
        get => delay;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); delay=initial=value; duration=(int)Math.Min(int.MaxValue,10L*value); reshow=value/5; Refresh(); }
    }
    public int InitialDelay { get => initial; set { ArgumentOutOfRangeException.ThrowIfNegative(value); initial=value; Refresh(); } }
    public int ReshowDelay { get => reshow; set { ArgumentOutOfRangeException.ThrowIfNegative(value); reshow=value; Refresh(); } }
    public int AutoPopDelay { get => duration; set { ArgumentOutOfRangeException.ThrowIfNegative(value); duration=value; Refresh(); } }
    public ToolTipIcon ToolTipIcon { get => icon; set { icon=value; Refresh(); } }
    public string ToolTipTitle { get => title; set { title=value??""; Refresh(); } }
    public Color BackColor { get => back; set { back=value; Refresh(); } }
    public Color ForeColor { get => fore; set { fore=value; Refresh(); } }
    public string GetToolTip(Control control) => entries.TryGetValue(control,out var item) ? item.Text : "";
    private void Configure(Entry entry,IntPtr pointer)
    {
        if(pointer==0) return;
        mf_tip_config(pointer,JsonSerializer.Serialize(new { text=entry.Text, active,showAlways,strip,balloon,animation,fading,
            initial,reshow,duration,title,icon,back=back.ToArgb(),fore=fore.ToArgb() }));
    }
    private void Refresh() { foreach(var entry in entries.Values) Configure(entry,entry.Peer?.Pointer??0); }
    private Entry Get(Control control)
    {
        if(entries.TryGetValue(control,out var entry)) return entry;
        entry=new Entry { Before=(ref int w,ref int h) =>
        {
            var args=new PopupEventArgs(control,control,balloon,new(w,h));
            Popup?.Invoke(this,args);
            w=args.ToolTipSize.Width; h=args.ToolTipSize.Height;
            return args.Cancel ? 0:1;
        }};
        entries.Add(control,entry);
        entry.Peer=new ProviderPeer(control,() =>
        {
            IntPtr ptr=mf_tip_attach(control.ViewHandle,entry.Before);
            Configure(entry,ptr); return ptr;
        },mf_tip_release,Forget);
        return entry;
    }
    private void Forget(Control c) { if (entries.Remove(c,out var entry)) entry.Peer?.Dispose(); }
    public void SetToolTip(Control control,string? caption)
    {
        ArgumentNullException.ThrowIfNull(control);
        if(string.IsNullOrEmpty(caption)) { Forget(control); return; }
        var entry=Get(control); entry.Text=caption; Configure(entry,entry.Peer!.Pointer);
    }
    public void Show(string text,IWin32Window window) => Show(text,window,0,-1,0);
    public void Show(string text,IWin32Window window,int duration) => Show(text,window,0,-1,duration);
    public void Show(string text,IWin32Window window,Point point) => Show(text,window,point.X,point.Y,0);
    public void Show(string text,IWin32Window window,Point point,int duration) => Show(text,window,point.X,point.Y,duration);
    public void Show(string text,IWin32Window window,int x,int y) => Show(text,window,x,y,0);
    public void Show(string text,IWin32Window window,int x,int y,int duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(duration);
        if(active && Control.FromHandle(window.Handle) is {} c) mf_tip_show(Get(c).Peer!.Pointer,text,x,y,duration);
    }
    public void Hide(IWin32Window window)
    {
        if(Control.FromHandle(window.Handle) is {} c && entries.TryGetValue(c,out var entry)) mf_tip_hide(entry.Peer!.Pointer);
    }
    public void RemoveAll() { foreach(var c in entries.Keys.ToArray()) Forget(c); }
    protected override void Dispose(bool disposing) { if(disposing) RemoveAll(); base.Dispose(disposing); }
}
