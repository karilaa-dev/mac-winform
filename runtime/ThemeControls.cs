using System.Drawing;
using System.Windows.Forms.VisualStyles;

namespace System.Windows.Forms;

public static partial class Application
{
    public static bool RenderWithVisualStyles { get; private set; }
    public static void EnableVisualStyles()
    {
        Initialize();
        RenderWithVisualStyles = true;
    }
}

public static class ControlPaint
{
    public static void DrawFocusRectangle(Graphics graphics,Rectangle rectangle) =>
        DrawFocusRectangle(graphics,rectangle,Color.Black,Color.White);
    public static void DrawFocusRectangle(Graphics graphics,Rectangle rectangle,Color foreColor,Color backColor)
    {
        using var ink=new SolidBrush(foreColor);
        for (int x=rectangle.Left; x<rectangle.Right; x+=2)
        {
            graphics.FillRectangle(ink,x,rectangle.Top,1,1);
            graphics.FillRectangle(ink,x,rectangle.Bottom-1,1,1);
        }
        for (int y=rectangle.Top+2; y<rectangle.Bottom-1; y+=2)
        {
            graphics.FillRectangle(ink,rectangle.Left,y,1,1);
            graphics.FillRectangle(ink,rectangle.Right-1,y,1,1);
        }
    }
    public static void DrawCheckBox(Graphics g,Rectangle bounds,ButtonState state) =>
        DrawClassicGlyph(g,bounds,false,state);
    public static void DrawCheckBox(Graphics g,int x,int y,int width,int height,ButtonState state) =>
        DrawCheckBox(g,new Rectangle(x,y,width,height),state);
    public static void DrawRadioButton(Graphics g,Rectangle bounds,ButtonState state) =>
        DrawClassicGlyph(g,bounds,true,state);
    public static void DrawRadioButton(Graphics g,int x,int y,int width,int height,ButtonState state) =>
        DrawRadioButton(g,new Rectangle(x,y,width,height),state);
    internal static void DrawClassicGlyph(Graphics g,Rectangle r,bool radio,ButtonState state,bool mixed=false)
    {
        using var border=new SolidBrush(Color.DimGray);
        using var face=new SolidBrush(state.HasFlag(ButtonState.Inactive) || state.HasFlag(ButtonState.Pushed) ? Color.LightGray : Color.White);
        var inner=Rectangle.Inflate(r,-2,-2);
        if (inner.Width<=0 || inner.Height<=0) return;
        if (radio) { g.FillEllipse(border,r); g.FillEllipse(face,inner); }
        else { g.FillRectangle(border,r); g.FillRectangle(face,inner); }
        if (!state.HasFlag(ButtonState.Checked)) return;
        var ink=state.HasFlag(ButtonState.Inactive) ? Color.Gray : Color.Black;
        using var glyph=new SolidBrush(ink);
        if (radio || mixed)
        {
            var dot=Rectangle.Inflate(r,-Math.Max(1,r.Width/4),-Math.Max(1,r.Height/4));
            if (radio) g.FillEllipse(glyph,dot); else g.FillRectangle(glyph,dot);
        }
        else
        {
            using var pen=new Pen(ink,Math.Max(1,r.Width/7f));
            g.DrawLines(pen,new PointF[] {new(r.X+r.Width*.18f,r.Y+r.Height*.48f),
                new(r.X+r.Width*.42f,r.Y+r.Height*.7f),new(r.X+r.Width*.8f,r.Y+r.Height*.22f)});
        }
    }
    public static void DrawButton(Graphics g,int x,int y,int width,int height,ButtonState state) =>
        DrawButton(g,new Rectangle(x,y,width,height),state);
    public static void DrawButton(Graphics g,Rectangle r,ButtonState state)
    {
        if (r.Width<=0 || r.Height<=0) return;
        using var face=new SolidBrush(Color.FromArgb(212,208,200));
        g.FillRectangle(face,r);
        bool sunken=state.HasFlag(ButtonState.Pushed) || state.HasFlag(ButtonState.Checked);
        using var light=new SolidBrush(sunken ? Color.DimGray : Color.White);
        using var shadow=new SolidBrush(sunken ? Color.White : Color.DimGray);
        g.FillRectangle(light,r.Left,r.Top,r.Width,1);
        g.FillRectangle(light,r.Left,r.Top,1,r.Height);
        g.FillRectangle(shadow,r.Left,r.Bottom-1,r.Width,1);
        g.FillRectangle(shadow,r.Right-1,r.Top,1,r.Height);
    }
}

