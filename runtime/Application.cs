using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms;

public static partial class Application
{
    private static AppKitContext? context;
    internal static int UIThread { get; private set; }
    internal static void Initialize()
    {
        if (context != null) return;
        Native.mf_init();
        Control.InstallKeyDispatcher();
        UIThread = Environment.CurrentManagedThreadId;
        context = new AppKitContext();
        SynchronizationContext.SetSynchronizationContext(context);
    }
    internal static SynchronizationContext UIContext { get { Initialize(); return context!; } }
    private sealed class AppKitContext : SynchronizationContext
    {
        private static readonly ConcurrentQueue<(SendOrPostCallback Work,object? State)> pending = new();
        private static readonly Native.PostCallback process = () =>
        {
            if (pending.TryDequeue(out var item)) item.Work(item.State);
        };
        public override void Post(SendOrPostCallback d,object? state)
        {
            pending.Enqueue((d,state));
            Native.mf_post(process);
        }
        public override SynchronizationContext CreateCopy() => this;
    }
    public static Font DefaultFont { get; private set; } = new("Segoe UI",9);
    public static void SetDefaultFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);
        if (OpenForms.Count != 0) throw new InvalidOperationException("Set the default font before showing a form.");
        DefaultFont = font;
    }
    public static HighDpiMode HighDpiMode { get; private set; } = HighDpiMode.SystemAware;
    public static bool SetHighDpiMode(HighDpiMode highDpiMode)
    {
        if (OpenForms.Count != 0) return false;
        HighDpiMode = highDpiMode;
        return true;
    }
    public static void SetCompatibleTextRenderingDefault(bool defaultValue) { }
    internal static FormCollection Open => forms;
    public static void Run(Form form) => Run(new ApplicationContext(form));
    public static void Exit()
    {
        foreach (var form in OpenForms.Cast<Form>().ToArray())
        {
            form.Close();
            if (!form.Closed && !form.IsDisposed) return;
        }
        ExitThread();
    }
    public static void DoEvents() { Initialize(); Native.mf_pump(); }
}
public enum CloseReason { None, WindowsShutDown, MdiFormClosing, UserClosing, TaskManagerClosing, FormOwnerClosing, ApplicationExitCall }
public sealed class FormClosingEventArgs(CloseReason reason, bool cancel) : CancelEventArgs(cancel)
{
    public CloseReason CloseReason { get; } = reason;
}
public sealed class FormClosedEventArgs(CloseReason reason) : EventArgs
{
    public CloseReason CloseReason { get; } = reason;
}
public partial class Form : ContainerControl
{
    private bool loaded, modal;
    internal bool Closed { get; private set; }
    internal override int Kind => IsMdiChild ? 38 : 1;
    public Form() { ClientSize = new Size(300,300); }
    public FormStartPosition StartPosition { get; set; } = FormStartPosition.WindowsDefaultLocation;
    public MenuStrip? MainMenuStrip { get; set; }
    private IButtonControl? acceptButton;
    public IButtonControl? AcceptButton
    {
        get=>acceptButton;
        set
        {
            if (acceptButton==value) return;
            acceptButton?.NotifyDefault(false);
            acceptButton=value;
            value?.NotifyDefault(true);
            if (NativeHandle!=0 && !IsMdiChild) Native.mf_default_button(NativeHandle,(value as Button)?.Handle ?? 0);
        }
    }
    public IButtonControl? CancelButton { get; set; }
    public DialogResult DialogResult { get; set; }
    public event EventHandler? Load, Shown;
    public event FormClosingEventHandler? FormClosing;
    public event FormClosedEventHandler? FormClosed;

    protected virtual void OnLoad(EventArgs e) => Load?.Invoke(this,e);
    protected virtual void OnShown(EventArgs e) => Shown?.Invoke(this,e);
    protected virtual void OnFormClosing(FormClosingEventArgs e) => FormClosing?.Invoke(this,e);
    protected virtual void OnFormClosed(FormClosedEventArgs e) => FormClosed?.Invoke(this,e);

