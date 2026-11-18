using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public sealed class Cursor : IDisposable
{
    [DllImport("MacForms")] private static extern IntPtr mf_cursor_standard(int kind);
    [DllImport("MacForms")] private static extern IntPtr mf_cursor_load(byte[] data,int size);
    [DllImport("MacForms")] private static extern void mf_cursor_release(IntPtr cursor);
    [DllImport("MacForms")] private static extern void mf_cursor_info(IntPtr cursor,out int w,out int h,out int x,out int y);
    [DllImport("MacForms")] private static extern void mf_cursor_set_current(IntPtr cursor);
    [DllImport("MacForms")] private static extern void mf_cursor_hide(int hide);
    [DllImport("MacForms")] private static extern void mf_cursor_location(int write,ref int x,ref int y);
    private bool owned;
    private static Cursor? current;
    public IntPtr Handle { get; private set; }
    internal Cursor(int kind) => Handle = mf_cursor_standard(kind);
    public Cursor(IntPtr handle) { if (handle == 0) throw new ArgumentException("Invalid NSCursor handle.",nameof(handle)); Handle = handle; }
    public Cursor(string fileName) : this(File.ReadAllBytes(fileName)) {}
    public Cursor(Stream stream) : this(Read(stream)) {}
    public Cursor(Type type,string resource) : this(Resource(type,resource)) {}
    private static byte[] Resource(Type type,string name)
    { using var stream = type.Assembly.GetManifestResourceStream(type,name) ?? throw new ArgumentException("Cursor resource missing.",nameof(name)); return Read(stream); }
    private static byte[] Read(Stream stream) { using var data = new MemoryStream(); stream.CopyTo(data); return data.ToArray(); }
    private Cursor(byte[] data)
    { Handle=mf_cursor_load(data,data.Length); if (Handle == 0) throw new ArgumentException("Could not decode the cursor image."); owned = true; }
    public static Cursor? Current { get => current ?? Cursors.Default; set { current=value; mf_cursor_set_current((value ?? Cursors.Default).Handle); } }
    public static Point Position
    {
        get { int x=0,y=0; mf_cursor_location(0,ref x,ref y); return new(x,y); }
        set { int x=value.X,y=value.Y; mf_cursor_location(1,ref x,ref y); }
    }
    public Size Size { get { mf_cursor_info(Handle,out int w,out int h,out _,out _); return new(w,h); } }
    public Point HotSpot { get { mf_cursor_info(Handle,out _,out _,out int x,out int y); return new(x,y); } }
    public static void Hide() => mf_cursor_hide(1);
    public static void Show() => mf_cursor_hide(0);
    ~Cursor() => Release();
    private void Release() { if (owned) { mf_cursor_release(Handle); Handle=0; owned=false; } }
    public void Dispose() { Release(); GC.SuppressFinalize(this); }
}
public static class Cursors
{
    public static Cursor Arrow { get; } = new(0);
    public static Cursor Default => Arrow;
    public static Cursor IBeam { get; } = new(1);
    public static Cursor Cross { get; } = new(2);
    public static Cursor Hand { get; } = new(3);
    public static Cursor No { get; } = new(4);
    public static Cursor SizeWE { get; } = new(5);
    public static Cursor SizeNS { get; } = new(6);
    public static Cursor SizeAll { get; } = new(7);
    public static Cursor VSplit => SizeWE;
    public static Cursor HSplit => SizeNS;
}
public partial class Control
{
    [DllImport("MacForms")] private static extern void mf_view_cursor(IntPtr view,IntPtr cursor);
    [DllImport("MacForms",EntryPoint="mf_widget_view")] private static extern IntPtr CursorView(IntPtr handle);
    private Cursor? cursor;
    public virtual Cursor Cursor
    {
        get => cursor ?? Parent?.Cursor ?? Cursors.Default;
        set { if (!ReferenceEquals(cursor,value)) { cursor=value; ApplyCursor(); CursorChanged?.Invoke(this,EventArgs.Empty); } }
    }
    public event EventHandler? CursorChanged;
    internal void ApplyCursor() { if (NativeHandle != 0) mf_view_cursor(CursorView(NativeHandle),cursor?.Handle ?? 0); }
}
