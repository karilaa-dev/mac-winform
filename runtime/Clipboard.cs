using System.Collections.Specialized;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

public static class DataFormats
{
    public static readonly string Text = "Text", UnicodeText = "UnicodeText", StringFormat = typeof(string).FullName!,
        Html = "HTML Format", Rtf = "Rich Text Format", CommaSeparatedValue = "Csv", FileDrop = "FileDrop",
        Bitmap = "Bitmap", WaveAudio = "WaveAudio";
    internal static string For(TextDataFormat format) => format switch
    {
        TextDataFormat.Text => Text, TextDataFormat.UnicodeText => UnicodeText, TextDataFormat.Html => Html,
        TextDataFormat.Rtf => Rtf, TextDataFormat.CommaSeparatedValue => CommaSeparatedValue,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
    internal static bool IsText(string name) => name == Text || name == UnicodeText || name == StringFormat;
}
public class DataObject : IDataObject
{
    private readonly Dictionary<string,(object? Value,bool Convert)> values = new(StringComparer.Ordinal);
    public DataObject() {}
    public DataObject(string format,object? data) => SetData(format,data);
    public DataObject(object data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data is IDataObject source)
            foreach (var name in source.GetFormats(false)) SetData(name,source.GetData(name,false));
        else SetData(data);
    }
    public virtual object? GetData(string format,bool autoConvert)
    {
        if (values.TryGetValue(format,out var item)) return item.Value;
        if (autoConvert && DataFormats.IsText(format))
            foreach (var entry in values)
                if (entry.Value.Convert && DataFormats.IsText(entry.Key)) return entry.Value.Value;
        return null;
    }
    public virtual object? GetData(string format) => GetData(format,true);
    public virtual object? GetData(Type format) => GetData(format.FullName!);
    public virtual void SetData(string format,bool autoConvert,object? data)
    {
        ArgumentException.ThrowIfNullOrEmpty(format);
        values[format] = (data,autoConvert);
    }
    public virtual void SetData(string format,object? data) => SetData(format,true,data);
    public virtual void SetData(Type format,object? data) => SetData(format.FullName!,data);
    public virtual void SetData(object? data) { if (data != null) SetData(data.GetType(),data); }
    public virtual string[] GetFormats(bool autoConvert)
    {
        var names = values.Keys.ToHashSet();
        if (autoConvert && values.Any(kv => kv.Value.Convert && DataFormats.IsText(kv.Key)))
            names.UnionWith(new[]{DataFormats.Text,DataFormats.UnicodeText,DataFormats.StringFormat});
        return names.ToArray();
    }
    public virtual string[] GetFormats() => GetFormats(true);
    public virtual bool GetDataPresent(string format,bool autoConvert) => GetFormats(autoConvert).Contains(format);
    public virtual bool GetDataPresent(string format) => GetDataPresent(format,true);
    public virtual bool GetDataPresent(Type format) => GetDataPresent(format.FullName!);
    public bool ContainsText() => ContainsText(TextDataFormat.UnicodeText);
    public bool ContainsText(TextDataFormat format) => GetDataPresent(DataFormats.For(format));
    public string GetText() => GetText(TextDataFormat.UnicodeText);
    public string GetText(TextDataFormat format) => GetData(DataFormats.For(format)) as string ?? "";
    public void SetText(string text) => SetText(text,TextDataFormat.UnicodeText);
    public void SetText(string text,TextDataFormat format)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        SetData(DataFormats.For(format),true,text);
    }
    public bool ContainsFileDropList() => GetDataPresent(DataFormats.FileDrop);
    public StringCollection GetFileDropList()
    {
        var result = new StringCollection();
        if (GetData(DataFormats.FileDrop) is string[] paths) result.AddRange(paths);
        return result;
    }
    public void SetFileDropList(StringCollection paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        SetData(DataFormats.FileDrop,paths.Cast<string>().ToArray());
    }
    public virtual bool ContainsImage() => GetDataPresent(DataFormats.Bitmap);
    public virtual Image? GetImage() => GetData(DataFormats.Bitmap) as Image;
    public virtual void SetImage(Image image) => SetData(DataFormats.Bitmap,image ?? throw new ArgumentNullException(nameof(image)));
    public virtual bool ContainsAudio() => GetDataPresent(DataFormats.WaveAudio);
    public virtual Stream? GetAudioStream() => GetData(DataFormats.WaveAudio) as Stream;
    public virtual void SetAudio(byte[] audioBytes) => SetAudio(new MemoryStream(audioBytes ?? throw new ArgumentNullException(nameof(audioBytes)),false));
    public virtual void SetAudio(Stream audioStream) => SetData(DataFormats.WaveAudio,false,audioStream ?? throw new ArgumentNullException(nameof(audioStream)));
}
internal static class Pasteboard
{
    [DllImport("MacForms")] private static extern IntPtr mf_board_read(string? name);
    [DllImport("MacForms")] private static extern int mf_board_write(string? name,string data);
    [DllImport("MacForms")] private static extern void mf_free(IntPtr bytes);

