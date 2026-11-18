using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MacWinForms.Tool;

internal static class ResourceConverter
{
    private const string TypesKey = "__MacWinForms_DesignerTypes_v1__";
    private const string ImageListType = "System.Windows.Forms.ImageListStreamer";
    private const string Serialized = "application/x-microsoft.net.object.binary.base64";
    private const string ByteArray = "application/x-microsoft.net.object.bytearray.base64";
    private static readonly HashSet<string> ScalarTypes = new(StringComparer.Ordinal)
    {
        "System.Boolean", "System.Byte", "System.SByte", "System.Int16", "System.UInt16", "System.Int32", "System.UInt32",
        "System.Int64", "System.UInt64", "System.Char", "System.Decimal", "System.Double", "System.Single", "System.DateTime",
        "System.TimeSpan", "System.Byte[]", "System.Drawing.Point", "System.Drawing.PointF", "System.Drawing.Size",
        "System.Drawing.SizeF", "System.Drawing.Rectangle", "System.Drawing.Color"
    };

    public static void Run(string action)
    {
        if (action is not ("prepare" or "wrap")) throw new ArgumentException("Expected resources prepare or resources wrap.");
        foreach (string line in File.ReadLines("MacResources.inputs"))
        {
            var tab = line.LastIndexOf('\t');
            if (tab < 1) throw new InvalidDataException("Malformed MacResources.inputs entry.");
            var source = line[..tab];
            var name = line[(tab + 1)..].Replace('\\', '/');
            if (name.Length == 0 || Path.IsPathRooted(name) || name.Split('/').Contains(".."))
                throw new InvalidDataException($"Invalid manifest resource name in {source}.");
            var staged = Path.Combine("MacResources", name + ".resx");
            if (action == "prepare") Prepare(source, staged);
            else if (File.ReadAllText(Path.ChangeExtension(staged, ".media")) == "1") Wrap(name + ".resources");
        }
    }

    private static bool IsImage(string type) =>
        type is "System.Drawing.Bitmap" or "System.Drawing.Icon" or "System.Drawing.Image";

    private static void Prepare(string source, string staged)
    {
        var xml = XDocument.Load(source, LoadOptions.PreserveWhitespace);
        var types = new Dictionary<string, string>();
        foreach (var entry in xml.Root!.Elements("data"))
        {
            string name = (string?)entry.Attribute("name") ?? throw new InvalidDataException($"{source}: resource has no name.");
            if (name == TypesKey) throw new InvalidDataException($"{source}: reserved resource key {TypesKey}.");
            if (entry.Element("value") is not {} value) continue;
            string type = ((string?)entry.Attribute("type") ?? "").Split(',')[0];
            string mime = (string?)entry.Attribute("mimetype") ?? "";
            if (ScalarTypes.Contains(type) && mime.Length == 0)
            {
                entry.SetAttributeValue("type", null);
                types[name] = type;
                continue;
            }

            try
            {
                byte[] data;
                if (type == "System.Resources.ResXFileRef")
                {
                    int separator = value.Value.IndexOf(';');
                    Require(separator >= 0, "Invalid file reference");
                    string file = Path.GetFullPath(value.Value[..separator].Replace('\\', '/'), Path.GetDirectoryName(Path.GetFullPath(source))!);
                    string rest = value.Value[(separator + 1)..];
                    type = rest.Split(',')[0].Trim();
                    if (!IsImage(type))
                    {
                        value.Value = file + ";" + rest;
                        continue;
                    }
                    data = File.ReadAllBytes(file);
                }
                else if (mime == Serialized) (type, data) = ReadImageRecord(Convert.FromBase64String(value.Value));
                else if (mime == ByteArray && (IsImage(type) || type == ImageListType))
                    data = Convert.FromBase64String(value.Value);
                else continue;

                if (type == ImageListType) data = DecodeImageList(data);
                types[name] = type;
                entry.SetAttributeValue("mimetype", null);
                entry.SetAttributeValue("type", null);
                value.Value = Convert.ToBase64String(data);
            }
            catch (Exception e) when (e is IOException or InvalidDataException or FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidDataException($"{source}, resource '{name}': {e.Message}", e);
            }
        }
        if (types.Count > 0)
            xml.Root.Add(new XElement("data", new XAttribute("name", TypesKey), new XElement("value", JsonSerializer.Serialize(types))));
        GeneratedFiles.WriteXml(staged, xml);
        GeneratedFiles.Write(Path.ChangeExtension(staged, ".media"), types.Count > 0 ? "1"u8.ToArray() : "0"u8.ToArray());
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }

