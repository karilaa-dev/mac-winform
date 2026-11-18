using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Resources;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        var resources=new ComponentResourceManager(typeof(Fixture));
        using var icon=(Icon)resources.GetObject("$this.Icon")!;
        using var image=(Bitmap)resources.GetObject("button.Image")!;
        using var linked=(Bitmap)resources.GetObject("linked.Image")!;
        using var stream=(ImageListStreamer)resources.GetObject("smallImages.ImageStream")!;
        using var images=new ImageList { ImageStream=stream };
        images.Images.SetKeyName(1,"second");
        if (icon.Width<16 || image.Size!=new Size(16,16) || linked.Size!=new Size(16,16) ||
            images.Images.Count!=2 || images.Images["second"]!.Size!=new Size(16,16))
            throw new InvalidOperationException("Designer image resources mismatch.");
        if (resources.GetString("label.Text")!="Neutral" || resources.GetString("label.Text",new CultureInfo("fr"))!="Français" ||
            resources.GetObject("count") is not 37) throw new InvalidOperationException("Scalar and satellite resources mismatch.");
        using var converted = new Bitmap(images.Images[0]);
        int red=0,transparent=0;
        for (int x=0;x<converted.Width;x++)
            for (int y=0;y<converted.Height;y++)
            {
                var c=converted.GetPixel(x,y);
                if (c.A==255 && c.R==255 && c.G==51 && c.B==51) red++;
                if (c.A==0) transparent++;
            }
        if (red!=38 || transparent!=0) throw new InvalidOperationException($"Unexpected first image-list frame. Red={red}, transparent={transparent}");
        using var form=new Fixture { Icon=icon };
        form.CreateControl();
        form.ShowIcon=false;
        form.ShowIcon=true;
        Console.WriteLine("PASS: stock ResourceManager loads embedded and linked bitmaps, icon, Windows image-list frames, strings, integer and satellite.");
    }
}
internal class Fixture : Form { }
