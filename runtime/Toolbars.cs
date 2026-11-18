using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public class ToolStrip : ScrollableControl
{
    public ToolStrip()
    {
        Items = new(this);
        Dock = DockStyle.Top;
        Height = 25;
    }
    public ToolStrip(params ToolStripItem[] items) : this() => Items.AddRange(items);
    public ToolStripItemCollection Items { get; }
    internal override void Arrange(Size delta)
    {
        int x = Padding.Left;
        foreach (var item in Items)
        {
            if (!item.Visible) continue;
            if (item.AutoSize && item is ToolStripButton) item.Width = Math.Max(24,(int)(item.Font.Size*item.Text.Length*.9)+14);
            x += item.Margin.Left;
            item.Bounds = new(x,Padding.Top+item.Margin.Top,item.Width,Math.Max(0,Height-Padding.Top-Padding.Bottom-item.Margin.Top-item.Margin.Bottom));
            x += item.Width + item.Margin.Right;
        }
    }
}
public class ToolStripLabel : ToolStripItem
{
    public ToolStripLabel() {}
    public ToolStripLabel(string? text) { Text = text ?? ""; }
}
public class ToolStripButton : ToolStripItem
{
    internal override int Kind => 4;
    public ToolStripButton() {}
    public ToolStripButton(string? text) { Text = text ?? ""; }
    public ToolStripButton(string? text,Image? image,EventHandler? onClick) : this(text) { Image = image; Click += onClick; }
    private bool check;
    public bool CheckOnClick { get; set; }
    public bool Checked { get => check; set { if (check != value) { check = value; ApplyCheck(); CheckedChanged?.Invoke(this,EventArgs.Empty); } } }
    public event EventHandler? CheckedChanged;
    [DllImport("MacForms")] private static extern void mf_button_state(IntPtr h,int value);
    private void ApplyCheck() { if (NativeHandle != 0) mf_button_state(NativeHandle,Checked ? 1 : 0); }
    internal override void ApplyNative() { base.ApplyNative(); ApplyCheck(); }
    protected override void OnClick(EventArgs e) { if (CheckOnClick) Checked = !Checked; base.OnClick(e); }
}
public class ToolStripControlHost : ToolStripItem
{
    public ToolStripControlHost(Control control)
    {
        Control = control;
        AutoSize = false;
        Size = control.Size;
        Control.Dock = DockStyle.Fill;
        Controls.Add(control);
    }
    internal override int Kind => 2;
    public Control Control { get; }
    public override string Text { get => Control.Text; set => Control.Text = value; }
}
public class ToolStripTextBox : ToolStripControlHost
{
    public ToolStripTextBox() : base(new TextBox()) {}
    public TextBox TextBox => (TextBox)Control;
}
public class ToolStripComboBox : ToolStripControlHost
{
    public ToolStripComboBox() : base(new ComboBox()) {}
    public ComboBox ComboBox => (ComboBox)Control;
    public ListControl.ObjectCollection Items => ComboBox.Items;
    public int SelectedIndex { get => ComboBox.SelectedIndex; set => ComboBox.SelectedIndex = value; }
    public object? SelectedItem { get => ComboBox.SelectedItem; set => ComboBox.SelectedItem = value; }
    public event EventHandler? SelectedIndexChanged { add => ComboBox.SelectedIndexChanged += value; remove => ComboBox.SelectedIndexChanged -= value; }
}
public class ToolStripProgressBar : ToolStripControlHost
{
    public ToolStripProgressBar() : base(new ProgressBar()) {}
    public ProgressBar ProgressBar => (ProgressBar)Control;
    public int Minimum { get => ProgressBar.Minimum; set => ProgressBar.Minimum = value; }
    public int Maximum { get => ProgressBar.Maximum; set => ProgressBar.Maximum = value; }
    public int Value { get => ProgressBar.Value; set => ProgressBar.Value = value; }
}
public class ToolStripDropDownButton : ToolStripMenuItem
{
    public ToolStripDropDownButton() {}
    public ToolStripDropDownButton(string? text) : base(text ?? "") {}
}
public class ToolStripDropDown : ToolStrip
{
    [DllImport("MacForms")] private static extern void mf_popup(IntPtr menu,IntPtr control,int x,int y);
    private ToolStripDropDownCloseReason closeReason = ToolStripDropDownCloseReason.AppClicked;
    internal override int Kind => 30;
    public Control? SourceControl { get; internal set; }
    public event CancelEventHandler? Opening;
    public event EventHandler? Opened;
    public event ToolStripDropDownClosedEventHandler? Closed;
    internal bool Open()
    {
        var e = new CancelEventArgs();
        Opening?.Invoke(this,e);
        if (!e.Cancel) Opened?.Invoke(this,EventArgs.Empty);
        return !e.Cancel;
    }
    internal override void OnNative(int ev)
    {
        if (ev == 32) { Closed?.Invoke(this,new(closeReason)); closeReason = ToolStripDropDownCloseReason.AppClicked; return; }
        if (ev == 31) { if (!Open()) mf_cancel_menu(Handle); return; }
        base.OnNative(ev);
    }
    [DllImport("MacForms")] private static extern void mf_cancel_menu(IntPtr menu);
    public void Show(Control control,Point position)
    {
        SourceControl = control;
        mf_popup(Handle,control.Handle,position.X,position.Y);
    }
    public void Show(Point screenLocation)
    {
        SourceControl = null;
        mf_popup(Handle,IntPtr.Zero,screenLocation.X,screenLocation.Y);
    }
    public void Show(int x,int y) => Show(new Point(x,y));
    public void Close() => Close(ToolStripDropDownCloseReason.CloseCalled);
    public void Close(ToolStripDropDownCloseReason reason)
    {
        closeReason = reason;
        if (NativeHandle != 0) mf_cancel_menu(NativeHandle);
    }
    public void Show(Control control,int x,int y) => Show(control,new Point(x,y));
}
public class ToolStripDropDownMenu : ToolStripDropDown { }
public class ContextMenuStrip : ToolStripDropDownMenu
{
    public ContextMenuStrip() { }
    public ContextMenuStrip(IContainer container) => container.Add(this);
}

public partial class Control
{
    [DllImport("MacForms")] private static extern void mf_context_menu(IntPtr h,IntPtr menu);
    private ContextMenuStrip? contextMenu;
    public virtual ContextMenuStrip? ContextMenuStrip
    {
        get => contextMenu;
        set { contextMenu = value; if (NativeHandle != 0) UpdateContextMenu(); }
    }
    internal void UpdateContextMenu()
    {
        if (contextMenu != null) contextMenu.SourceControl = this;
        mf_context_menu(NativeHandle,contextMenu?.Handle ?? 0);
    }
}
