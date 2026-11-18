using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms.VisualStyles;

// The class/part/state identifiers are the same values as the Windows theme schema.
// Only parts with a corresponding drawing implementation are advertised.
public sealed class VisualStyleRenderer
{
    private static readonly Color Accent = Color.FromArgb(0,120,212);
    private static readonly Color Face = Color.FromArgb(225,225,225);
    private static readonly Color Line = Color.FromArgb(173,173,173);
    private static readonly Color LightFace = Color.FromArgb(240,240,240);
    private static readonly Font DefaultFont = new("Segoe UI",9);
    private string key = "";
    public string Class { get; private set; } = "";
    public int Part { get; private set; }
    public int State { get; private set; }
    public static bool IsSupported => true;
    public int LastHResult => 0;

    public VisualStyleRenderer(VisualStyleElement element) => SetParameters(element);
    public VisualStyleRenderer(string className,int part,int state) => SetParameters(className,part,state);
    public static bool IsElementDefined(VisualStyleElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return Supports(element.ClassName.ToUpperInvariant(),element.Part,element.State);
    }
    private static bool Supports(string name,int part,int state)
    {
        int last = (name,part) switch
        {
            ("BUTTON",1) => 5, ("BUTTON",2) => 8, ("BUTTON",3) => 12, ("BUTTON",4) => 2,
            ("EDIT",1) => 7, ("EDIT",2) => 0, ("TAB",>=1 and <=8) => 4,
            ("TAB",9 or 10) or ("PROGRESS",>=1 and <=4) => 0,
            ("COMBOBOX",1) => 4, ("HEADER",>=1 and <=3) => 3,
            _ => -1
        };
        return state >= 0 && state <= last;
    }
    public void SetParameters(VisualStyleElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        SetParameters(element.ClassName,element.Part,element.State);
    }
    public void SetParameters(string className,int part,int state)
    {
        ArgumentNullException.ThrowIfNull(className);
        string name=className.ToUpperInvariant();
        if (!Supports(name,part,state))
            throw new ArgumentException($"No native drawing is available for theme element '{className}' part {part}, state {state}.");
        Class=className;
        key=name;
        Part=part;
        State=state;
    }
    private static Graphics Canvas(IDeviceContext dc) => dc as Graphics ??
        throw new NotSupportedException("The macOS renderer requires a System.Drawing.Graphics context.");
    private bool Option => key=="BUTTON" && Part is 2 or 3;
    [DllImport("MacForms")] private static extern int mf_choice_color(int radio,int state,int part);
    private Color ChoiceColor(int part) => Color.FromArgb(mf_choice_color(Part==2 ? 1 : 0,State,part));
    private int Interaction => Option && State>0 ? (State-1)%4+1 : State;
    private Color TextColor => Interaction==4 ? Color.FromArgb(109,109,109) : Color.Black;
    private Color FillColor => (key,Part,Interaction) switch
    {
        ("PROGRESS",3 or 4,_) => Color.FromArgb(6,176,37),
        ("EDIT",2,_) => Color.Black,
        ("EDIT",1,4 or 6) => LightFace,
        ("EDIT",_,_) or ("TAB",9 or 10,_) or ("TAB",_,3) => Color.White,
        ("HEADER",_,1 or 0) => Color.White,
        ("BUTTON",2 or 3,_) => ChoiceColor(0),
        (_,_,2) => Color.FromArgb(229,241,251),
        (_,_,3) => Color.FromArgb(204,228,247),
        (_,_,4) => LightFace,
        _ => Face
    };
    private Color BorderColor => (key,Part,Interaction) switch
    {
        ("BUTTON",1,5) or ("EDIT",1,5) => Accent,
        ("BUTTON",2 or 3,_) => ChoiceColor(1),
        (_,_,2 or 3) => Accent,
        ("EDIT",1,_) => Color.FromArgb(171,173,179),
        _ => Line
    };
    public Color GetColor(ColorProperty prop) => prop switch
    {
        ColorProperty.GlyphTextColor when Option => ChoiceColor(2),
        ColorProperty.TextColor or ColorProperty.GlyphTextColor => TextColor,
        ColorProperty.FillColor or ColorProperty.FillColorHint => FillColor,
        ColorProperty.BorderColor or ColorProperty.BorderColorHint => BorderColor,
        ColorProperty.AccentColorHint => Accent,
        ColorProperty.TransparentColor or ColorProperty.GlyphTransparentColor => Color.Transparent,
        _ => throw new NotSupportedException($"No color is defined for {prop} on this renderer.")
    };

