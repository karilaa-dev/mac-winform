using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public class AccessibleObject
{
    public virtual string? Name { get; set; }
    public virtual string? Description => null;
    public virtual string? DefaultAction => null;
    public virtual string? Help => null;
    public virtual string? KeyboardShortcut => null;
    public virtual AccessibleObject? Parent => null;
    public virtual Rectangle Bounds => Rectangle.Empty;
    public virtual AccessibleRole Role => AccessibleRole.None;
    public virtual AccessibleStates State => AccessibleStates.None;
    public virtual string? Value { get; set; }
    public virtual int GetChildCount() => -1;
    public virtual AccessibleObject? GetChild(int index) => null;
    public virtual AccessibleObject? GetFocused() => null;
    public virtual void DoDefaultAction() { }
    public virtual void Select(AccessibleSelection flags) { }
    public virtual AccessibleObject? HitTest(int x,int y)
    {
        for (int i=GetChildCount()-1;i>=0;i--)
            if (GetChild(i)?.HitTest(x,y) is {} child) return child;
        return Bounds.Contains(x,y) ? this : null;
    }
}

public partial class Control
{
    [DllImport("MacForms")] private static extern void mf_ax_properties(IntPtr view,string? label,string? description,int role);
    [DllImport("MacForms")] private static extern void mf_ax_notify(IntPtr view,int ev);
    private AccessibleObject? accessibility;
    private string? accessibleName, accessibleDescription;
    private AccessibleRole role = AccessibleRole.Default;
    public string? AccessibleName { get => accessibleName; set { accessibleName = value; ApplyAccessibility(); } }
    public string? AccessibleDescription { get => accessibleDescription; set { accessibleDescription = value; ApplyAccessibility(); } }
    public string? AccessibleDefaultActionDescription { get; set; }
    public AccessibleRole AccessibleRole { get => role; set { role = value; ApplyAccessibility(); } }
    public AccessibleObject AccessibilityObject => accessibility ??= CreateAccessibilityInstance();
    protected virtual AccessibleObject CreateAccessibilityInstance() => new ControlAccessibleObject(this);
    private void ApplyAccessibility()
    {
        if (NativeHandle != 0) mf_ax_properties(ViewHandle,accessibleName,accessibleDescription,(int)role);
    }
    protected void AccessibilityNotifyClients(AccessibleEvents accEvent,int childID)
    {
        var view = childID >= 0 && childID < Controls.Count ? Controls[childID] : this;
        if (view.IsHandleCreated) mf_ax_notify(view.ViewHandle,(int)accEvent);
    }
    public class ControlAccessibleObject : AccessibleObject
    {
        public ControlAccessibleObject(Control ownerControl) => Owner = ownerControl ?? throw new ArgumentNullException(nameof(ownerControl));
        public Control Owner { get; }
        public override string? Name { get => Owner.AccessibleName ?? Owner.Text; set => Owner.AccessibleName = value; }
        public override string? Description => Owner.AccessibleDescription;
        public override string? DefaultAction => Owner.AccessibleDefaultActionDescription ?? (Owner is IButtonControl ? "Press" : null);
        public override AccessibleObject? Parent => Owner.Parent?.AccessibilityObject;
        public override Rectangle Bounds => Owner.RectangleToScreen(Owner.ClientRectangle);
        public override string? Value
        {
            get => Owner is TextBox { UseSystemPasswordChar:true } or TextBox { PasswordChar: not '\0' } ? null : Owner.Text;
            set { if (Owner is TextBoxBase { ReadOnly:false }) Owner.Text = value ?? ""; }
        }
        public override AccessibleRole Role => Owner.AccessibleRole != AccessibleRole.Default ? Owner.AccessibleRole : Owner switch
        {
            Form => AccessibleRole.Window, RadioButton => AccessibleRole.RadioButton, CheckBox => AccessibleRole.CheckButton,
            Button => AccessibleRole.PushButton, TextBoxBase => AccessibleRole.Text, ComboBox => AccessibleRole.ComboBox,
            LinkLabel => AccessibleRole.Link, Label => AccessibleRole.StaticText, GroupBox => AccessibleRole.Grouping,
            ProgressBar => AccessibleRole.ProgressBar, ListView or ListBox => AccessibleRole.List,
            TreeView => AccessibleRole.Outline, DataGridView => AccessibleRole.Table, _ => AccessibleRole.Client
        };
        public override AccessibleStates State => (Owner.Enabled ? 0 : AccessibleStates.Unavailable)
            | (Owner.Visible ? 0 : AccessibleStates.Invisible) | (Owner.Focused ? AccessibleStates.Focused : 0)
            | (Owner.TabStop ? AccessibleStates.Focusable : 0) | (Owner is CheckBox { Checked:true } ? AccessibleStates.Checked : 0);
        public override int GetChildCount() => Owner.Controls.Count;
        public override AccessibleObject? GetChild(int index) => index >= 0 && index < Owner.Controls.Count ? Owner.Controls[index].AccessibilityObject : null;
        public override AccessibleObject? GetFocused() => Owner.FocusedChild?.AccessibilityObject;
        public override void Select(AccessibleSelection flags) { if ((flags & AccessibleSelection.TakeFocus) != 0) Owner.Focus(); }
        public override void DoDefaultAction() { if (Owner is IButtonControl b) b.PerformClick(); else if (Owner is CheckBox check && Owner.Enabled) check.Checked = !check.Checked; }
    }
}