public static class ButtonRenderer
{
    internal const TextFormatFlags Center=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
    public static bool RenderMatchingApplicationState { get; set; } = true;
    private static VisualStyleRenderer Theme(PushButtonState state) => new("BUTTON",1,(int)state);
    public static bool IsBackgroundPartiallyTransparent(PushButtonState state) => Theme(state).IsBackgroundPartiallyTransparent();
    public static void DrawParentBackground(Graphics g,Rectangle bounds,Control childControl) =>
        Theme(PushButtonState.Normal).DrawParentBackground(g,bounds,childControl);
    public static void DrawButton(Graphics g,Rectangle bounds,PushButtonState state) => DrawButton(g,bounds,false,state);
    public static void DrawButton(Graphics g,Rectangle bounds,bool focused,PushButtonState state) =>
        Draw(g,bounds,null,null,Center,null,Rectangle.Empty,focused,state);
    public static void DrawButton(Graphics g,Rectangle bounds,string? buttonText,Font? font,bool focused,PushButtonState state) =>
        Draw(g,bounds,buttonText,font,Center,null,Rectangle.Empty,focused,state);
    public static void DrawButton(Graphics g,Rectangle bounds,string? buttonText,Font? font,TextFormatFlags flags,bool focused,PushButtonState state) =>
        Draw(g,bounds,buttonText,font,flags,null,Rectangle.Empty,focused,state);
    public static void DrawButton(Graphics g,Rectangle bounds,Image image,Rectangle imageBounds,bool focused,PushButtonState state) =>
        Draw(g,bounds,null,null,Center,image,imageBounds,focused,state);
    public static void DrawButton(Graphics g,Rectangle bounds,string? buttonText,Font? font,Image image,Rectangle imageBounds,bool focused,PushButtonState state) =>
        Draw(g,bounds,buttonText,font,Center,image,imageBounds,focused,state);
    public static void DrawButton(Graphics g,Rectangle bounds,string? buttonText,Font? font,TextFormatFlags flags,Image image,Rectangle imageBounds,bool focused,PushButtonState state) =>
        Draw(g,bounds,buttonText,font,flags,image,imageBounds,focused,state);
    private static void Draw(Graphics g,Rectangle bounds,string? text,Font? font,TextFormatFlags flags,Image? image,Rectangle imageBounds,bool focused,PushButtonState state)
    {
        var theme=Theme(state);
        if (RenderMatchingApplicationState && !Application.RenderWithVisualStyles)
            ControlPaint.DrawButton(g,bounds,state==PushButtonState.Pressed ? ButtonState.Pushed : ButtonState.Normal);
        else theme.DrawBackground(g,bounds);
        if (image!=null) g.DrawImage(image,imageBounds);
        var inner=theme.GetBackgroundContentRectangle(g,bounds);
        VisualStyleRenderer.PaintText(g,inner,text,font,theme.GetColor(ColorProperty.TextColor),flags);
        if (focused) ControlPaint.DrawFocusRectangle(g,inner);
    }
}

public static class TextBoxRenderer
{
    public static bool IsSupported => VisualStyleRenderer.IsSupported;
    public static void DrawTextBox(Graphics g,Rectangle bounds,TextBoxState state) =>
        new VisualStyleRenderer("EDIT",1,(int)state).DrawBackground(g,bounds);
    public static void DrawTextBox(Graphics g,Rectangle bounds,string? text,Font? font,TextBoxState state) =>
        DrawTextBox(g,bounds,text,font,Rectangle.Inflate(bounds,-3,-3),TextFormatFlags.TextBoxControl,state);
    public static void DrawTextBox(Graphics g,Rectangle bounds,string? text,Font? font,Rectangle textBounds,TextBoxState state) =>
        DrawTextBox(g,bounds,text,font,textBounds,TextFormatFlags.TextBoxControl,state);
    public static void DrawTextBox(Graphics g,Rectangle bounds,string? text,Font? font,TextFormatFlags flags,TextBoxState state) =>
        DrawTextBox(g,bounds,text,font,Rectangle.Inflate(bounds,-3,-3),flags,state);
    public static void DrawTextBox(Graphics g,Rectangle bounds,string? text,Font? font,Rectangle textBounds,TextFormatFlags flags,TextBoxState state)
    {
        var theme=new VisualStyleRenderer("EDIT",1,(int)state);
        theme.DrawBackground(g,bounds);
        VisualStyleRenderer.PaintText(g,textBounds,text,font,theme.GetColor(ColorProperty.TextColor),flags);
    }
}

