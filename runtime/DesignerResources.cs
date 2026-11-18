using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace MacWinForms.Resources
{
    // ResourceManager creates this reader using the header in the manifest resource.
    public sealed class DesignerReader : IResourceReader
    {
        private const string TypesKey = "__MacWinForms_DesignerTypes_v1__";
        private readonly Hashtable values = new(StringComparer.Ordinal);
        public DesignerReader(Stream stream)
        {
            using var header = new BinaryReader(stream,Encoding.UTF8);
            stream.Position = 0;
            if (header.ReadUInt32()!=0xbeefcace || header.ReadInt32()!=1) throw new InvalidDataException("Invalid resource header.");
            int headerSize=header.ReadInt32();
            if (headerSize < 0 || headerSize > stream.Length-stream.Position) throw new InvalidDataException("Invalid resource header size.");
            stream.Position += headerSize;
            // ResourceReader's data offsets are absolute within the inner, stock SDK resource file.
            using var inner = new MemoryStream();
            stream.CopyTo(inner);
            inner.Position=0;
            using var reader=new ResourceReader(inner);
            var entries=reader.GetEnumerator();
            while (entries.MoveNext())
            {
                var key=(string)entries.Key;
                reader.GetResourceData(key,out string kind,out _);
                if (!kind.StartsWith("ResourceTypeCode.",StringComparison.Ordinal))
                    throw new NotSupportedException($"Resource '{key}' uses unsupported type '{kind}'.");
                values.Add(key,entries.Value);
            }
            if (values[TypesKey] is not string json) throw new InvalidDataException("Missing designer resource type table.");
            values.Remove(TypesKey);
            foreach (var (key,type) in JsonSerializer.Deserialize<Dictionary<string,string>>(json)!)
            {
                if (values[key] is not string encoded) throw new InvalidDataException("Invalid designer resource " + key);
                var scalar = type switch
                {
                    "System.Boolean" => typeof(bool), "System.Byte" => typeof(byte), "System.SByte" => typeof(sbyte),
                    "System.Int16" => typeof(short), "System.UInt16" => typeof(ushort), "System.Int32" => typeof(int),
                    "System.UInt32" => typeof(uint), "System.Int64" => typeof(long), "System.UInt64" => typeof(ulong),
                    "System.Char" => typeof(char), "System.Decimal" => typeof(decimal), "System.Double" => typeof(double),
                    "System.Single" => typeof(float), "System.DateTime" => typeof(DateTime), "System.TimeSpan" => typeof(TimeSpan),
                    "System.Drawing.Point" => typeof(Point), "System.Drawing.PointF" => typeof(PointF),
                    "System.Drawing.Size" => typeof(Size), "System.Drawing.SizeF" => typeof(SizeF),
                    "System.Drawing.Rectangle" => typeof(Rectangle), "System.Drawing.Color" => typeof(Color),
                    _ => null
                };
                if (scalar != null)
                {
                    values[key] = TypeDescriptor.GetConverter(scalar).ConvertFromInvariantString(encoded);
                    continue;
                }
                byte[] bytes = Convert.FromBase64String(encoded);
                using var input=new MemoryStream(bytes,writable:false);
                values[key] = type switch
                {
                    "System.Drawing.Image" or "System.Drawing.Bitmap" => new Bitmap(input),
                    "System.Drawing.Icon" => new Icon(input),
                    "System.Windows.Forms.ImageListStreamer" => new ImageListStreamer(input),
                    "System.Byte[]" => bytes,
                    _ => throw new NotSupportedException("Unsupported designer resource type "+type)
                };
            }
        }
        public IDictionaryEnumerator GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Close() => Dispose();
        public void Dispose() => values.Clear();
    }
    public sealed class DesignerResourceSet : ResourceSet
    {
        public DesignerResourceSet(IResourceReader reader) : base(reader) {}
        public DesignerResourceSet(Stream stream) : base(new DesignerReader(stream)) {}
    }
}
namespace System.Windows.Forms
{
    public sealed class ImageListStreamer : IDisposable
    {
        internal readonly Size ImageSize;
        internal readonly Bitmap[] Frames;
        internal ImageListStreamer(Stream input)
        {
            using var data = new BinaryReader(input,Encoding.UTF8,leaveOpen:true);
            ImageSize=new(data.ReadInt32(),data.ReadInt32());
            int count=data.ReadInt32();
            if (ImageSize.Width<=0 || ImageSize.Height<=0 || count<0 || count>(input.Length-input.Position)/4)
                throw new InvalidDataException("Invalid image list stream.");
            Frames = new Bitmap[count];
            for (int i=0; i<count; i++)
            {
                int length=data.ReadInt32();
                if (length<1 || length>input.Length-input.Position) throw new InvalidDataException("Invalid image list frame.");
                using var frame=new MemoryStream(data.ReadBytes(length),false);
                Frames[i]=new Bitmap(frame);
            }
        }
        internal ImageListStreamer(ImageList list)
        { ImageSize=list.ImageSize; Frames=list.Images.Select(x=>new Bitmap(x)).ToArray(); }
        public void Dispose() { foreach (var frame in Frames) frame?.Dispose(); }
    }
}

namespace System.Windows.Forms
{
    public partial class Form
    {
        [System.Runtime.InteropServices.DllImport("MacForms")]
        private static extern void mf_form_icon(IntPtr handle,IntPtr image);
        private static readonly Lazy<Icon> defaultCaptionIcon = new(() =>
        {
            using var stream = typeof(Form).Assembly.GetManifestResourceStream("wfc.ico")!;
            return new Icon(stream,new Size(16,16));
        });
        private Icon? icon;
        private bool showIcon = true;
        public Icon? Icon { get => icon; set { icon=value; SetWindowIcon(); } }
        public bool ShowIcon { get => showIcon; set { showIcon=value; SetWindowIcon(); } }
        private void SetWindowIcon()
        {
            if (NativeHandle==0 || IsMdiChild) return;
            Icon? image = showIcon && FormBorderStyle != FormBorderStyle.None
                ? icon ?? (FormBorderStyle == FormBorderStyle.FixedDialog ? null : defaultCaptionIcon.Value) : null;
            mf_form_icon(NativeHandle,image?.Bitmap.Handle.DangerousGetHandle() ?? 0);
        }
        protected override void OnHandleCreated(EventArgs e) { SetWindowIcon(); base.OnHandleCreated(e); }
    }
}
