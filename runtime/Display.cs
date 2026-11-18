using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

public class Screen
{
    [DllImport("MacForms")] private static extern IntPtr mf_screens();
    [DllImport("MacForms")] private static extern void mf_free(IntPtr data);
    private Screen(JsonElement e,bool primary)
    {
        Bounds = Rect("bounds"); WorkingArea = Rect("work"); Primary = primary;
        BitsPerPixel = e.GetProperty("depth").GetInt32(); DeviceName = e.GetProperty("name").GetString()!;
        Rectangle Rect(string name) { var a = e.GetProperty(name); return new(a[0].GetInt32(),a[1].GetInt32(),a[2].GetInt32(),a[3].GetInt32()); }
    }
    public Rectangle Bounds { get; }
    public Rectangle WorkingArea { get; }
    public string DeviceName { get; }
    public bool Primary { get; }
    public int BitsPerPixel { get; }
    public static Screen[] AllScreens
    {
        get
        {
            IntPtr raw = mf_screens();
            try
            {
                using var doc = JsonDocument.Parse(Marshal.PtrToStringUTF8(raw)!);
                return doc.RootElement.EnumerateArray().Select((e,i)=>new Screen(e,i==0)).ToArray();
            }
            finally { mf_free(raw); }
        }
    }
    public static Screen? PrimaryScreen => AllScreens.FirstOrDefault();
    public static Screen FromPoint(Point point) => FromRectangle(new Rectangle(point,new Size(1,1)));
    public static Screen FromRectangle(Rectangle rect)
    {
        double Distance(Rectangle r) => Math.Pow(Math.Max(0,Math.Max(r.Left-rect.Right,rect.Left-r.Right)),2)
            + Math.Pow(Math.Max(0,Math.Max(r.Top-rect.Bottom,rect.Top-r.Bottom)),2);
        return AllScreens.OrderByDescending(s=>{var hit=Rectangle.Intersect(rect,s.Bounds); return (long)hit.Width*hit.Height;})
            .ThenBy(s=>Distance(s.Bounds)).First();
    }
    public static Screen FromControl(Control control) => FromRectangle(control.RectangleToScreen(control.ClientRectangle));
    public static Screen FromHandle(IntPtr hwnd) => Control.FromHandle(hwnd) is {} c ? FromControl(c) : AllScreens.First();
    public static Rectangle GetBounds(Point pt) => FromPoint(pt).Bounds;
    public static Rectangle GetBounds(Rectangle rect) => FromRectangle(rect).Bounds;
    public static Rectangle GetBounds(Control c) => FromControl(c).Bounds;
    public static Rectangle GetWorkingArea(Point pt) => FromPoint(pt).WorkingArea;
    public static Rectangle GetWorkingArea(Rectangle rect) => FromRectangle(rect).WorkingArea;
    public static Rectangle GetWorkingArea(Control c) => FromControl(c).WorkingArea;
}
public partial class Control
{
    public static Point MousePosition => Cursor.Position;
}