    public override void Show()
    {
        ApplyDesignerScale();
        CreateControl();
        Closed = false;
        Application.Open.Add(this);
        if (!loaded)
        {
            UpdateAppearance();
            if (AcceptButton is Button button) Native.mf_default_button(NativeHandle,button.Handle);
            if (!IsMdiChild && StartPosition != FormStartPosition.Manual) Native.mf_center(NativeHandle);
            loaded = true;
            OnLoad(EventArgs.Empty);
            if (Closed || IsDisposed) return;
            if (!IsMdiChild && (ActiveControl == null || !ActiveControl.Focus())) FocusPast();
            base.Show();
            if (state != FormWindowState.Normal) ApplyWindowState();
            if (IsMdiChild) Activate();
            OnShown(EventArgs.Empty);
        }
        else { base.Show(); if (IsMdiChild) Activate(); }
    }
    public DialogResult ShowDialog(Form? owner = null)
    {
        var enabled = owner?.Enabled == true;
        if (enabled) owner!.Enabled = false;
        try
        {
            modal = true;
            DialogResult = DialogResult.None;
            Show();
            while (!Closed && !IsDisposed)
            {
                if (DialogResult == DialogResult.None) Native.mf_pump();
                else Close();
            }
            return DialogResult;
        }
        finally { modal = false; if (enabled && !owner!.IsDisposed) owner.Enabled = true; }
    }
    public void Activate()
    {
        if (NativeHandle == 0) return;
        if (IsMdiChild) mf_mdi_activate(NativeHandle);
        else Native.mf_visible(NativeHandle,1);
    }
    public void Close()
    {
        if (Closed || IsDisposed) return;
        var children = MdiChildren;
        var args = new FormClosingEventArgs(CloseReason.UserClosing,false);
        foreach (var child in children)
        {
            var closing = new FormClosingEventArgs(CloseReason.MdiFormClosing,args.Cancel);
            child.OnFormClosing(closing);
            args.Cancel |= closing.Cancel;
        }
        OnFormClosing(args);
        if (args.Cancel) { DialogResult = DialogResult.None; return; }
        foreach (var child in children)
        {
            child.Closed = true;
            child.OnFormClosed(new(CloseReason.MdiFormClosing));
            child.Dispose();
        }
        Closed = true;
        if (modal && DialogResult == DialogResult.None) DialogResult = DialogResult.Cancel;
        Hide();
        Application.Open.Remove(this);
        OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));
        if (!modal) Dispose();
    }
    internal override void OnNative(int ev)
    {
        if (ev == 45) { MdiParent?.SetActiveChild(this); return; }
        if (ev == 46) { mf_mdi_location(NativeHandle,out int x,out int y); SetBoundsCore(new(new Point(x,y),Size),false); return; }
        if (ev == 5) { Close(); return; }
        base.OnNative(ev);
    }
    protected override void Dispose(bool disposing)
    {
        var mdiOwner = MdiParent;
        Application.Open.Remove(this);
        base.Dispose(disposing);
        if (mdiOwner?.ActiveMdiChild == this) mdiOwner.SetActiveChild(mdiOwner.MdiChildren.LastOrDefault(c=>!c.IsDisposed && c.loaded));
    }
}
public static class MessageBox
{
    public static DialogResult Show(string? text) => Show((IWin32Window?)null,text);
    public static DialogResult Show(string? text,string? caption) => Show(null,text,caption);
    public static DialogResult Show(string? text,string? caption,MessageBoxButtons buttons) => Show(null,text,caption,buttons);
    public static DialogResult Show(string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon) => Show(null,text,caption,buttons,icon);
    public static DialogResult Show(string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton) =>
        Show(null,text,caption,buttons,icon,defaultButton);
    public static DialogResult Show(string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton,MessageBoxOptions options) =>
        Show(null,text,caption,buttons,icon,defaultButton,options);
    public static DialogResult Show(IWin32Window? owner,string? text) => Show(owner,text,"");
    public static DialogResult Show(IWin32Window? owner,string? text,string? caption) => Show(owner,text,caption,MessageBoxButtons.OK);
    public static DialogResult Show(IWin32Window? owner,string? text,string? caption,MessageBoxButtons buttons) => Show(owner,text,caption,buttons,MessageBoxIcon.None);
    public static DialogResult Show(IWin32Window? owner,string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon) =>
        Show(owner,text,caption,buttons,icon,MessageBoxDefaultButton.Button1);
    public static DialogResult Show(IWin32Window? owner,string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton) =>
        Show(owner,text,caption,buttons,icon,defaultButton,0);
    public static DialogResult Show(IWin32Window? owner,string? text,string? caption,MessageBoxButtons buttons,MessageBoxIcon icon,MessageBoxDefaultButton defaultButton,MessageBoxOptions options)
    {
        if ((options & (MessageBoxOptions.ServiceNotification | MessageBoxOptions.DefaultDesktopOnly)) != 0)
            throw new PlatformNotSupportedException("A macOS desktop application cannot display a Windows service notification.");
        Application.Initialize();
        return (DialogResult)Native.mf_alert(CommonDialog.OwnerView(owner?.Handle ?? 0),text ?? "",caption ?? "",
            (int)buttons,(int)icon,(int)defaultButton >> 8,(int)options);
    }
}

public sealed class Timer : Component
{
    private int interval = 100, generation, queued;
    private Threading.Timer? timer;
    public Timer() {}
    public Timer(IContainer container) { container.Add(this); }
    public int Interval
    {
        get => interval;
        set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); interval = value; timer?.Change(value,value); }
    }
    public bool Enabled { get => timer != null; set { if (value) Start(); else Stop(); } }
    public event EventHandler? Tick;
    public void Start()
    {
        if (Enabled) return;
        var ui = Application.UIContext;
        int version = ++generation;
        timer = new Threading.Timer(_ =>
        {
            if (Interlocked.Exchange(ref queued,1) != 0) return;
            ui.Post(_ =>
            {
                Interlocked.Exchange(ref queued,0);
                if (version == generation) Tick?.Invoke(this,EventArgs.Empty);
            },null);
        },null,interval,interval);
    }
    public void Stop() { generation++; timer?.Dispose(); timer = null; }
    protected override void Dispose(bool disposing) { if (disposing) Stop(); base.Dispose(disposing); }
}