public static class ProgressBarRenderer
{
    public static bool IsSupported => VisualStyleRenderer.IsSupported;
    public static void DrawHorizontalBar(Graphics g,Rectangle r) => new VisualStyleRenderer(VisualStyleElement.ProgressBar.Bar.Normal).DrawBackground(g,r);
    public static void DrawVerticalBar(Graphics g,Rectangle r) => new VisualStyleRenderer(VisualStyleElement.ProgressBar.BarVertical.Normal).DrawBackground(g,r);
    public static void DrawHorizontalChunks(Graphics g,Rectangle r) => new VisualStyleRenderer(VisualStyleElement.ProgressBar.Chunk.Normal).DrawBackground(g,r);
    public static void DrawVerticalChunks(Graphics g,Rectangle r) => new VisualStyleRenderer(VisualStyleElement.ProgressBar.ChunkVertical.Normal).DrawBackground(g,r);
}

public static class TabRenderer
{
    public static bool IsSupported => VisualStyleRenderer.IsSupported;
    public static void DrawTabPage(Graphics g,Rectangle r) => new VisualStyleRenderer(VisualStyleElement.Tab.Pane.Normal).DrawBackground(g,r);
    public static void DrawTabItem(Graphics g,Rectangle r,TabItemState state) => DrawTabItem(g,r,false,state);
    public static void DrawTabItem(Graphics g,Rectangle r,bool focused,TabItemState state) =>
        DrawTabItem(g,r,null,null,ButtonRenderer.Center,focused,state);
    public static void DrawTabItem(Graphics g,Rectangle r,string? text,Font? font,TabItemState state) => DrawTabItem(g,r,text,font,false,state);
    public static void DrawTabItem(Graphics g,Rectangle r,string? text,Font? font,bool focused,TabItemState state) =>
        DrawTabItem(g,r,text,font,ButtonRenderer.Center,focused,state);
    public static void DrawTabItem(Graphics g,Rectangle r,string? text,Font? font,TextFormatFlags flags,bool focused,TabItemState state)
    {
        var theme=new VisualStyleRenderer("TAB",1,(int)state);
        theme.DrawBackground(g,r);
        var inner=theme.GetBackgroundContentRectangle(g,r);
        VisualStyleRenderer.PaintText(g,inner,text,font,theme.GetColor(ColorProperty.TextColor),flags);
        if (focused) ControlPaint.DrawFocusRectangle(g,inner);
    }
}

internal static class OptionGlyph
{
    internal static void Draw(Graphics g,VisualStyleRenderer theme,Point location,Rectangle textBounds,
        string? text,Font? font,TextFormatFlags flags,Image? image,Rectangle imageBounds,bool focused,bool matchApplication)
    {
        var bounds=new Rectangle(location,theme.GetPartSize(g,ThemeSizeType.True));
        if (matchApplication && !Application.RenderWithVisualStyles)
        {
            int interaction=(theme.State-1)%4+1;
            var state=interaction==4 ? ButtonState.Inactive : interaction==3 ? ButtonState.Pushed : ButtonState.Normal;
            if (theme.State>=5) state|=ButtonState.Checked;
            ControlPaint.DrawClassicGlyph(g,bounds,theme.Part==2,state,theme.State>=9);
        }
        else theme.DrawBackground(g,bounds);
        if (image!=null) g.DrawImage(image,imageBounds);
        VisualStyleRenderer.PaintText(g,textBounds,text,font,theme.GetColor(ColorProperty.TextColor),flags);
        if (focused) ControlPaint.DrawFocusRectangle(g,textBounds);
    }
}

