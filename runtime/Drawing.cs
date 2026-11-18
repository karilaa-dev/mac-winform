using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace System.Drawing
{
    internal static class Canvas
    {
        private const string Lib = "MacForms";
        [DllImport(Lib)] internal static extern void mf_shape(int kind,int color,float lineWidth,float x,float y,float w,float h);
        [DllImport(Lib)] internal static extern void mf_polygon(PointF[] points,int count,int close,int winding,int argb,float width);
        [DllImport(Lib)] internal static extern void mf_arc(float x,float y,float w,float h,float angle,float sweep,int pie,int color,float stroke);
        [DllImport(Lib)] internal static extern void mf_clear(int color,float width,float height);
        [DllImport(Lib)] internal static extern void mf_draw_string(string text,string family,float size,int style,int color,float x,float y,float width,float height);
        [DllImport(Lib)] internal static extern void mf_measure_string(string text,string family,float size,int style,float width,float height,out float w,out float h);
        [DllImport(Lib)] internal static extern void mf_format_text(string text,string family,float size,int style,int ink,int background,
            int flags,float x,float y,float width,float height,int draw,out float w,out float h);
        [DllImport(Lib)] internal static extern ImageHandle mf_image_load(byte[] bytes,int length);
        [DllImport(Lib)] internal static extern ImageHandle mf_image_create(int width,int height);
        [DllImport(Lib)] internal static extern int mf_image_size(DrawingHandle image,out int w,out int h);
        [DllImport(Lib)] internal static extern void mf_image_draw(DrawingHandle image,float x,float y,float w,float h,float sx,float sy,float sw,float sh);
        [DllImport(Lib)] internal static extern int mf_pixel_get(DrawingHandle image,int x,int y);
        [DllImport(Lib)] internal static extern void mf_pixel_set(DrawingHandle image,int x,int y,int color);
        [DllImport(Lib)] internal static extern IntPtr mf_image_encode(DrawingHandle image,int format,out int size);
        [DllImport(Lib)] internal static extern DrawingHandle mf_graphics_current();
        [DllImport(Lib)] internal static extern void mf_draw_choice(int x,int y,int width,int height,int radio,int visualState);
        [DllImport(Lib)] internal static extern DrawingHandle mf_graphics_image(DrawingHandle image);
        [DllImport(Lib)] internal static extern void mf_graphics_begin(DrawingHandle graphics,in Matrix3x2 transform,int useClip,float x,float y,float w,float h,int smooth);
        [DllImport(Lib)] internal static extern void mf_graphics_end();
        [DllImport(Lib)] internal static extern void mf_drawing_release(IntPtr handle);
        [DllImport(Lib)] internal static extern void mf_free(IntPtr pointer);
    }
    internal class DrawingHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public DrawingHandle() : base(true) {}
        protected override bool ReleaseHandle() { Canvas.mf_drawing_release(handle); return true; }
    }

    internal sealed class ImageHandle : DrawingHandle { public ImageHandle() {} }

    public abstract class Image : IDisposable, ICloneable
    {
        internal ImageHandle Handle { get; }
        internal Image(ImageHandle handle)
        {
            Handle = handle;
            if (handle.IsInvalid) throw new ArgumentException("Could not create the image.");
            int code = Canvas.mf_image_size(Handle,out int width,out int height);
            Size = new(width,height);
            RawFormat = code switch { 1 => ImageFormat.Png, 2 => ImageFormat.Jpeg, 3 => ImageFormat.Bmp, _ => ImageFormat.MemoryBmp };
        }
        public int Width => Size.Width;
        public int Height => Size.Height;
        public Size Size { get; }
        public SizeF PhysicalDimension => Size;
        public ImageFormat RawFormat { get; }
        public PixelFormat PixelFormat => PixelFormat.Format32bppArgb;
        public static Image FromFile(string filename) => new Bitmap(filename);
        public static Image FromStream(Stream stream) => new Bitmap(stream);
        public void Save(string filename) => Save(filename,RawFormat==ImageFormat.MemoryBmp ? ImageFormat.Png : RawFormat);
        public void Save(string filename,ImageFormat format)
        {
            // Encode before opening the destination so unsupported formats preserve existing files.
            byte[] bytes=Encode(format);
            File.WriteAllBytes(filename,bytes);
        }
        public void Save(Stream stream,ImageFormat format) { ArgumentNullException.ThrowIfNull(stream); stream.Write(Encode(format)); }
        private byte[] Encode(ImageFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);
            int id=format.Equals(ImageFormat.Png) ? 1 : format.Equals(ImageFormat.Jpeg) ? 2 : format.Equals(ImageFormat.Bmp) ? 3 :
                throw new NotSupportedException("The Mac image encoder supports PNG, JPEG and BMP.");
            IntPtr data=Canvas.mf_image_encode(Handle,id,out int length);
            if (data==0) throw new ExternalException("ImageIO could not encode the image.");
            try { var result=new byte[length]; Marshal.Copy(data,result,0,length); return result; }
            finally { Canvas.mf_free(data); }
        }
        public object Clone() => new Bitmap(this);
        public void Dispose() => Handle.Dispose();
    }
    public sealed class Bitmap : Image
    {
        public Bitmap(string filename) : this(File.ReadAllBytes(filename)) {}
        public Bitmap(Stream stream) : this(Read(stream)) {}
        private Bitmap(byte[] bytes) : base(Canvas.mf_image_load(bytes,bytes.Length)) {}
        public Bitmap(int width,int height) : base(Create(width,height)) {}
        public Bitmap(Image original) : this(original,original.Width,original.Height) {}
        public Bitmap(Image original,Size newSize) : this(original,newSize.Width,newSize.Height) {}
        public Bitmap(Image original,int width,int height) : this(width,height)
        {
            using var graphics=Graphics.FromImage(this);
            graphics.DrawImage(original,0,0,width,height);
        }
        private static ImageHandle Create(int width,int height)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
            return Canvas.mf_image_create(width,height);
        }
        private static byte[] Read(Stream stream) { using var data=new MemoryStream(); stream.CopyTo(data); return data.ToArray(); }
        private void CheckPoint(int x,int y)
        {
            if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
            if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        }
        public Color GetPixel(int x,int y) { CheckPoint(x,y); return Color.FromArgb(Canvas.mf_pixel_get(Handle,x,y)); }
        public void SetPixel(int x,int y,Color color) { CheckPoint(x,y); Canvas.mf_pixel_set(Handle,x,y,color.ToArgb()); }
    }
    public abstract class Brush : IDisposable
    {
        internal abstract Color Ink { get; }
        public void Dispose() {}
    }
    public sealed class SolidBrush(Color color) : Brush
    {
        public Color Color { get; set; } = color;
        internal override Color Ink => Color;
    }
    public sealed class Pen(Color color,float width = 1) : IDisposable
    {
        public Color Color { get; set; } = color;
        public float Width { get; set; } = width;
        public void Dispose() {}
    }
    public static class Brushes
    {
        public static Brush Black => new SolidBrush(Color.Black);
        public static Brush White => new SolidBrush(Color.White);
        public static Brush Blue => new SolidBrush(Color.Blue);
        public static Brush Red => new SolidBrush(Color.Red);
        public static Brush Green => new SolidBrush(Color.Green);
    }

    public enum GraphicsUnit { World, Display, Pixel, Point, Inch, Document, Millimeter }

    public interface IDeviceContext : IDisposable
    {
        IntPtr GetHdc();
        void ReleaseHdc();
    }
    public sealed class Graphics : IDeviceContext
    {
        public IntPtr GetHdc() => throw new PlatformNotSupportedException("Win32 device context handles are unavailable on macOS.");
        public void ReleaseHdc() => throw new PlatformNotSupportedException("Win32 device context handles are unavailable on macOS.");
        private readonly DrawingHandle context;
        private readonly Rectangle surface;
        private Matrix3x2 transform=Matrix3x2.Identity;
        private RectangleF? deviceClip;
        private readonly List<GraphicsState> saved=new();
        public float DpiX { get; }
        public float DpiY => DpiX;
        internal Graphics(Rectangle bounds,float dpi=96) : this(Canvas.mf_graphics_current(),bounds,dpi) {}
        private Graphics(DrawingHandle handle,Rectangle bounds,float dpi=96)
        {
            context=handle;
            surface=bounds;
            DpiX=dpi;
            if (handle.IsInvalid) throw new InvalidOperationException("No active drawing context.");
        }
        public static Graphics FromImage(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);
            return new Graphics(Canvas.mf_graphics_image(image.Handle),new(Point.Empty,image.Size));
        }
        public void Dispose() => context.Dispose();

        internal void DrawChoice(Rectangle bounds,bool radio,int visualState)
        {
            using var scope=Enter();
            Canvas.mf_draw_choice(bounds.X,bounds.Y,bounds.Width,bounds.Height,radio ? 1 : 0,visualState);
        }

        // Each drawing command installs its own target only for that command. Offscreen surfaces,
        // paint callbacks and nested print previews can therefore be interleaved on the same thread.
        private readonly struct Scope : IDisposable { public void Dispose() => Canvas.mf_graphics_end(); }
        private Scope Enter(bool identity=false)
        {
            var matrix=identity ? Matrix3x2.Identity : transform;
            RectangleF clip=deviceClip ?? surface;
            int smooth=SmoothingMode switch { SmoothingMode.None or SmoothingMode.HighSpeed => 0, SmoothingMode.AntiAlias or SmoothingMode.HighQuality => 1, _ => -1 };
            Canvas.mf_graphics_begin(context,in matrix,deviceClip.HasValue?1:0,clip.X,clip.Y,clip.Width,clip.Height,smooth);
            return new Scope();
        }

        public SmoothingMode SmoothingMode { get; set; }
        public Matrix Transform { get => new(transform); set => transform=value.Value; }
        public void ResetTransform() => transform=Matrix3x2.Identity;
        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix,MatrixOrder.Prepend);
        public void MultiplyTransform(Matrix matrix,MatrixOrder order) => transform=Matrix.Compose(transform,matrix.Value,order);
        public void TranslateTransform(float dx,float dy) => TranslateTransform(dx,dy,MatrixOrder.Prepend);
        public void TranslateTransform(float dx,float dy,MatrixOrder order) => transform=Matrix.Compose(transform,Matrix3x2.CreateTranslation(dx,dy),order);
        public void RotateTransform(float angle) => RotateTransform(angle,MatrixOrder.Prepend);
        public void RotateTransform(float angle,MatrixOrder order) => transform=Matrix.Compose(transform,Matrix3x2.CreateRotation(MathF.PI*angle/180),order);
        public void ScaleTransform(float sx,float sy) => ScaleTransform(sx,sy,MatrixOrder.Prepend);
        public void ScaleTransform(float sx,float sy,MatrixOrder order) => transform=Matrix.Compose(transform,Matrix3x2.CreateScale(sx,sy),order);
        public GraphicsState Save()
        {
            var state=new GraphicsState(transform,deviceClip,SmoothingMode);
            saved.Add(state);
            return state;
        }
        public void Restore(GraphicsState state)
        {
            int index=saved.IndexOf(state);
            if (index<0) throw new ArgumentException("State does not belong to this graphics, or was already restored.",nameof(state));
            transform=state.Transform;
            deviceClip=state.Clip;
            SmoothingMode=state.Smoothing;
            saved.RemoveRange(index,saved.Count-index);
        }
        public void ResetClip() => deviceClip=null;
        public void SetClip(Rectangle rect) => SetClip((RectangleF)rect);
        public void IntersectClip(Rectangle rect) => IntersectClip((RectangleF)rect);
        public void IntersectClip(RectangleF rect)
        {
            RectangleF? previous = deviceClip;
            SetClip(rect);
            if (previous.HasValue) deviceClip = RectangleF.Intersect(previous.Value,deviceClip!.Value);
        }
        public void SetClip(RectangleF rect)
        {
            if (transform.M12!=0 || transform.M21!=0)
                throw new NotSupportedException("Rectangular clipping with a rotated or sheared transform is not implemented.");
            var a=Vector2.Transform(new Vector2(rect.Left,rect.Top),transform);
            var b=Vector2.Transform(new Vector2(rect.Right,rect.Bottom),transform);
            deviceClip=RectangleF.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));
        }

        public void Clear(Color color)
        {
            using var scope=Enter(identity:true);
            Canvas.mf_clear(color.ToArgb(),surface.Width,surface.Height);
        }
        private void Shape(int kind,Color color,float stroke,float x,float y,float w,float h)
        {
            using var scope=Enter();
            Canvas.mf_shape(kind,color.ToArgb(),stroke,x,y,w,h);
        }
        public void DrawLine(Pen p,float x1,float y1,float x2,float y2) => Shape(2,p.Color,p.Width,x1,y1,x2,y2);
        public void DrawLine(Pen p,Point a,Point b) => DrawLine(p,a.X,a.Y,b.X,b.Y);
        public void DrawLine(Pen p,PointF a,PointF b) => DrawLine(p,a.X,a.Y,b.X,b.Y);
        public void DrawRectangle(Pen p,float x,float y,float w,float h) => Shape(0,p.Color,p.Width,x,y,w,h);
        public void DrawRectangle(Pen p,Rectangle r) => DrawRectangle(p,r.X,r.Y,r.Width,r.Height);
        public void DrawRectangle(Pen p,RectangleF r) => DrawRectangle(p,r.X,r.Y,r.Width,r.Height);
        public void FillRectangle(Brush b,float x,float y,float w,float h) => Shape(0,b.Ink,-1,x,y,w,h);
        public void FillRectangle(Brush b,Rectangle r) => FillRectangle(b,r.X,r.Y,r.Width,r.Height);
        public void FillRectangle(Brush b,RectangleF r) => FillRectangle(b,r.X,r.Y,r.Width,r.Height);
        public void DrawEllipse(Pen p,float x,float y,float w,float h) => Shape(1,p.Color,p.Width,x,y,w,h);
        public void DrawEllipse(Pen p,Rectangle r) => DrawEllipse(p,r.X,r.Y,r.Width,r.Height);
        public void DrawEllipse(Pen p,RectangleF r) => DrawEllipse(p,r.X,r.Y,r.Width,r.Height);
        public void FillEllipse(Brush b,float x,float y,float w,float h) => Shape(1,b.Ink,-1,x,y,w,h);
        public void FillEllipse(Brush b,Rectangle r) => FillEllipse(b,r.X,r.Y,r.Width,r.Height);
        public void FillEllipse(Brush b,RectangleF r) => FillEllipse(b,r.X,r.Y,r.Width,r.Height);

        private void Points(PointF[] points,bool close,FillMode fill,Color color,float stroke)
        {
            ArgumentNullException.ThrowIfNull(points);
            if (points.Length<(close?3:2)) throw new ArgumentException("Too few points.",nameof(points));
            using var scope=Enter();
            Canvas.mf_polygon(points,points.Length,close?1:0,fill==FillMode.Winding?1:0,color.ToArgb(),stroke);
        }
        private static PointF[] FloatPoints(Point[] points) => Array.ConvertAll(points,p=>(PointF)p);
        public void DrawLines(Pen p,PointF[] points) => Points(points,false,FillMode.Alternate,p.Color,p.Width);
        public void DrawLines(Pen p,Point[] points) => DrawLines(p,FloatPoints(points));
        public void DrawPolygon(Pen p,PointF[] points) => Points(points,true,FillMode.Alternate,p.Color,p.Width);
        public void DrawPolygon(Pen p,Point[] points) => DrawPolygon(p,FloatPoints(points));
        public void FillPolygon(Brush b,PointF[] points,FillMode fillMode) => Points(points,true,fillMode,b.Ink,-1);
        public void FillPolygon(Brush b,PointF[] points) => FillPolygon(b,points,FillMode.Alternate);
        public void FillPolygon(Brush b,Point[] points,FillMode fillMode) => FillPolygon(b,FloatPoints(points),fillMode);
        public void FillPolygon(Brush b,Point[] points) => FillPolygon(b,FloatPoints(points),FillMode.Alternate);
        private void Arc(Color color,float stroke,float x,float y,float w,float h,float start,float sweep,bool pie)
        {
            using var scope=Enter();
            Canvas.mf_arc(x,y,w,h,start,Math.Clamp(sweep,-360,360),pie?1:0,color.ToArgb(),stroke);
        }
        public void DrawArc(Pen p,float x,float y,float w,float h,float startAngle,float sweepAngle) => Arc(p.Color,p.Width,x,y,w,h,startAngle,sweepAngle,false);
        public void DrawArc(Pen p,Rectangle r,float startAngle,float sweepAngle) => DrawArc(p,r.X,r.Y,r.Width,r.Height,startAngle,sweepAngle);
        public void DrawArc(Pen p,RectangleF r,float startAngle,float sweepAngle) => DrawArc(p,r.X,r.Y,r.Width,r.Height,startAngle,sweepAngle);
        public void DrawPie(Pen p,float x,float y,float w,float h,float startAngle,float sweepAngle) => Arc(p.Color,p.Width,x,y,w,h,startAngle,sweepAngle,true);
        public void DrawPie(Pen p,Rectangle r,float startAngle,float sweepAngle) => DrawPie(p,r.X,r.Y,r.Width,r.Height,startAngle,sweepAngle);
        public void FillPie(Brush b,float x,float y,float w,float h,float startAngle,float sweepAngle) => Arc(b.Ink,-1,x,y,w,h,startAngle,sweepAngle,true);
        public void FillPie(Brush b,Rectangle r,float startAngle,float sweepAngle) => FillPie(b,r.X,r.Y,r.Width,r.Height,startAngle,sweepAngle);

        public void DrawString(string? text,Font font,Brush brush,float x,float y) => DrawString(text,font,brush,new RectangleF(x,y,0,0));
        public void DrawString(string? text,Font font,Brush brush,PointF point) => DrawString(text,font,brush,point.X,point.Y);
        public void DrawString(string? text,Font font,Brush brush,RectangleF layoutRectangle)
        {
            if (string.IsNullOrEmpty(text)) return;
            using var scope=Enter();
            Canvas.mf_draw_string(text,font.Name,font.SizeInPoints*DpiY/72,(int)font.Style,brush.Ink.ToArgb(),
                layoutRectangle.X,layoutRectangle.Y,layoutRectangle.Width,layoutRectangle.Height);
        }
        public SizeF MeasureString(string? text,Font font) => MeasureString(text,font,SizeF.Empty);
        public SizeF MeasureString(string? text,Font font,int width) => MeasureString(text,font,new SizeF(width,0));
        public SizeF MeasureString(string? text,Font font,SizeF layoutArea)
        {
            if (string.IsNullOrEmpty(text)) return SizeF.Empty;
            using var scope=Enter();
            Canvas.mf_measure_string(text,font.Name,font.SizeInPoints*DpiY/72,(int)font.Style,layoutArea.Width,layoutArea.Height,out var w,out var h);
            return new(w,h);
        }
        internal void DrawFormattedText(string text,Font font,Rectangle bounds,Color ink,Color background,int flags)
        {
            using var scope=Enter();
            Canvas.mf_format_text(text,font.Name,font.SizeInPoints*DpiY/72,(int)font.Style,ink.ToArgb(),background.ToArgb(),
                flags,bounds.X,bounds.Y,bounds.Width,bounds.Height,1,out _,out _);
        }
        public void DrawImage(Image image,Rectangle r) => DrawImage(image,r.X,r.Y,r.Width,r.Height);
        public void DrawImage(Image image,RectangleF r) => DrawImage(image,r.X,r.Y,r.Width,r.Height);
        public void DrawImage(Image image,Point point) => DrawImage(image,point.X,point.Y);
        public void DrawImage(Image image,float x,float y) => DrawImage(image,x,y,image.Width,image.Height);
        public void DrawImage(Image image,int x,int y) => DrawImage(image,(float)x,(float)y);
        public void DrawImage(Image image,int x,int y,int width,int height) => DrawImage(image,(float)x,y,width,height);
        public void DrawImage(Image image,float x,float y,float width,float height) =>
            DrawImage(image,new RectangleF(x,y,width,height),new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);
        public void DrawImage(Image image,Rectangle destRect,Rectangle srcRect,GraphicsUnit srcUnit) =>
            DrawImage(image,(RectangleF)destRect,(RectangleF)srcRect,srcUnit);
        public void DrawImage(Image image,RectangleF destRect,RectangleF srcRect,GraphicsUnit srcUnit)
        {
            if (srcUnit!=GraphicsUnit.Pixel) throw new NotSupportedException("Mac source rectangles currently use pixel units.");
            using var scope=Enter();
            Canvas.mf_image_draw(image.Handle,destRect.X,destRect.Y,destRect.Width,destRect.Height,srcRect.X,srcRect.Y,srcRect.Width,srcRect.Height);
        }
    }
}