    // Only accept the NRBF record sequence for a single Bitmap/ImageListStreamer containing
    // one byte array. No type loading, formatter or general object graph deserialization.
    private static (string Type, byte[] Payload) ReadImageRecord(byte[] data)
    {
        using var reader = new BinaryReader(new MemoryStream(data, writable: false), new UTF8Encoding(false, true));
        void Tag(byte expected) => Require(reader.ReadByte() == expected, "Unsupported serialized image record");
        string ReadText()
        {
            int length = reader.Read7BitEncodedInt();
            Require(length >= 0 && length <= reader.BaseStream.Length - reader.BaseStream.Position, "Invalid image record string size");
            return new UTF8Encoding(false, true).GetString(reader.ReadBytes(length));
        }

        Tag(0);
        int root = reader.ReadInt32();
        Require(reader.ReadInt32() == -1 && reader.ReadInt32() == 1 && reader.ReadInt32() == 0, "Invalid image record header");
        Tag(12);
        int library = reader.ReadInt32();
        ReadText();
        Tag(5);
        Require(reader.ReadInt32() == root, "Invalid image record root");
        var type = ReadText();
        Require(type is ImageListType or "System.Drawing.Bitmap", $"Unsupported serialized resource type {type}");
        Require(reader.ReadInt32() == 1 && ReadText().Equals("data", StringComparison.OrdinalIgnoreCase), "Expected a single image Data field");
        Tag(7); Tag(2);
        Require(reader.ReadInt32() == library, "Invalid image library reference");
        Tag(9);
        int arrayId = reader.ReadInt32();
        Tag(15);
        Require(reader.ReadInt32() == arrayId, "Invalid image data reference");
        int size = reader.ReadInt32();
        Tag(2);
        Require(size >= 0 && size == reader.BaseStream.Length - reader.BaseStream.Position - 1, "Invalid image payload size");
        var payload = reader.ReadBytes(size);
        Tag(11);
        return (type, payload);
    }

    private static void Wrap(string compiled)
    {
        using var names = new MemoryStream();
        using var fields = new BinaryWriter(names, Encoding.UTF8, true);
        fields.Write("MacWinForms.Resources.DesignerReader, MacWinForms");
        fields.Write("MacWinForms.Resources.DesignerResourceSet, MacWinForms");
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        writer.Write(0xbeefcaceu);
        writer.Write(1);
        writer.Write(checked((int)names.Length));
        writer.Write(names.ToArray());
        var resource = File.ReadAllBytes(compiled);
        if (resource.AsSpan().StartsWith(output.GetBuffer().AsSpan(0, (int)output.Length))) return;
        writer.Write(resource);
        GeneratedFiles.Write(compiled, output.ToArray());
    }

    private sealed class ListBitmap
    {
        private readonly byte[] data;
        private readonly int pixels, palette, bits, stride;
        private readonly bool topDown;
        internal readonly int Width, Height, End;

        internal ListBitmap(byte[] data, int start)
        {
            this.data = data;
            Require(data.Length - start >= 54 && data.AsSpan(start, 2).SequenceEqual("BM"u8), "Missing image list bitmap");
            int header = start + 14;
            pixels = checked(start + I32(start + 10));
            int headerSize = I32(header);
            Width = I32(header + 4);
            int height = I32(header + 8);
            bits = U16(header + 14);
            Require(headerSize >= 40 && Width > 0 && height != 0 && U16(header + 12) == 1 && I32(header + 16) == 0 &&
                bits is 1 or 4 or 8 or 24 or 32, "Unsupported image list bitmap encoding");
            Height = Math.Abs(height);
            topDown = height < 0;
            palette = checked(header + headerSize);
            stride = checked((int)(((long)Width * bits + 31) / 32 * 4));
            End = checked(pixels + Height * stride);
            Require(pixels >= palette && End <= data.Length, "Truncated image list bitmap");
        }

