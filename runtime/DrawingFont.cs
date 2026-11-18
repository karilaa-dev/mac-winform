using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Drawing;

public sealed class FontFamily : IDisposable
{
    public FontFamily(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
    public string Name { get; }
    public static FontFamily GenericSansSerif => new("Helvetica");
    public static FontFamily GenericSerif => new("Times New Roman");
    public static FontFamily GenericMonospace => new("Menlo");
    public static FontFamily[] Families
    {
        get
        {
            var ptr = mf_font_families();
            if (ptr == 0) return [];
            try
            {
                var names = JsonSerializer.Deserialize<string[]>(Marshal.PtrToStringUTF8(ptr)!) ?? [];
                return Array.ConvertAll(names, name => new FontFamily(name));
            }
            finally { Canvas.mf_free(ptr); }
        }
    }
    [DllImport("MacForms")] private static extern IntPtr mf_font_families();
    public override bool Equals(object? o) => o is FontFamily f && string.Equals(f.Name,Name,StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    public void Dispose() {}
}

public sealed class Font : IDisposable, ICloneable
{
    public Font(string familyName,float emSize) : this(new FontFamily(familyName),emSize) {}
    public Font(string familyName,float emSize,FontStyle style) : this(new FontFamily(familyName),emSize,style) {}
    public Font(string familyName,float emSize,GraphicsUnit unit) : this(new FontFamily(familyName),emSize,FontStyle.Regular,unit) {}
    public Font(string familyName,float emSize,FontStyle style,GraphicsUnit unit) : this(new FontFamily(familyName),emSize,style,unit) {}
    public Font(FontFamily family,float emSize) : this(family,emSize,FontStyle.Regular,GraphicsUnit.Point) {}
    public Font(FontFamily family,float emSize,FontStyle style) : this(family,emSize,style,GraphicsUnit.Point) {}
    public Font(FontFamily family,float emSize,GraphicsUnit unit) : this(family,emSize,FontStyle.Regular,unit) {}
    public Font(Font prototype,FontStyle newStyle) : this(prototype.FontFamily,prototype.Size,newStyle,prototype.Unit) {}
    public Font(FontFamily family,float emSize,FontStyle style,GraphicsUnit unit)
    {
        ArgumentNullException.ThrowIfNull(family);
        if (!float.IsFinite(emSize) || emSize <= 0) throw new ArgumentOutOfRangeException(nameof(emSize));
        FontFamily = family;
        Size = emSize;
        Style = style;
        Unit = unit;
        SizeInPoints = unit switch {
            GraphicsUnit.Point => emSize, GraphicsUnit.Pixel => emSize*72/96,
            GraphicsUnit.Inch => emSize*72, GraphicsUnit.Document => emSize*72/300,
            GraphicsUnit.Millimeter => emSize*72/25.4f,
            _ => throw new ArgumentException("Choose a physical unit or pixels.",nameof(unit)) };
    }
    public string Name => FontFamily.Name;
    public FontFamily FontFamily { get; }
    public float Size { get; }
    public FontStyle Style { get; }
    public GraphicsUnit Unit { get; }
    public float SizeInPoints { get; }
    public bool Bold => Style.HasFlag(FontStyle.Bold);
    public bool Italic => Style.HasFlag(FontStyle.Italic);
    public bool Underline => Style.HasFlag(FontStyle.Underline);
    public bool Strikeout => Style.HasFlag(FontStyle.Strikeout);
    public int Height => (int)Math.Ceiling(GetHeight());
    public float GetHeight() => GetHeight(96);
    public float GetHeight(Graphics graphics) => GetHeight(graphics.DpiY);
    public float GetHeight(float dpi) => mf_font_height(Name,(int)Style,SizeInPoints*dpi/72);
    [DllImport("MacForms")] private static extern float mf_font_height(string family,int style,float size);
    public object Clone() => new Font(FontFamily,Size,Style,Unit);
    public void Dispose() {}
}