public static class CheckBoxRenderer
{
    public static bool RenderMatchingApplicationState { get; set; } = true;
    private static VisualStyleRenderer Theme(CheckBoxState state) => new("BUTTON",3,(int)state);
    public static bool IsBackgroundPartiallyTransparent(CheckBoxState state) => Theme(state).IsBackgroundPartiallyTransparent();
    public static void DrawParentBackground(Graphics g,Rectangle bounds,Control childControl) =>
        Theme(CheckBoxState.UncheckedNormal).DrawParentBackground(g,bounds,childControl);
    public static Size GetGlyphSize(Graphics g,CheckBoxState state) => Theme(state).GetPartSize(g,ThemeSizeType.True);
    public static void DrawCheckBox(Graphics g,Point location,CheckBoxState state) =>
        OptionGlyph.Draw(g,Theme(state),location,Rectangle.Empty,null,null,ButtonRenderer.Center,null,Rectangle.Empty,false,RenderMatchingApplicationState);
    public static void DrawCheckBox(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,bool focused,CheckBoxState state) =>
        DrawCheckBox(g,location,textBounds,text,font,ButtonRenderer.Center,focused,state);
    public static void DrawCheckBox(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,TextFormatFlags flags,bool focused,CheckBoxState state) =>
        OptionGlyph.Draw(g,Theme(state),location,textBounds,text,font,flags,null,Rectangle.Empty,focused,RenderMatchingApplicationState);
    public static void DrawCheckBox(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,Image image,Rectangle imageBounds,bool focused,CheckBoxState state) =>
        DrawCheckBox(g,location,textBounds,text,font,ButtonRenderer.Center,image,imageBounds,focused,state);
    public static void DrawCheckBox(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,TextFormatFlags flags,Image image,Rectangle imageBounds,bool focused,CheckBoxState state) =>
        OptionGlyph.Draw(g,Theme(state),location,textBounds,text,font,flags,image,imageBounds,focused,RenderMatchingApplicationState);
}

public static class RadioButtonRenderer
{
    public static bool RenderMatchingApplicationState { get; set; } = true;
    private static VisualStyleRenderer Theme(RadioButtonState state) => new("BUTTON",2,(int)state);
    public static bool IsBackgroundPartiallyTransparent(RadioButtonState state) => Theme(state).IsBackgroundPartiallyTransparent();
    public static void DrawParentBackground(Graphics g,Rectangle bounds,Control childControl) =>
        Theme(RadioButtonState.UncheckedNormal).DrawParentBackground(g,bounds,childControl);
    public static Size GetGlyphSize(Graphics g,RadioButtonState state) => Theme(state).GetPartSize(g,ThemeSizeType.True);
    public static void DrawRadioButton(Graphics g,Point location,RadioButtonState state) =>
        OptionGlyph.Draw(g,Theme(state),location,Rectangle.Empty,null,null,ButtonRenderer.Center,null,Rectangle.Empty,false,RenderMatchingApplicationState);
    public static void DrawRadioButton(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,bool focused,RadioButtonState state) =>
        DrawRadioButton(g,location,textBounds,text,font,ButtonRenderer.Center,focused,state);
    public static void DrawRadioButton(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,TextFormatFlags flags,bool focused,RadioButtonState state) =>
        OptionGlyph.Draw(g,Theme(state),location,textBounds,text,font,flags,null,Rectangle.Empty,focused,RenderMatchingApplicationState);
    public static void DrawRadioButton(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,Image image,Rectangle imageBounds,bool focused,RadioButtonState state) =>
        DrawRadioButton(g,location,textBounds,text,font,ButtonRenderer.Center,image,imageBounds,focused,state);
    public static void DrawRadioButton(Graphics g,Point location,Rectangle textBounds,string? text,Font? font,TextFormatFlags flags,Image image,Rectangle imageBounds,bool focused,RadioButtonState state) =>
        OptionGlyph.Draw(g,Theme(state),location,textBounds,text,font,flags,image,imageBounds,focused,RenderMatchingApplicationState);
}
