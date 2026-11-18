using System.Drawing;

namespace System.Windows.Forms;

public partial class Control
{
    // Layout coordinates use 96-DPI logical units. AppKit applies the backing
    // scale (including Retina) when rasterizing these points.
    internal void ScaleDesignerBounds(float horizontal,float vertical)
    {
        static int Scale(int original,float factor) => (int)Math.Round(original * factor);
        int X(int n) => Scale(n,horizontal);
        int Y(int n) => Scale(n,vertical);
        int height = Y(Height);
        if (this is TextBox { Multiline:false } or ComboBox)
            height = (int)Math.Round(Font.Size * 15 / 9) + 8;
        Bounds = new(X(Left),Y(Top),X(Width),height);
        MinimumSize = new(X(MinimumSize.Width),Y(MinimumSize.Height));
        Padding = new(X(Padding.Left),Y(Padding.Top),X(Padding.Right),Y(Padding.Bottom));
        Margin = new(X(Margin.Left),Y(Margin.Top),X(Margin.Right),Y(Margin.Bottom));
        foreach (var child in Controls) child.ScaleDesignerBounds(horizontal,vertical);
    }
}
public partial class Form
{
    internal void ApplyDesignerScale()
    {
        if (NativeHandle != 0 || AutoScaleDimensions.Width <= 0 || AutoScaleDimensions.Height <= 0) return;
        SizeF current;
        if (AutoScaleMode == AutoScaleMode.Font)
        {
            // The standard WinForms Segoe UI 9 pt dialog base is 7 by 15.
            current = new((float)Math.Round(7 * Font.Size / 9), (float)Math.Round(15 * Font.Size / 9));
        }
        else if (AutoScaleMode == AutoScaleMode.Dpi) current = new(96,96);
        else return;
        if (current != AutoScaleDimensions)
        {
            ScaleDesignerBounds(current.Width/AutoScaleDimensions.Width,current.Height/AutoScaleDimensions.Height);
            AutoScaleDimensions = current;
        }
    }
}
