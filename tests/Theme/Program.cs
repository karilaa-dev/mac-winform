using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Theme;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();
        var descriptor = VisualStyleElement.Button.CheckBox.MixedDisabled;
        Check(descriptor.ClassName == "BUTTON" && descriptor.Part == 3 && descriptor.State == 12, "button theme descriptor");
        Check(VisualStyleElement.Window.FrameBottomSizingTemplate.Normal.Part == 36, "window theme descriptor");
        Check(VisualStyleRenderer.IsElementDefined(VisualStyleElement.Button.PushButton.Normal), "button renderer discovery");
        Check(!VisualStyleRenderer.IsElementDefined(VisualStyleElement.CreateElement("UNREGISTERED", 99, 1)), "unknown theme");
        var theme = new VisualStyleRenderer(VisualStyleElement.Button.PushButton.Normal);
        using var image = new Bitmap(90, 80);
        using var g = Graphics.FromImage(image);
        g.Clear(Color.Magenta);
        var box = new Rectangle(2, 2, 32, 16);
        theme.DrawBackground(g, box);
        var normal = image.GetPixel(10, 10);
        theme.SetParameters(VisualStyleElement.Button.PushButton.Pressed);
        theme.DrawBackground(g, new Rectangle(36, 2, 32, 16));
        Check(normal != image.GetPixel(42, 10) && normal.ToArgb() != Color.Magenta.ToArgb(), "pressed button appearance");
        Check(theme.GetBackgroundExtent(g, theme.GetBackgroundContentRectangle(g, box)) == box, "content inset");
        theme.SetParameters(VisualStyleElement.Button.CheckBox.UncheckedNormal);
        var size = theme.GetPartSize(g, ThemeSizeType.True);
        theme.DrawBackground(g, new Rectangle(new Point(2, 22), size));
        theme.SetParameters(VisualStyleElement.Button.CheckBox.CheckedNormal);
        theme.DrawBackground(g, new Rectangle(new Point(26, 22), size));
        int differences = 0;
        for (int y = 0; y < size.Height; y++)
            for (int x = 0; x < size.Width; x++)
                if (image.GetPixel(x + 2, y + 22) != image.GetPixel(x + 26, y + 22)) differences++;
        Check(differences > 8, "checkmark pixels");
        RadioButtonRenderer.DrawRadioButton(g, new Point(2, 22), RadioButtonState.CheckedNormal);
        CheckBoxRenderer.DrawCheckBox(g, new Point(26, 22), CheckBoxState.MixedNormal);
        Check(image.GetPixel(8, 28) != image.GetPixel(4, 28) && image.GetPixel(32, 28) != image.GetPixel(32, 25),
            "radio dot and mixed checkbox contrast with their surround");
        if (OperatingSystem.IsMacOS())
        {
            theme.SetParameters(VisualStyleElement.Button.CheckBox.MixedNormal);
            Check(image.GetPixel(32, 25).ToArgb() == theme.GetColor(ColorProperty.FillColor).ToArgb() &&
                image.GetPixel(32, 28).GetBrightness() > .8, "Windows blue theme fill and light mixed mark");
            // Compare the renderer with the sampled Windows default accent and field colors.
            theme.SetParameters(VisualStyleElement.TextBox.TextEdit.Focused);
            theme.DrawBackground(g, new Rectangle(52, 22, 25, 15), new Rectangle(52, 22, 12, 15));
            Check(image.GetPixel(52, 22).ToArgb() == theme.GetColor(ColorProperty.BorderColor).ToArgb(), "edit border matches theme property");
            Check(image.GetPixel(56, 27).ToArgb() == Color.White.ToArgb() &&
                image.GetPixel(66, 27).ToArgb() == Color.Magenta.ToArgb(), "white field and background clipping");
        }
        TabRenderer.DrawTabItem(g, new Rectangle(2, 42, 34, 24), "Tab", null, true, TabItemState.Selected);
        ButtonRenderer.DrawButton(g, new Rectangle(38, 42, 44, 24), "Button", null, true, PushButtonState.Default);
        Check(image.GetPixel(6, 44).ToArgb() != Color.Magenta.ToArgb(), "tab rendering");
        int ink = 0;
        for (int y = 47; y < 63; y++)
            for (int x = 43; x < 77; x++)
                if (image.GetPixel(x,y).GetBrightness() < .5) ink++;
        Check(ink > 8, "centered text is drawn");
        ProgressBarRenderer.DrawHorizontalBar(g, new Rectangle(2, 68, 80, 9));
        ProgressBarRenderer.DrawHorizontalChunks(g, new Rectangle(4, 70, 30, 5));
        Check(image.GetPixel(10, 72) != image.GetPixel(50, 72), "progress fill");
        using var font = new Font("Arial", 12);
        var plain = TextRenderer.MeasureText(g, "Save", font, Size.Empty, TextFormatFlags.NoPadding);
        var padded = TextRenderer.MeasureText("Save".AsSpan(), font, Size.Empty, TextFormatFlags.Default);
        var wrapped = TextRenderer.MeasureText(g, "Save more time", font, new Size(45, 120), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        Check(padded.Width > plain.Width && wrapped.Height > plain.Height, "padding and multiline measurement");
        Check(TextRenderer.MeasureText(g, "&Save", font, Size.Empty, TextFormatFlags.NoPadding) == plain, "mnemonic measurement");
        using var caption = new Bitmap(140, 40);
        using var c = Graphics.FromImage(caption);
        c.Clear(Color.White);
        TextRenderer.DrawText(c, "Save".AsSpan(), font, new Rectangle(0, 0, 130, 40), Color.Black, Color.Yellow,
            TextFormatFlags.SingleLine | TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        int black = 0, yellow = 0;
        for (int y = 0; y < 40; y++)
        {
            for (int x = 0; x < 140; x++)
            {
                var pixel = caption.GetPixel(x,y);
                if (pixel.GetBrightness() < .4 || pixel.ToArgb() == Color.Yellow.ToArgb())
                    Check(x > 60 && x < 130 && y > 3 && y < 38, "text alignment and clipping");
                if (pixel.GetBrightness() < .4) black++;
                if (pixel.ToArgb() == Color.Yellow.ToArgb()) yellow++;
            }
        }
        Check(black > 10 && yellow > 10, "foreground and background glyph rendering");
        Console.WriteLine("PASS: theme descriptors, states, glyphs, tabs, progress, TextRenderer drawing, flags and measurement.");
        return 0;
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
