using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public partial class Form
{
    public bool KeyPreview { get; set; }
    internal bool TryShortcut(Keys keys)
    {
        if (MainMenuStrip != null && Trigger(MainMenuStrip.Items)) return true;
        return false;
        bool Trigger(ToolStripItemCollection items)
        {
            foreach (var item in items.OfType<ToolStripMenuItem>())
            {
                if (!item.Enabled) continue;
                if (item.ShortcutKeys == keys && keys != Keys.None) { item.PerformClick(); return true; }
                if (Trigger(item.DropDownItems)) return true;
            }
            return false;
        }
    }
}
public partial class Control
{
    private delegate int KeyCallback(IntPtr handle,int phase,int code);
    private static readonly KeyCallback routeKey = RouteKey;
    [DllImport("MacForms")] private static extern void mf_set_key_callback(KeyCallback callback);
    [DllImport("MacForms")] private static extern int mf_key_modifiers();
    internal static void InstallKeyDispatcher()
    {
        mf_set_key_callback(routeKey);
        mf_set_drag_callback(routeDrag);
        mf_set_focus_callback(routeFocus);
    }
    public static Keys ModifierKeys => (Keys)mf_key_modifiers();
    public event KeyEventHandler? KeyDown, KeyUp;
    public event KeyPressEventHandler? KeyPress;
    public event PreviewKeyDownEventHandler? PreviewKeyDown;
    protected virtual void OnKeyDown(KeyEventArgs e) => KeyDown?.Invoke(this,e);
    protected virtual void OnKeyUp(KeyEventArgs e) => KeyUp?.Invoke(this,e);
    protected virtual void OnKeyPress(KeyPressEventArgs e) => KeyPress?.Invoke(this,e);
    protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e) => PreviewKeyDown?.Invoke(this,e);
    public Form? FindForm()
    {
        for (Control? owner = this; owner != null; owner = owner.Parent)
            if (owner is Form form) return form;
        return null;
    }
    private static int RouteKey(IntPtr handle,int phase,int code)
    {
        if (!peers.TryGetValue(handle,out var target)) return phase == 3 ? code : 0;
        Form? form = target.FindForm();
        if (phase == 4) return form?.ProcessMnemonic((char)code) == true ? 1 : 0;
        bool preview = form?.KeyPreview == true && form != target;
        if (phase == 3)
        {
            var character = new KeyPressEventArgs((char)code);
            if (preview) form!.OnKeyPress(character);
            if (!character.Handled && !target.IsDisposed) target.OnKeyPress(character);
            return character.Handled ? -1 : character.KeyChar;
        }
        var e = new KeyEventArgs((Keys)code);
        if (phase == 1)
        {
            target.OnPreviewKeyDown(new PreviewKeyDownEventArgs(e.KeyData));
            if (form?.TryShortcut(e.KeyData) == true) return 1;
            if (preview) form!.OnKeyDown(e);
            if (!e.Handled && !target.IsDisposed) target.OnKeyDown(e);
            if (!e.Handled && e.KeyData == Keys.Escape && form?.CancelButton is {} cancel)
            {
                cancel.PerformClick();
                return 1;
            }
        }
        else
        {
            if (preview) form!.OnKeyUp(e);
            if (!e.Handled && !target.IsDisposed) target.OnKeyUp(e);
        }
        return e.Handled || target.IsDisposed ? 1 : 0;
    }
}
