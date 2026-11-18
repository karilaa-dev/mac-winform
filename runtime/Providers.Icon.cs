using System.Drawing.Imaging;
using System.Reflection;

namespace System.Drawing;

public sealed class Icon : ICloneable, IDisposable
{
    internal Bitmap Bitmap { get; }
    public Icon(string fileName) : this(new Bitmap(fileName)) { }
    public Icon(Stream stream) : this(new Bitmap(stream)) { }
    public Icon(Icon original, Size size) : this(new Bitmap(original.Bitmap, size)) { }
    public Icon(Icon original, int width, int height) : this(original, new Size(width, height)) { }
    public Icon(string fileName, Size size) : this(Resize(File.OpenRead(fileName), size, true)) { }
    public Icon(string fileName, int width, int height) : this(fileName,new Size(width,height)) { }
    public Icon(Stream stream, Size size) : this(Resize(stream,size,false)) { }
    public Icon(Stream stream, int width, int height) : this(stream,new Size(width,height)) { }
    public Icon(Type type,string resource) : this(ReadResource(type,resource)) { }
    internal Icon(Bitmap bitmap) => Bitmap=bitmap;
    private static Bitmap ReadResource(Type type,string resource)
    {
        using var stream=type.Assembly.GetManifestResourceStream(type,resource) ?? throw new FileNotFoundException(resource);
        return new(stream);
    }
    private static Bitmap Resize(Stream stream,Size size,bool own)
    {
        try { using var image=new Bitmap(stream); return new(image,size); }
        finally { if (own) stream.Dispose(); }
    }
    public int Width => Bitmap.Width;
    public int Height => Bitmap.Height;
    public Size Size => Bitmap.Size;
    public Bitmap ToBitmap() => new(Bitmap);
    public object Clone() => new Icon(new Bitmap(Bitmap));
    public void Dispose() => Bitmap.Dispose();
    public void Save(Stream outputStream)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        if (Width>256 || Height>256) throw new InvalidOperationException("ICO images must fit within 256 × 256 pixels.");
        using var data=new MemoryStream();
        Bitmap.Save(data,ImageFormat.Png);
        using var writer=new BinaryWriter(outputStream,System.Text.Encoding.UTF8,true);
        writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)1);
        writer.Write((byte)(Width%256)); writer.Write((byte)(Height%256)); writer.Write((ushort)0);
        writer.Write((ushort)1); writer.Write((ushort)32); writer.Write((uint)data.Length); writer.Write((uint)22);
        data.Position=0; data.CopyTo(outputStream);
    }
}
