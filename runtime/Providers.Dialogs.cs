using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

public interface IWin32Window { IntPtr Handle { get; } }
public partial class Control : IWin32Window { }

internal static class ProviderDialogs
{
    internal delegate void ActionCallback(int action, IntPtr json);
    [DllImport("MacForms")] private static extern IntPtr mf_provider_dialog(IntPtr ownerView, int kind, string options, ActionCallback callback);
    [DllImport("MacForms")] private static extern void mf_free(IntPtr data);

    internal static string? Run(IntPtr owner, int kind, object options, ActionCallback callback)
    {
        var data = mf_provider_dialog(CommonDialog.OwnerView(owner), kind, JsonSerializer.Serialize(options), callback);
        if (data == 0) return null;
        try { return Marshal.PtrToStringUTF8(data); }
        finally { mf_free(data); }
    }
}

public class ColorDialog : CommonDialog
{
    private int[] palette = new int[16];
    private sealed record Result(int Color, int[] Palette);
    public ColorDialog() => Reset();
    public bool AllowFullOpen { get; set; }
    public bool FullOpen { get; set; }
    public bool ShowHelp { get; set; }
    public Color Color { get; set; }
    public int[] CustomColors
    {
        get => (int[])palette.Clone();
        set
        {
            Array.Fill(palette, 0xffffff);
            if (value != null) Array.Copy(value, palette, Math.Min(value.Length, palette.Length));
        }
    }
    public override void Reset()
    {
        Color = Color.Black;
        FullOpen = ShowHelp = false;
        AllowFullOpen = true;
        Array.Fill(palette, 0xffffff);
    }
    protected override bool RunDialog(IntPtr hwndOwner)
    {
        var options = new { Color = Color.ToArgb(), Palette = palette, AllowFullOpen, FullOpen, ShowHelp };
        string? json = ProviderDialogs.Run(hwndOwner, 0, options, (_,_) => OnHelpRequest(EventArgs.Empty));
        if (json == null) return false;
        var selected = JsonSerializer.Deserialize<Result>(json)!;
        Color = Color.FromArgb(selected.Color);
        CustomColors = selected.Palette;
        return true;
    }
}

public class FontDialog : CommonDialog
{
    private int minimum, maximum;
    private sealed record Result(string Family, float Size, FontStyle Style, int Color);
    public FontDialog() => Reset();
    public Font Font { get; set; } = new("Segoe UI", 9);
    public Color Color { get; set; }
    public int MinSize { get => minimum; set { minimum = Math.Max(0,value); if (maximum > 0 && minimum > maximum) maximum = minimum; } }
    public int MaxSize { get => maximum; set { maximum = Math.Max(0,value); if (maximum > 0 && minimum > maximum) minimum = maximum; } }
    public bool ShowApply { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowColor { get; set; }
    public bool ShowEffects { get; set; }
    public bool FixedPitchOnly { get; set; }
    public event EventHandler? Apply;
    protected virtual void OnApply(EventArgs e) => Apply?.Invoke(this,e);
    public override void Reset()
    {
        Font = new("Segoe UI", 9);
        Color = Color.Black;
        minimum = maximum = 0;
        ShowApply = ShowColor = ShowHelp = FixedPitchOnly = false;
        ShowEffects = true;
    }
    private void Read(string json)
    {
        var selected = JsonSerializer.Deserialize<Result>(json)!;
        Font = new(selected.Family, selected.Size, selected.Style);
        Color = Color.FromArgb(selected.Color);
    }
    protected override bool RunDialog(IntPtr hwndOwner)
    {
        var options = new { Family = Font.Name, Size = Font.SizeInPoints, Style = (int)Font.Style, Color = Color.ToArgb(),
            MinSize, MaxSize, ShowApply, ShowHelp, ShowColor, ShowEffects, FixedPitchOnly };
        string? json = ProviderDialogs.Run(hwndOwner, 1, options, (action, data) =>
        {
            if (action == 2) OnHelpRequest(EventArgs.Empty);
            else { Read(Marshal.PtrToStringUTF8(data)!); OnApply(EventArgs.Empty); }
        });
        if (json == null) return false;
        Read(json);
        return true;
    }
}
