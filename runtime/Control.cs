using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public partial class Control : Component
{
    private static readonly Dictionary<IntPtr,Control> peers = new();
    private static readonly Native.EventCallback callback = Dispatch;
    private string text = "";
    private bool visible = true, enabled = true;
    private Rectangle bounds = new(0, 0, 100, 23);
    private Font font = Application.DefaultFont;
    private Color foreColor;
    private ContentAlignment alignment = ContentAlignment.MiddleLeft;
    private int layoutDepth;
    private DockStyle dock;

    public Control() { Controls = CreateControlsInstance(); }
    protected virtual ControlCollection CreateControlsInstance() => new ControlCollection(this);
    internal virtual int Kind => 2;
    internal IntPtr NativeHandle { get; private set; }
    [DllImport("MacForms")] private static extern IntPtr mf_widget_view(IntPtr h);
    internal IntPtr ViewHandle => mf_widget_view(Handle);
    public IntPtr Handle { get { CreateControl(); return NativeHandle; } }
    public ControlCollection Controls { get; }
    private Control? parent;
    public Control? Parent
    {
        get => parent;
        set { if (value == null) parent?.Controls.Remove(this); else value.Controls.Add(this); }
    }
    public string Name { get; set; } = "";
    public object? Tag { get; set; }
    public virtual string Text
    {
        get => text;
        set
        {
            value ??= "";
            if (text == value) return;
            text = value;
            if (NativeHandle != 0) Native.mf_text(NativeHandle,value);
            FitText();
            OnTextChanged(EventArgs.Empty);
        }
    }
    public bool AutoSize { get; set; }
    internal void FitText()
    {
        if (AutoSize && this is ButtonBase button) { button.FitContents(); return; }
        if (AutoSize && this is Label)
            Size = new Size(Math.Max(12, (int)Math.Ceiling(Text.Length * Font.Size * .86) + (this is Label ? 4 : 28)), Math.Max(22, (int)Font.Size+12));
    }
    internal bool LayoutVisible => visible;
    public bool Visible
    {
        get => visible && (Parent?.Visible ?? true) && (this is not TabPage || Parent is not TabControl tabs || tabs.SelectedTab == this);
        set
        {
            bool changed = visible != value;
            visible = value;
            if (NativeHandle != 0) Native.mf_visible(NativeHandle,value ? 1 : 0);
            if (changed) { OnVisibleChanged(EventArgs.Empty); Parent?.PerformLayout(this,nameof(Visible)); }
        }
    }
    public bool Enabled
    {
        get => enabled && (Parent?.Enabled ?? true);
        set { if (enabled == value) return; enabled = value; RefreshEnabled(); OnEnabledChanged(EventArgs.Empty); }
    }
    private void RefreshEnabled()
    {
        if (NativeHandle == 0) return;
        Native.mf_enabled(NativeHandle, Enabled ? 1 : 0);
        foreach (var child in Controls) child.RefreshEnabled();
    }
    public Rectangle Bounds
    {
        get => bounds;
        set => SetBoundsCore(value, true);
    }
    internal virtual void SetBoundsCore(Rectangle value, bool updateNative)
    {
        value.Size = new Size(Math.Clamp(value.Width,MinimumSize.Width,Math.Max(MinimumSize.Width,MaximumSize.Width > 0 ? MaximumSize.Width : int.MaxValue)),
            Math.Clamp(value.Height,MinimumSize.Height,Math.Max(MinimumSize.Height,MaximumSize.Height > 0 ? MaximumSize.Height : int.MaxValue)));
        if (bounds == value) return;
        var previous = bounds;
        bounds = value;
        if (NativeHandle != 0)
        {
            if (updateNative) SetFrame();
            Arrange(value.Size - previous.Size);
            if (Parent is ScrollableControl owner && owner.AutoScroll) owner.SyncScroll();
            if (this is ScrollableControl scroll && scroll.AutoScroll) scroll.SyncScroll();
        }
        if (value.Location != previous.Location) OnLocationChanged(EventArgs.Empty);
        if (value.Size != previous.Size) { OnSizeChanged(EventArgs.Empty); OnClientSizeChanged(EventArgs.Empty); }
    }
    private void SetFrame() => Native.mf_frame(NativeHandle, Left, Top, Math.Max(0,Width), Math.Max(0,Height));
    public Point Location { get => bounds.Location; set => Bounds = new(value, Size); }
    public Size Size { get => bounds.Size; set => Bounds = new(Location, value); }
    public int Left { get => bounds.Left; set => Location = new(value,Top); }
    public int Top { get => bounds.Top; set => Location = new(Left,value); }
    public int Width { get => bounds.Width; set => Size = new(value, Height); }
    public int Height { get => bounds.Height; set => Size = new(Width,value); }
    public int Right => bounds.Right;
    public int Bottom => bounds.Bottom;
    public Size ClientSize { get => Size; set => Size = value; }
    public Size MinimumSize { get; set; }
    public Size MaximumSize { get; set; }
    public AnchorStyles Anchor { get; set; } = AnchorStyles.Top | AnchorStyles.Left;
    public DockStyle Dock { get => dock; set { dock = value; Parent?.PerformLayout(); } }
    public Padding Padding { get; set; }
    public Padding Margin { get; set; } = new(3);
    public int TabIndex { get; set; }
    public bool TabStop { get; set; } = true;
    public Font Font { get => font; set { font = value; Style(); } }
    public Color ForeColor { get => foreColor; set { foreColor = value; Style(); } }
    public virtual ContentAlignment TextAlign { get => alignment; set { alignment = value; Style(); } }
    private void Style()
    {
        if (NativeHandle != 0 && this is ButtonBase button) button.UpdateButton();
        if (NativeHandle != 0) Native.mf_style(NativeHandle,font.Name,font.SizeInPoints,font.Style.HasFlag(FontStyle.Bold) ? 1 : 0,foreColor.ToArgb(),(int)alignment);
    }
    public bool IsDisposed { get; private set; }
    public bool Disposing { get; private set; }
    public event EventHandler? Click, TextChanged, Resize;
    protected virtual void OnClick(EventArgs e) => Click?.Invoke(this,e);
    protected virtual void OnTextChanged(EventArgs e) => TextChanged?.Invoke(this,e);
    public void CreateControl()
    {
        if (NativeHandle != 0) return;
        ObjectDisposedException.ThrowIf(IsDisposed,this);
        Application.Initialize();
        FitText();
        NativeHandle = Native.mf_create(Kind,callback,text);
        if (NativeHandle == 0) throw new InvalidOperationException("AppKit could not create " + GetType().Name);
        peers.Add(NativeHandle,this);
        if (Parent?.NativeHandle is IntPtr p && p != 0) Native.mf_parent(NativeHandle,p);
        SetFrame();
        Style();
        Native.mf_enabled(NativeHandle,Enabled ? 1 : 0);
        if (Kind != 1) Native.mf_visible(NativeHandle,visible ? 1 : 0);
        foreach (var child in Controls)
        {
            if (child.NativeHandle == 0) child.CreateControl(); else Native.mf_parent(child.NativeHandle,NativeHandle);
        }
        ApplyNative();
        if (cursor != null) ApplyCursor();
        if (accessibleName != null || accessibleDescription != null || role != AccessibleRole.Default) ApplyAccessibility();
        if (contextMenu != null) UpdateContextMenu();
        if (allowDrop) mf_allow_drop(NativeHandle,1);
        PerformLayout();
        OnHandleCreated(EventArgs.Empty);
    }
    internal void RecreateWidget()
    {
        if (NativeHandle == 0) return;
        OnHandleDestroyed(EventArgs.Empty);
        peers.Remove(NativeHandle);
        Native.mf_destroy(NativeHandle);
        NativeHandle = 0;
        CreateControl();
    }
    internal virtual void ApplyNative() {}
    private static void Dispatch(IntPtr handle,int ev)
    {
        if (peers.TryGetValue(handle,out var control)) control.OnNative(ev);
    }
    internal void UpdateTextFromPeer(string value)
    {
        if (text == value) return;
        text = value;
        OnTextChanged(EventArgs.Empty);
    }
    internal virtual void OnNative(int ev)
    {
        if (ev >= 8 && ev <= 11) DispatchCanvas(ev);
        if (ev == 1) OnClick(EventArgs.Empty);
        if (ev == 2)
        {
            UpdateTextFromPeer(Marshal.PtrToStringUTF8(Native.mf_get_text(NativeHandle)) ?? "");
        }
        if (ev == 6 && this is Form)
            SetBoundsCore(new(Location,new Size(Native.mf_width(NativeHandle),Native.mf_height(NativeHandle))),false);
    }
    public bool InvokeRequired => Application.UIThread != 0 && Application.UIThread != Environment.CurrentManagedThreadId;
    public IAsyncResult BeginInvoke(Delegate method, params object?[]? args)
    {
        if (NativeHandle == 0) throw new InvalidOperationException("Create the control before invoking across threads.");
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Application.UIContext.Post(_ =>
        {
            try { completion.SetResult(method.DynamicInvoke(args)); }
            catch (Exception ex) { completion.SetException(ex.InnerException ?? ex); }
        },null);
        return completion.Task;
    }
    public object? EndInvoke(IAsyncResult result) => ((Task<object?>)result).GetAwaiter().GetResult();
    public object? Invoke(Delegate method, params object?[]? args) =>
        InvokeRequired ? EndInvoke(BeginInvoke(method,args)) : method.DynamicInvoke(args);
    public bool Focus() { if (!Enabled || !Visible) return false; CreateControl(); return Native.mf_focus(NativeHandle) != 0; }
    public virtual void Refresh() { if (NativeHandle != 0) { Invalidate(); Update(); } }
    [DllImport("MacForms")] private static extern void mf_update_view(IntPtr h);
    public void Update() { if (NativeHandle != 0) mf_update_view(NativeHandle); }
    public virtual void Show() { CreateControl(); Visible = true; }
    public void Hide() => Visible = false;
    public void SuspendLayout() => layoutDepth++;
    public void ResumeLayout(bool performLayout = true)
    {
        if (layoutDepth > 0) layoutDepth--;
        if (layoutDepth == 0 && this is Form form) form.ApplyDesignerScale();
        if (performLayout) PerformLayout();
    }
    public void PerformLayout() => PerformLayout(null,null);

    internal virtual void Arrange(Size delta)
    {
        var area = new Rectangle(Padding.Left, Padding.Top, Width - Padding.Left - Padding.Right, Height - Padding.Top - Padding.Bottom);
        if (this is GroupBox) { area.Y += 17; area.Height -= 17; }
        foreach (var child in Controls.Reverse())
        {
            var b = child.Bounds;
            switch (child.Dock)
            {
                case DockStyle.Fill: b = area; break;
                case DockStyle.Top: b = new(area.X,area.Y,area.Width,b.Height); area.Y += b.Height; area.Height -= b.Height; break;
                case DockStyle.Bottom: b = new(area.X,area.Bottom-b.Height,area.Width,b.Height); area.Height -= b.Height; break;
                case DockStyle.Left: b = new(area.X,area.Y,b.Width,area.Height); area.X += b.Width; area.Width -= b.Width; break;
                case DockStyle.Right: b = new(area.Right-b.Width,area.Y,b.Width,area.Height); area.Width -= b.Width; break;
                default:
                    if (child.Anchor.HasFlag(AnchorStyles.Right))
                    {
                        if (child.Anchor.HasFlag(AnchorStyles.Left)) b.Width += delta.Width; else b.X += delta.Width;
                    }
                    if (child.Anchor.HasFlag(AnchorStyles.Bottom))
                    {
                        if (child.Anchor.HasFlag(AnchorStyles.Top)) b.Height += delta.Height; else b.Y += delta.Height;
                    }
                    break;
            }
            child.Bounds = b;
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;
        Disposing = true;
        if (disposing)
        {
            foreach (var child in Controls.ToArray()) child.Dispose();
            Parent?.Controls.Remove(this);
            if (NativeHandle != 0) { OnHandleDestroyed(EventArgs.Empty); peers.Remove(NativeHandle); Native.mf_destroy(NativeHandle); NativeHandle = 0; }
        }
        IsDisposed = true;
        Disposing = false;
        base.Dispose(disposing);
    }
    public class ControlCollection : Collection<Control>
    {
        private readonly Control owner;
        internal ControlCollection(Control owner) { this.owner = owner; }
        public Control? this[string? key] => this.FirstOrDefault(c => !string.IsNullOrEmpty(key) && string.Equals(c.Name,key,StringComparison.OrdinalIgnoreCase));
        public bool ContainsKey(string? key) => this[key] != null;
        public int IndexOfKey(string? key) => this[key] is {} item ? IndexOf(item) : -1;
        public void RemoveByKey(string? key) { if (this[key] is {} item) Remove(item); }
        public Control[] Find(string key,bool searchAllChildren)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Specify a control name.",nameof(key));
            var found = this.Where(c=>string.Equals(c.Name,key,StringComparison.OrdinalIgnoreCase)).ToList();
            if (searchAllChildren) foreach (var c in this) found.AddRange(c.Controls.Find(key,true));
            return found.ToArray();
        }
        public void AddRange(Control[] controls) { foreach (var control in controls) Add(control); }
        public void Add(Control child,int column,int row)
        {
            if (owner is not TableLayoutPanel table) throw new InvalidOperationException("Cell coordinates require TableLayoutPanel");
            table.Positions[child] = (column,row);
            Add(child);
        }
        protected override void InsertItem(int index,Control child)
        {
            if (owner is TabControl && child is not TabPage) throw new ArgumentException("Tab controls accept only TabPage children.",nameof(child));
            for (Control? p = owner; p != null; p = p.Parent)
                if (p == child) throw new ArgumentException("A control cannot contain itself");
            if (child.Parent == owner) return;
            child.Parent?.Controls.Remove(child);
            child.parent = owner;
            base.InsertItem(index,child);
            child.OnParentChanged(EventArgs.Empty);
            owner.OnControlAdded(new ControlEventArgs(child));
            if (owner.NativeHandle != 0)
            {
                if (child.NativeHandle == 0) child.CreateControl(); else Native.mf_parent(child.NativeHandle,owner.NativeHandle);
                child.RefreshEnabled();
                owner.PerformLayout();
            }
        }
        protected override void ClearItems() { while (Count > 0) RemoveAt(Count-1); }
        protected override void RemoveItem(int index)
        {
            var child = this[index];
            if (child.NativeHandle != 0) Native.mf_parent(child.NativeHandle,0);
            child.parent = null;
            base.RemoveItem(index);
            child.OnParentChanged(EventArgs.Empty);
            owner.OnControlRemoved(new ControlEventArgs(child));
            if (owner is TableLayoutPanel table) table.ForgetCell(child);
            owner.PerformLayout();
        }
    }
}
