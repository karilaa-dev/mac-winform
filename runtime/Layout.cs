using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

internal static class PaddingDimensions
{
    internal static int Horizontal(this Padding p) => p.Left + p.Right;
    internal static int Vertical(this Padding p) => p.Top + p.Bottom;
}

public enum ToolStripItemDisplayStyle { None, Text, Image, ImageAndText }
public class ToolStripItem : Label
{
    private string tooltip = "";
    private Image? image;
    public Image? Image { get => image; set { image = value; ApplyImage(); } }
    private ToolStripItemDisplayStyle display = ToolStripItemDisplayStyle.ImageAndText;
    public ToolStripItemDisplayStyle DisplayStyle { get => display; set { display = value; ApplyImage(); } }
    [DllImport("MacForms")] private static extern void mf_item_image(IntPtr h,IntPtr image,int display);
    private void ApplyImage() { if (NativeHandle != 0) mf_item_image(NativeHandle,image?.Handle.DangerousGetHandle() ?? 0,(int)display); }
    public void PerformClick() { if (Enabled) OnClick(EventArgs.Empty); }
    public ToolStripItem() { AutoSize = true; Margin = new Padding(0, 2, 0, 2); }
    public string ToolTipText
    {
        get => tooltip;
        set { tooltip = value; UpdateTooltip(); }
    }
    [DllImport("MacForms")] private static extern void mf_tooltip(IntPtr handle, string text);
    internal override void ApplyNative() { base.ApplyNative(); UpdateTooltip(); ApplyImage(); }
    internal void UpdateTooltip()
    {
        if (NativeHandle != IntPtr.Zero) mf_tooltip(NativeHandle, Parent is StatusStrip { ShowItemToolTips: false } ? "" : tooltip);
    }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Parent?.PerformLayout(); }
}

public class ToolStripStatusLabel : ToolStripLabel
{
    public ToolStripStatusLabel() {}
    public ToolStripStatusLabel(string? text) : base(text) {}
}

public class StatusStrip : ToolStrip
{
    private bool tooltips;
    public StatusStrip() { Dock = DockStyle.Bottom; Height = 22; }
    public bool SizingGrip { get; set; } = true;
    public bool ShowItemToolTips
    {
        get => tooltips;
        set { tooltips = value; foreach (var item in Items.OfType<ToolStripItem>()) item.UpdateTooltip(); }
    }
    internal override void Arrange(Size delta)
    {
        int x = Padding.Left;
        foreach (var child in Controls)
        {
            var margin = child.Margin;
            x += margin.Left;
            child.Bounds = new Rectangle(x, Padding.Top + margin.Top, child.Width,
                Math.Max(0, Height - Padding.Vertical() - margin.Vertical()));
            x += child.Width + margin.Right;
        }
    }
}