        private int I32(int index) => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(index));
        private int U16(int index) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(index));

        internal void Pixel(int x, int y, Span<byte> rgba)
        {
            int row = pixels + (topDown ? y : Height - 1 - y) * stride;
            int offset;
            if (bits < 24)
            {
                int bit = checked(x * bits);
                int index = (data[row + bit / 8] >> (8 - bits - bit % 8)) & ((1 << bits) - 1);
                offset = checked(palette + 4 * index);
                Require(offset <= pixels - 4, "Truncated image list palette");
            }
            else offset = row + x * (bits / 8);
            rgba[0] = data[offset + 2]; rgba[1] = data[offset + 1]; rgba[2] = data[offset];
            rgba[3] = bits == 32 ? data[offset + 3] : (byte)255;
        }
    }

    private static byte[] DecodeImageList(byte[] data)
    {
        if (data.AsSpan().StartsWith("MSFt"u8))
        {
            Require(data.Length % 2 == 0, "Invalid compressed image list");
            long length = 0;
            for (int i = 4; i < data.Length; i += 2) length += data[i];
            Require(length <= 64 * 1024 * 1024, "Compressed image list is too large");
            var decoded = new byte[(int)length];
            int offset = 0;
            for (int i = 4; i < data.Length; i += 2)
            {
                decoded.AsSpan(offset, data[i]).Fill(data[i + 1]);
                offset += data[i];
            }
            data = decoded;
        }
        Require(data.Length >= 28 && data[0] == 'I' && data[1] == 'L' && data[2] == 1 && data[3] == 1, "Unsupported image list header");
        int count = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(4));
        int width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(10));
        int height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(12));
        Require(width is >= 1 and <= 256 && height is >= 1 and <= 256 && (long)count * width * height <= 16 * 1024 * 1024,
            "Unsupported image list dimensions");
        var color = new ListBitmap(data, 28);
        var mask = data.Length == color.End ? null : new ListBitmap(data, color.End);
        Require(mask == null || (mask.Width == color.Width && mask.Height == color.Height), "Mismatched image list mask");
        int across = color.Width / width;
        Require(across > 0 && (long)(color.Height / height) * across >= count, "Invalid image list frame count");
        using var frames = new MemoryStream();
        using var writer = new BinaryWriter(frames);
        writer.Write(width); writer.Write(height); writer.Write(count);
        Span<byte> maskPixel = stackalloc byte[4];
        int rowSize = 1 + 4 * width;
        var raw = new byte[rowSize * height];
        for (int frame = 0; frame < count; frame++)
        {
            bool hasAlpha = false;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int left = frame % across * width + x, top = frame / across * height + y;
                    Span<byte> pixel = raw.AsSpan(y * rowSize + 1 + 4 * x, 4);
                    color.Pixel(left, top, pixel);
                    hasAlpha |= pixel[3] != 0;
                }
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int alpha = y * rowSize + 1 + 4 * x + 3;
                    if (!hasAlpha) raw[alpha] = 255;
                    if (mask == null) continue;
                    mask.Pixel(frame % across * width + x, frame / across * height + y, maskPixel);
                    if (maskPixel[0] != 0) raw[alpha] = 0;
                }
            var png = Png(width, height, raw);
            writer.Write(png.Length);
            writer.Write(png);
        }
        return frames.ToArray();
    }

    private static byte[] Png(int width, int height, byte[] scanlines)
    {
        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        void Chunk(ReadOnlySpan<byte> type, ReadOnlySpan<byte> payload)
        {
            Span<byte> word = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(word, payload.Length);
            output.Write(word);
            output.Write(type);
            output.Write(payload);
            uint crc = UpdateCrc(UpdateCrc(uint.MaxValue, type), payload);
            BinaryPrimitives.WriteUInt32BigEndian(word, ~crc);
            output.Write(word);
        }

        Span<byte> header = stackalloc byte[13];
        header.Clear();
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8; header[9] = 6;
        Chunk("IHDR"u8, header);
        using var compressed = new MemoryStream();
        using (var zip = new ZLibStream(compressed, CompressionLevel.Optimal, true)) zip.Write(scanlines);
        Chunk("IDAT"u8, compressed.ToArray());
        Chunk("IEND"u8, []);
        return output.ToArray();
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) == 0 ? crc >> 1 : 0xedb88320u ^ (crc >> 1);
        }
        return crc;
    }
}