    public void DrawBackground(IDeviceContext dc,Rectangle bounds)
    {
        var g=Canvas(dc);
        if (bounds.Width<=0 || bounds.Height<=0) return;
        if (Option)
        {
            g.DrawChoice(bounds,Part==2,State);
            return;
        }
        using var fill=new SolidBrush(FillColor);
        using var border=new SolidBrush(BorderColor);
        if (key=="EDIT" && Part==2 || key=="PROGRESS" && Part>=3 || key=="TAB" && Part==10)
        {
            g.FillRectangle(fill,bounds);
            return;
        }
        int thickness=key=="BUTTON" && Part==1 && State==5 ? Math.Min(2,Math.Min(bounds.Width,bounds.Height)) : 1;
        g.FillRectangle(border,bounds.X,bounds.Y,bounds.Width,thickness);
        g.FillRectangle(border,bounds.X,bounds.Bottom-thickness,bounds.Width,thickness);
        g.FillRectangle(border,bounds.X,bounds.Y,thickness,bounds.Height);
        g.FillRectangle(border,bounds.Right-thickness,bounds.Y,thickness,bounds.Height);
        var content=Rectangle.Inflate(bounds,-thickness,-thickness);
        if (content.Width<=0 || content.Height<=0 || key=="BUTTON" && Part==4) return;
        g.FillRectangle(fill,content);
        if (key=="COMBOBOX")
        {
            using var pen=new Pen(TextColor,1.5f);
            int x=bounds.X+bounds.Width/2, y=bounds.Y+bounds.Height/2;
            g.DrawLines(pen,new Point[] { new(x-3,y-1),new(x,y+2),new(x+3,y-1) });
        }
    }
    public void DrawBackground(IDeviceContext dc,Rectangle bounds,Rectangle clipRectangle)
    {
        var g=Canvas(dc);
        var state=g.Save();
        try { g.IntersectClip(clipRectangle); DrawBackground(g,bounds); }
        finally { g.Restore(state); }
    }
    public void DrawParentBackground(IDeviceContext dc,Rectangle bounds,Control childControl)
    {
        ArgumentNullException.ThrowIfNull(childControl);
        var color=childControl.Parent?.BackColor ?? Color.Empty;
        using var fill=new SolidBrush(color.IsEmpty ? LightFace : color);
        Canvas(dc).FillRectangle(fill,bounds);
    }
    public bool IsBackgroundPartiallyTransparent() => key=="BUTTON" && Part is 2 or 3 or 4;
    public void DrawImage(Graphics g,Rectangle bounds,Image image) => g.DrawImage(image,bounds);
    public void DrawImage(Graphics g,Rectangle bounds,ImageList imageList,int imageIndex) => g.DrawImage(imageList.Images[imageIndex],bounds);
    private int Inset => key=="EDIT" || key=="BUTTON" && Part==1 ? 3 : 1;
    public Rectangle GetBackgroundContentRectangle(IDeviceContext dc,Rectangle bounds) => Rectangle.Inflate(bounds,-Inset,-Inset);
    public Rectangle GetBackgroundExtent(IDeviceContext dc,Rectangle contentBounds) => Rectangle.Inflate(contentBounds,Inset,Inset);
    public Padding GetMargins(IDeviceContext dc,MarginProperty prop) => new(prop==MarginProperty.SizingMargins ? 1 : Inset);
    public Size GetPartSize(IDeviceContext dc,ThemeSizeType type)
    {
        Size logical=(key,Part) switch
        {
            ("BUTTON",2 or 3) => new(13,13),
            ("BUTTON",1) => new(75,23),
            ("EDIT",2) => new(1,15),
            ("EDIT",1) or ("COMBOBOX",1) => new(17,23),
            ("TAB",_) or ("HEADER",_) => new(20,20),
            _ => new(6,6)
        };
        float scale=Canvas(dc).DpiY/96f;
        return new((int)Math.Round(logical.Width*scale),(int)Math.Round(logical.Height*scale));
    }
    public Size GetPartSize(IDeviceContext dc,Rectangle bounds,ThemeSizeType type) =>
        type==ThemeSizeType.Draw && !Option ? bounds.Size : GetPartSize(dc,type);
    public Font GetFont(IDeviceContext dc,FontProperty prop) => (Font)DefaultFont.Clone();

    public void DrawText(IDeviceContext dc,Rectangle bounds,string? textToDraw) => DrawText(dc,bounds,textToDraw,false);
    public void DrawText(IDeviceContext dc,Rectangle bounds,string? textToDraw,bool drawDisabled) =>
        DrawText(dc,bounds,textToDraw,drawDisabled,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    public void DrawText(IDeviceContext dc,Rectangle bounds,string? textToDraw,bool drawDisabled,TextFormatFlags flags) =>
        PaintText(Canvas(dc),bounds,textToDraw,DefaultFont,drawDisabled ? Color.Gray : TextColor,flags);
    public Rectangle GetTextExtent(IDeviceContext dc,Rectangle bounds,string textToDraw,TextFormatFlags flags)
    {
        var size=TextRenderer.MeasureText(dc,textToDraw,DefaultFont,bounds.Size,flags);
        int x=bounds.X,y=bounds.Y;
        if (flags.HasFlag(TextFormatFlags.HorizontalCenter)) x+=(bounds.Width-size.Width)/2;
        else if (flags.HasFlag(TextFormatFlags.Right)) x+=bounds.Width-size.Width;
        if (flags.HasFlag(TextFormatFlags.VerticalCenter)) y+=(bounds.Height-size.Height)/2;
        else if (flags.HasFlag(TextFormatFlags.Bottom)) y+=bounds.Height-size.Height;
        return new Rectangle(new Point(x,y),size);
    }
    public Rectangle GetTextExtent(IDeviceContext dc,string textToDraw,TextFormatFlags flags) =>
        new(Point.Empty,TextRenderer.MeasureText(dc,textToDraw,DefaultFont,Size.Empty,flags));
    internal static void PaintText(Graphics g,Rectangle bounds,string? text,Font? font,Color ink,TextFormatFlags flags) =>
        TextRenderer.DrawText(g,text,font,bounds,ink,flags);
}
