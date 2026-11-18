using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

internal static class Program
{
    static void Expect(Bitmap bitmap,int x,int y,Color color)
    {
        var actual=bitmap.GetPixel(x,y);
        if (actual.ToArgb()!=color.ToArgb()) throw new Exception($"Pixel ({x},{y}): expected {color}, got {actual}");
    }
    [STAThread]
    public static void Main(string[] args)
    {
        using var image=new Bitmap(64,48);
        using var second=new Bitmap(64,48);
        using (var a=Graphics.FromImage(image))
        using (var b=Graphics.FromImage(second))
        {
            a.Clear(Color.White);
            b.Clear(Color.Green);
            var state=a.Save();
            a.TranslateTransform(6,8);
            a.ScaleTransform(2,2);
            a.FillRectangle(Brushes.Blue,0,0,6,5);
            b.FillRectangle(Brushes.Red,4,4,4,4);
            a.Restore(state);
            a.FillPolygon(Brushes.Red,new Point[] { new(25,2),new(47,2),new(36,14) });
            a.FillPie(Brushes.Green,new Rectangle(20,20,20,20),0,90);
            a.SetClip(new Rectangle(52,2,4,4));
            a.FillRectangle(Brushes.Blue,48,0,16,12);
            a.ResetClip();
            var size=a.MeasureString("WinForms",new Font("Arial",12));
            if (size.Width<20 || size.Height<5) throw new Exception("Invalid text measurement.");
            a.DrawString("Test",new Font("Arial",8),Brushes.Black,new PointF(0,35));
        }
        Expect(image,7,9,Color.Blue); Expect(image,16,16,Color.Blue);
        Expect(image,19,19,Color.White); Expect(image,36,5,Color.Red);
        Expect(image,33,33,Color.Green); Expect(image,27,27,Color.White);
        Expect(image,53,3,Color.Blue); Expect(image,58,3,Color.White);
        Expect(second,5,6,Color.Red); Expect(second,16,16,Color.Green);
        image.SetPixel(1,1,Color.FromArgb(128,0,0,255));
        Expect(image,1,1,Color.FromArgb(128,0,0,255));

        foreach (var format in new[]{ImageFormat.Png,ImageFormat.Bmp,ImageFormat.Jpeg})
        {
            using var stream=new MemoryStream();
            image.Save(stream,format);
            stream.Position=0;
            using var loaded=new Bitmap(stream);
            if (loaded.Size!=image.Size || !loaded.RawFormat.Equals(format))
                throw new Exception($"Encoded format or size mismatch for {format.Guid}");
            if (format.Equals(ImageFormat.Png)) Expect(loaded,1,1,Color.FromArgb(128,0,0,255));
            if (format.Equals(ImageFormat.Jpeg))
            {
                var red=loaded.GetPixel(36,5);
                if (red.R<160 || red.G>100 || red.B>100) throw new Exception($"JPEG encoder produced unexpected red pixel: {red}");
            }
            else Expect(loaded,10,12,Color.Blue);
        }
        using var crop=new Bitmap(6,6);
        using (var g=Graphics.FromImage(crop))
            g.DrawImage(image,new Rectangle(0,0,6,6),new Rectangle(8,9,6,6),GraphicsUnit.Pixel);
        Expect(crop,3,3,Color.Blue);
        if (args.Length==1) image.Save(args[0],ImageFormat.Png);
        Console.WriteLine("PASS: independent contexts, transforms, state restore, clip, polygon/pie, text measurement, RGBA, source cropping and PNG/JPEG/BMP round trips.");
    }
}
