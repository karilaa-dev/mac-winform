using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public partial class Control
{
    private delegate int FocusCallback(IntPtr previous,IntPtr next,int committed);
    private static readonly FocusCallback routeFocus = RouteFocus;
    [DllImport("MacForms")] private static extern void mf_set_focus_callback(FocusCallback callback);
    private static Control? focused;
    public bool Focused => focused == this;
    public bool ContainsFocus => focused == this || (focused != null && Contains(focused));
    public bool Contains(Control? child)
    {
        for (var p = child?.Parent; p != null; p = p.Parent) if (p == this) return true;
        return false;
    }
    public bool IsHandleCreated => NativeHandle != 0;
    public bool HasChildren => Controls.Count != 0;
    public bool CausesValidation { get; set; } = true;
    public event EventHandler? Enter, Leave, GotFocus, LostFocus, Validated, ParentChanged, HandleCreated, HandleDestroyed;
    public event CancelEventHandler? Validating;
    public event ControlEventHandler? ControlAdded, ControlRemoved;
    protected virtual void OnEnter(EventArgs e) => Enter?.Invoke(this,e);
    protected virtual void OnLeave(EventArgs e) => Leave?.Invoke(this,e);
    protected virtual void OnGotFocus(EventArgs e) => GotFocus?.Invoke(this,e);
    protected virtual void OnLostFocus(EventArgs e) => LostFocus?.Invoke(this,e);
    protected virtual void OnValidating(CancelEventArgs e) => Validating?.Invoke(this,e);
    protected virtual void OnValidated(EventArgs e) => Validated?.Invoke(this,e);
    protected virtual void OnControlAdded(ControlEventArgs e) => ControlAdded?.Invoke(this,e);
    protected virtual void OnControlRemoved(ControlEventArgs e) => ControlRemoved?.Invoke(this,e);
    protected virtual void OnParentChanged(EventArgs e) => ParentChanged?.Invoke(this,e);
    protected virtual void OnHandleCreated(EventArgs e) => HandleCreated?.Invoke(this,e);
    protected virtual void OnHandleDestroyed(EventArgs e)
    {
        if (focused == this) focused = null;
        HandleDestroyed?.Invoke(this,e);
    }
    internal bool ValidateInput()
    {
        var args = new CancelEventArgs();
        OnValidating(args);
        if (args.Cancel) return false;
        OnValidated(EventArgs.Empty);
        return true;
    }
    private static int RouteFocus(IntPtr previous,IntPtr next,int committed)
    {
        peers.TryGetValue(previous,out var old);
        peers.TryGetValue(next,out var current);
        if (committed == 0) return old == null || current?.CausesValidation == false || old.ValidateInput() ? 1 : 0;
        if (ReferenceEquals(focused,current)) return 1;
        old = focused;
        focused = current;
        for (var parent = current?.Parent; parent != null; parent = parent.Parent)
            if (parent is ContainerControl container) container.RememberActiveControl(current!);
        old?.OnLeave(EventArgs.Empty);
        old?.OnLostFocus(EventArgs.Empty);
        current?.OnEnter(EventArgs.Empty);
        current?.OnGotFocus(EventArgs.Empty);
        return 1;
    }
    public void SetBounds(int x,int y,int width,int height) => Bounds = new(x,y,width,height);
    public void Select() => Focus();
    internal Control? FocusedChild => ContainsFocus ? focused : null;
}
public partial class ContainerControl
{
    private Control? activeControl;
    internal void RememberActiveControl(Control control) => activeControl = control;
    public Control? ActiveControl
    {
        get => activeControl != null && Contains(activeControl) ? activeControl : null;
        set
        {
            if (value == null) { activeControl = null; return; }
            if (!Contains(value)) return;
            if (!IsHandleCreated || value.Focus()) activeControl = value;
        }
    }
    public bool Validate() => ActiveControl?.ValidateInput() ?? true;
    public bool ValidateChildren()
    {
        bool success = true;
        void Visit(Control parent)
        {
            foreach (var child in parent.Controls.ToArray())
            {
                if (child.CausesValidation) success &= child.ValidateInput();
                Visit(child);
            }
        }
        Visit(this);
        return success;
    }
}