    private static byte[] Pixels(Image image)
    {
        using var output = new MemoryStream();
        image.Save(output,System.Drawing.Imaging.ImageFormat.Png);
        return output.ToArray();
    }
    private static byte[] Bytes(Stream stream)
    {
        long saved = stream.CanSeek ? stream.Position : 0;
        try
        {
            if (stream.CanSeek) stream.Position = 0;
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }
        finally { if (stream.CanSeek) stream.Position = saved; }
    }
    internal static string Encode(IDataObject data,bool forDrag = false)
    {
        var values = new Dictionary<string,object?>();
        foreach (var format in data.GetFormats(false))
        {
            var value = data.GetData(format,false);
            if (format == DataFormats.FileDrop && (value is not string[] paths || paths.Any(string.IsNullOrEmpty)))
                throw new ArgumentException("File drop data requires a list of file paths.");
            if (value is string || (format == DataFormats.FileDrop && value is string[]))
                values[format] = value;
            else if (value is Image image && format == DataFormats.Bitmap) values[format] = new { bytes = Convert.ToBase64String(Pixels(image)) };
            else if (value is Stream audio && format == DataFormats.WaveAudio) values[format] = new { bytes = Convert.ToBase64String(Bytes(audio)) };
            else if (value is byte[] bytes) values[format] = new { bytes = Convert.ToBase64String(bytes) };
            else if (!forDrag) throw new NotSupportedException($"Clipboard format '{format}' needs a string or byte array.");
        }
        if (data.GetData(DataFormats.UnicodeText) is string text) values[DataFormats.UnicodeText] = text;
        return JsonSerializer.Serialize(values);
    }
    internal static DataObject Decode(string json)
    {
        var result = new DataObject();
        using var doc = JsonDocument.Parse(json);
        foreach (var pair in doc.RootElement.EnumerateObject())
        {
            if (pair.Value.ValueKind == JsonValueKind.String) result.SetData(pair.Name,pair.Value.GetString());
            else if (pair.Value.ValueKind == JsonValueKind.Array && pair.Name == DataFormats.FileDrop)
                result.SetData(pair.Name,pair.Value.EnumerateArray().Where(x=>x.ValueKind == JsonValueKind.String).Select(x=>x.GetString()!).ToArray());
            else if (pair.Value.ValueKind == JsonValueKind.Object && pair.Value.TryGetProperty("bytes",out var raw)
                && raw.ValueKind == JsonValueKind.String && raw.TryGetBytesFromBase64(out var bytes))
            {
                if (pair.Name == DataFormats.WaveAudio) result.SetAudio(bytes);
                else if (pair.Name == DataFormats.Bitmap)
                {
                    // A foreign pasteboard item can claim image data while containing an invalid payload.
                    try { using var stream = new MemoryStream(bytes,false); result.SetImage(Image.FromStream(stream)); }
                    catch (ArgumentException) { }
                }
                else result.SetData(pair.Name,bytes);
            }
        }
        return result;
    }
    internal static DataObject Read(string? board = null)
    {
        IntPtr raw = mf_board_read(board);
        try { return Decode(Marshal.PtrToStringUTF8(raw) ?? "{}"); }
        finally { mf_free(raw); }
    }
    internal static void Write(IDataObject data,string? board = null)
    {
        if (mf_board_write(board,Encode(data)) == 0) throw new ExternalException("The macOS pasteboard rejected the data.");
    }
}
public static class Clipboard
{
    public static void Clear() => Pasteboard.Write(new DataObject());
    public static void SetDataObject(object data) => SetDataObject(data,false);
    public static void SetDataObject(object data,bool copy) => Pasteboard.Write(data as IDataObject ?? new DataObject(data));
    public static IDataObject GetDataObject() => Pasteboard.Read();
    public static bool ContainsData(string format) => GetDataObject().GetDataPresent(format);
    public static object? GetData(string format) => GetDataObject().GetData(format);
    public static void SetData(string format,object? data) => SetDataObject(new DataObject(format,data));
    public static bool ContainsText() => ContainsText(TextDataFormat.UnicodeText);
    public static bool ContainsText(TextDataFormat format) => ContainsData(DataFormats.For(format));
    public static string GetText() => GetText(TextDataFormat.UnicodeText);
    public static string GetText(TextDataFormat format) => Pasteboard.Read().GetText(format);
    public static void SetText(string text) => SetText(text,TextDataFormat.UnicodeText);
    public static void SetText(string text,TextDataFormat format)
    {
        var value = new DataObject(); value.SetText(text,format); Pasteboard.Write(value);
    }
    public static bool ContainsImage() => ContainsData(DataFormats.Bitmap);
    public static Image? GetImage() => Pasteboard.Read().GetImage();
    public static void SetImage(Image image) { var data = new DataObject(); data.SetImage(image); Pasteboard.Write(data); }
    public static bool ContainsAudio() => ContainsData(DataFormats.WaveAudio);
    public static Stream? GetAudioStream() => Pasteboard.Read().GetAudioStream();
    public static void SetAudio(Stream stream) { var data = new DataObject(); data.SetAudio(stream); Pasteboard.Write(data); }
    public static void SetAudio(byte[] bytes) { var data = new DataObject(); data.SetAudio(bytes); Pasteboard.Write(data); }
    public static bool ContainsFileDropList() => ContainsData(DataFormats.FileDrop);
    public static StringCollection GetFileDropList() => Pasteboard.Read().GetFileDropList();
    public static void SetFileDropList(StringCollection paths)
    {
        var data = new DataObject(); data.SetFileDropList(paths); Pasteboard.Write(data);
    }
}
