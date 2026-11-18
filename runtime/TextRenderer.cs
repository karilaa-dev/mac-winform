using System.Drawing;

namespace System.Windows.Forms;

public sealed class TextRenderer
{
    private static readonly Font DefaultFont = new("Segoe UI",9);
    private TextRenderer() { }

    private static Graphics GraphicsFor(IDeviceContext dc)
    {
        ArgumentNullException.ThrowIfNull(dc);
        return dc as Graphics ?? throw new PlatformNotSupportedException("TextRenderer on macOS requires a System.Drawing.Graphics context.");
    }
    private static Size Measure(string? text,Font? font,Size proposedSize,TextFormatFlags flags,float dpi)
    {
        if (string.IsNullOrEmpty(text)) return Size.Empty;
        font ??= DefaultFont;
        Canvas.mf_format_text(text,font.Name,font.SizeInPoints*dpi/72,(int)font.Style,0,0,(int)flags,
            0,0,proposedSize.Width,proposedSize.Height,0,out float width,out float height);
        return Size.Ceiling(new SizeF(width,height));
    }
    public static Size MeasureText(string? text,Font? font) =>
        MeasureText(text,font,Size.Empty,TextFormatFlags.Default);
    public static Size MeasureText(string? text,Font? font,Size proposedSize) =>
        MeasureText(text,font,proposedSize,TextFormatFlags.Default);
    public static Size MeasureText(string? text,Font? font,Size proposedSize,TextFormatFlags flags) =>
        Measure(text,font,proposedSize,flags,96);
    public static Size MeasureText(ReadOnlySpan<char> text,Font? font) =>
        MeasureText(text.ToString(),font,Size.Empty,TextFormatFlags.Default);
    public static Size MeasureText(ReadOnlySpan<char> text,Font? font,Size proposedSize) =>
        MeasureText(text.ToString(),font,proposedSize,TextFormatFlags.Default);
    public static Size MeasureText(ReadOnlySpan<char> text,Font? font,Size proposedSize,TextFormatFlags flags) =>
        Measure(text.ToString(),font,proposedSize,flags,96);
    public static Size MeasureText(IDeviceContext dc,string? text,Font? font) =>
        MeasureText(dc,text,font,Size.Empty,TextFormatFlags.Default);
    public static Size MeasureText(IDeviceContext dc,string? text,Font? font,Size proposedSize) =>
        MeasureText(dc,text,font,proposedSize,TextFormatFlags.Default);
    public static Size MeasureText(IDeviceContext dc,string? text,Font? font,Size proposedSize,TextFormatFlags flags) =>
        Measure(text,font,proposedSize,flags,GraphicsFor(dc).DpiY);
    public static Size MeasureText(IDeviceContext dc,ReadOnlySpan<char> text,Font? font) =>
        MeasureText(dc,text.ToString(),font,Size.Empty,TextFormatFlags.Default);
    public static Size MeasureText(IDeviceContext dc,ReadOnlySpan<char> text,Font? font,Size proposedSize) =>
        MeasureText(dc,text.ToString(),font,proposedSize,TextFormatFlags.Default);
    public static Size MeasureText(IDeviceContext dc,ReadOnlySpan<char> text,Font? font,Size proposedSize,TextFormatFlags flags) =>
        Measure(text.ToString(),font,proposedSize,flags,GraphicsFor(dc).DpiY);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Point pt,Color foreColor) =>
        DrawText(dc,text,font,pt,foreColor,Color.Empty,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Point pt,Color foreColor,Color backColor) =>
        DrawText(dc,text,font,pt,foreColor,backColor,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Point pt,Color foreColor,TextFormatFlags flags) =>
        DrawText(dc,text,font,pt,foreColor,Color.Empty,flags);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Point pt,Color foreColor,Color backColor,TextFormatFlags flags) =>
        DrawText(dc,text,font,new Rectangle(pt,MeasureText(dc,text,font,Size.Empty,flags)),foreColor,backColor,flags);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Point pt,Color foreColor) =>
        DrawText(dc,text.ToString(),font,pt,foreColor,Color.Empty,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Point pt,Color foreColor,Color backColor) =>
        DrawText(dc,text.ToString(),font,pt,foreColor,backColor,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Point pt,Color foreColor,TextFormatFlags flags) =>
        DrawText(dc,text.ToString(),font,pt,foreColor,Color.Empty,flags);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Point pt,Color foreColor,Color backColor,TextFormatFlags flags) =>
        DrawText(dc,text.ToString(),font,pt,foreColor,backColor,flags);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Rectangle bounds,Color foreColor) =>
        DrawText(dc,text,font,bounds,foreColor,Color.Empty,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Rectangle bounds,Color foreColor,Color backColor) =>
        DrawText(dc,text,font,bounds,foreColor,backColor,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Rectangle bounds,Color foreColor,TextFormatFlags flags) =>
        DrawText(dc,text,font,bounds,foreColor,Color.Empty,flags);
    public static void DrawText(IDeviceContext dc,string? text,Font? font,Rectangle bounds,Color foreColor,Color backColor,TextFormatFlags flags)
    {
        var g = GraphicsFor(dc);
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0) return;
        g.DrawFormattedText(text,font ?? DefaultFont,bounds,foreColor,backColor,(int)flags);
    }
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Rectangle bounds,Color foreColor) =>
        DrawText(dc,text.ToString(),font,bounds,foreColor,Color.Empty,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Rectangle bounds,Color foreColor,Color backColor) =>
        DrawText(dc,text.ToString(),font,bounds,foreColor,backColor,TextFormatFlags.Default);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Rectangle bounds,Color foreColor,TextFormatFlags flags) =>
        DrawText(dc,text.ToString(),font,bounds,foreColor,Color.Empty,flags);
    public static void DrawText(IDeviceContext dc,ReadOnlySpan<char> text,Font font,Rectangle bounds,Color foreColor,Color backColor,TextFormatFlags flags) =>
        DrawText(dc,text.ToString(),font,bounds,foreColor,backColor,flags);
}