namespace System.Drawing.Drawing2D
{
    public enum FillMode { Alternate, Winding }
    public enum MatrixOrder { Prepend, Append }
    public enum SmoothingMode { Invalid=-1, Default, HighSpeed, HighQuality, None, AntiAlias }
    public sealed class GraphicsState
    {
        internal GraphicsState(Matrix3x2 transform,RectangleF? clip,SmoothingMode smoothing)
        { Transform=transform; Clip=clip; Smoothing=smoothing; }
        internal Matrix3x2 Transform { get; }
        internal RectangleF? Clip { get; }
        internal SmoothingMode Smoothing { get; }
    }
    public sealed class Matrix : IDisposable
    {
        internal Matrix3x2 Value { get; private set; }
        public Matrix() : this(Matrix3x2.Identity) {}
        internal Matrix(Matrix3x2 value) => Value=value;
        public Matrix(float m11,float m12,float m21,float m22,float dx,float dy) : this(new Matrix3x2(m11,m12,m21,m22,dx,dy)) {}
        public float[] Elements => [Value.M11,Value.M12,Value.M21,Value.M22,Value.M31,Value.M32];
        public bool IsIdentity => Value.IsIdentity;
        public float OffsetX => Value.M31;
        public float OffsetY => Value.M32;
        public bool IsInvertible => Matrix3x2.Invert(Value,out _);
        public void Invert() { if (!Matrix3x2.Invert(Value,out var inverse)) throw new ArgumentException("Matrix is singular."); Value=inverse; }
        public Matrix Clone() => new(Value);
        public void Reset() => Value=Matrix3x2.Identity;
        internal static Matrix3x2 Compose(Matrix3x2 current,Matrix3x2 next,MatrixOrder order) => order==MatrixOrder.Prepend ? next*current : current*next;
        public void Multiply(Matrix matrix,MatrixOrder order=MatrixOrder.Prepend) => Value=Compose(Value,matrix.Value,order);
        public void Translate(float dx,float dy,MatrixOrder order=MatrixOrder.Prepend) => Value=Compose(Value,Matrix3x2.CreateTranslation(dx,dy),order);
        public void Scale(float sx,float sy,MatrixOrder order=MatrixOrder.Prepend) => Value=Compose(Value,Matrix3x2.CreateScale(sx,sy),order);
        public void Rotate(float angle,MatrixOrder order=MatrixOrder.Prepend) => Value=Compose(Value,Matrix3x2.CreateRotation(angle*MathF.PI/180),order);
        public void TransformPoints(PointF[] pts)
        {
            for (int i=0;i<pts.Length;i++) { var v=Vector2.Transform(new Vector2(pts[i].X,pts[i].Y),Value); pts[i]=new(v.X,v.Y); }
        }
        public void Dispose() {}
    }
}
namespace System.Drawing.Imaging
{
    public enum PixelFormat { Format32bppArgb=2498570 }
    public sealed class ImageFormat(Guid guid)
    {
        public Guid Guid { get; }=guid;
        public static ImageFormat MemoryBmp { get; }=new(new Guid("b96b3caa-0728-11d3-9d7b-0000f81ef32e"));
        public static ImageFormat Bmp { get; }=new(new Guid("b96b3cab-0728-11d3-9d7b-0000f81ef32e"));
        public static ImageFormat Jpeg { get; }=new(new Guid("b96b3cae-0728-11d3-9d7b-0000f81ef32e"));
        public static ImageFormat Png { get; }=new(new Guid("b96b3caf-0728-11d3-9d7b-0000f81ef32e"));
        public override bool Equals(object? o) => o is ImageFormat format && format.Guid==Guid;
        public override int GetHashCode() => Guid.GetHashCode();
    }
}
