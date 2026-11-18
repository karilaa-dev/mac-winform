using System.Runtime.InteropServices;

namespace System.Windows.Forms;

internal static class Native
{
    private const string Lib = "MacForms";
    internal delegate void EventCallback(IntPtr handle, int kind);
    internal delegate void PostCallback();
    [DllImport(Lib)] internal static extern void mf_init();
    [DllImport(Lib)] internal static extern void mf_pump();
    [DllImport(Lib)] internal static extern void mf_post(PostCallback callback);
    [DllImport(Lib)] internal static extern IntPtr mf_create(int kind, EventCallback callback, string text);
    [DllImport(Lib)] internal static extern void mf_destroy(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_parent(IntPtr h, IntPtr parent);
    [DllImport(Lib)] internal static extern void mf_frame(IntPtr h, int x, int y, int w, int height);
    [DllImport(Lib)] internal static extern void mf_text(IntPtr h, string text);
    [DllImport(Lib)] internal static extern IntPtr mf_get_text(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_visible(IntPtr h, int visible);
    [DllImport(Lib)] internal static extern void mf_enabled(IntPtr h, int enabled);
    [DllImport(Lib)] internal static extern void mf_checked(IntPtr h, int value);
    [DllImport(Lib)] internal static extern int mf_get_checked(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_add_item(IntPtr h, string text);
    [DllImport(Lib)] internal static extern void mf_clear_items(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_select(IntPtr h, int index);
    [DllImport(Lib)] internal static extern int mf_get_selected(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_default_button(IntPtr h,IntPtr button);
    [DllImport(Lib)] internal static extern void mf_readonly(IntPtr h,int readOnly);
    [DllImport(Lib)] internal static extern void mf_center(IntPtr h);
    [DllImport(Lib)] internal static extern int mf_focus(IntPtr h);
    [DllImport(Lib)] internal static extern int mf_width(IntPtr h);
    [DllImport(Lib)] internal static extern int mf_height(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_style(IntPtr h,string fontName,float points,int bold,int argb,int align);
    [DllImport(Lib)] internal static extern void mf_date_value(IntPtr h,long seconds);
    [DllImport(Lib)] internal static extern long mf_get_date_value(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_number(IntPtr h,double value,double min,double max);
    [DllImport(Lib)] internal static extern double mf_get_number(IntPtr h);
    [DllImport(Lib)] internal static extern int mf_alert(IntPtr owner,string text,string title,int buttons,int icon,int defaultButton,int options);
    [DllImport(Lib)] internal static extern void mf_grid_columns(IntPtr h,string columns);
    [DllImport(Lib)] internal static extern void mf_grid_header_height(IntPtr h, int height);
    [DllImport(Lib)] internal static extern void mf_grid_rows(IntPtr h,string rows);
}
