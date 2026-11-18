using System.Runtime.InteropServices;
namespace System.Windows.Forms;

public partial class Control
{
    private delegate int DragCallback(IntPtr handle,int phase,IntPtr payload,int x,int y,int state,int allowed);
    private static readonly DragCallback routeDrag = RouteDrag;
    [DllImport("MacForms")] private static extern void mf_set_drag_callback(DragCallback callback);
    [DllImport("MacForms")] private static extern void mf_allow_drop(IntPtr handle,int value);
    [DllImport("MacForms")] private static extern int mf_drag_data(IntPtr handle,string data,int effects);
    private static IDataObject? dragging;
    private IDataObject? incoming;
    private DragDropEffects lastEffect;
    private bool allowDrop;
    public virtual bool AllowDrop
    {
        get => allowDrop;
        set { allowDrop = value; if (NativeHandle != 0) mf_allow_drop(NativeHandle,value ? 1 : 0); }
    }
    public event DragEventHandler? DragEnter, DragOver, DragDrop;
    public event EventHandler? DragLeave;
    protected virtual void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(this,e);
    protected virtual void OnDragOver(DragEventArgs e) => DragOver?.Invoke(this,e);
    protected virtual void OnDragLeave(EventArgs e) => DragLeave?.Invoke(this,e);
    protected virtual void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(this,e);
    public DragDropEffects DoDragDrop(object data,DragDropEffects allowedEffects)
    {
        var previous = dragging;
        dragging = data as IDataObject ?? new DataObject(data);
        try { return (DragDropEffects)mf_drag_data(Handle,Pasteboard.Encode(dragging,true),(int)allowedEffects); }
        finally { dragging = previous; }
    }
    private static int RouteDrag(IntPtr handle,int phase,IntPtr payload,int x,int y,int state,int allowed)
    {
        if (!peers.TryGetValue(handle,out var target) || !target.AllowDrop) return 0;
        if (phase == 3)
        {
            target.OnDragLeave(EventArgs.Empty);
            target.incoming = null;
            target.lastEffect = DragDropEffects.None;
            return 0;
        }
        if (payload != 0) target.incoming = dragging ?? Pasteboard.Decode(Marshal.PtrToStringUTF8(payload) ?? "{}");
        var e = new DragEventArgs(target.incoming,state,x,y,(DragDropEffects)allowed,
            phase == 1 ? DragDropEffects.None : target.lastEffect);
        switch (phase)
        {
            case 1: target.OnDragEnter(e); break;
            case 2: target.OnDragOver(e); break;
            case 4: target.OnDragDrop(e); target.incoming = null; break;
        }
        target.lastEffect = e.Effect & e.AllowedEffect;
        return (int)target.lastEffect;
    }
}
