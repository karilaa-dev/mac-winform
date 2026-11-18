using System.Runtime.InteropServices;
namespace System.Windows.Forms;

public class WebBrowser : Control
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool NavigationPolicy(IntPtr handle,IntPtr url);
    private static readonly NavigationPolicy policy = Decide;
    [DllImport("MacForms")] private static extern void mf_browser_hook(IntPtr view,NavigationPolicy callback);
    [DllImport("MacForms")] private static extern void mf_browser_load(IntPtr view,string content,int html);
    [DllImport("MacForms")] private static extern int mf_browser_action(IntPtr view,int action);
    [DllImport("MacForms")] private static extern IntPtr mf_browser_string(IntPtr view,int field);
    [DllImport("MacForms")] private static extern void mf_free(IntPtr bytes);
    public bool AllowNavigation { get; set; } = true;
    public Stream? DocumentStream
    {
        get => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(DocumentText),false);
        set
        {
            if (value == null) { DocumentText = ""; return; }
            using var reader = new StreamReader(value,detectEncodingFromByteOrderMarks:true,leaveOpen:true);
            DocumentText = reader.ReadToEnd();
        }
    }
    private string? html;
    private Uri? pending;
    internal override int Kind => 31;
    public event WebBrowserNavigatingEventHandler? Navigating;
    public event WebBrowserNavigatedEventHandler? Navigated;
    public event WebBrowserDocumentCompletedEventHandler? DocumentCompleted;
    public Uri? Url
    {
        get => NativeHandle == 0 ? pending : Uri.TryCreate(Read(0),UriKind.Absolute,out var current) ? current : pending;
        set => Navigate(value ?? new Uri("about:blank"));
    }
    public string DocumentText
    {
        get => NativeHandle != 0 && ReadyState == WebBrowserReadyState.Complete ? Read(2) : html ?? "";
        set { html = value ?? ""; pending = null; if (NativeHandle != 0) mf_browser_load(ViewHandle,html,1); }
    }
    public string DocumentTitle => NativeHandle == 0 ? "" : Read(1);
    public bool IsBusy => NativeHandle != 0 && mf_browser_action(ViewHandle,6) != 0;
    public WebBrowserReadyState ReadyState { get; private set; }
    public bool CanGoBack => NativeHandle != 0 && mf_browser_action(ViewHandle,0) != 0;
    public bool CanGoForward => NativeHandle != 0 && mf_browser_action(ViewHandle,1) != 0;
    public bool GoBack() => CanGoBack && mf_browser_action(ViewHandle,2) != 0;
    public bool GoForward() => CanGoForward && mf_browser_action(ViewHandle,3) != 0;
    public void Stop() { if (NativeHandle != 0) mf_browser_action(ViewHandle,4); }
    public override void Refresh() { if (NativeHandle != 0) mf_browser_action(ViewHandle,5); }
    public void Navigate(string urlString) => Navigate(new Uri(urlString,UriKind.Absolute));
    public void Navigate(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        pending = url; html = null;
        if (NativeHandle != 0) mf_browser_load(ViewHandle,url.AbsoluteUri,0);
    }
    private string Read(int field)
    {
        var raw = mf_browser_string(ViewHandle,field);
        try { return Marshal.PtrToStringUTF8(raw) ?? ""; } finally { mf_free(raw); }
    }
    internal override void ApplyNative()
    {
        mf_browser_hook(ViewHandle,policy);
        if (html != null) mf_browser_load(ViewHandle,html,1);
        else if (pending != null) mf_browser_load(ViewHandle,pending.AbsoluteUri,0);
    }
    private static bool Decide(IntPtr handle,IntPtr url)
    {
        if (FromHandle(handle) is not WebBrowser browser) return false;
        Uri.TryCreate(Marshal.PtrToStringUTF8(url),UriKind.Absolute,out var address);
        if (!browser.AllowNavigation && browser.ReadyState == WebBrowserReadyState.Complete) return false;
        var e = new WebBrowserNavigatingEventArgs(address,"");
        browser.Navigating?.Invoke(browser,e);
        if (!e.Cancel) browser.ReadyState = WebBrowserReadyState.Loading;
        return !e.Cancel;
    }
    internal override void OnNative(int ev)
    {
        if (ev == 41) { ReadyState = WebBrowserReadyState.Interactive; Navigated?.Invoke(this,new(Url)); }
        else if (ev == 42) { ReadyState = WebBrowserReadyState.Complete; DocumentCompleted?.Invoke(this,new(Url)); }
        else base.OnNative(ev);
    }
}
