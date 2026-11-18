using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

[ProvideProperty("Error",typeof(Control))]
[ProvideProperty("IconPadding",typeof(Control))]
[ProvideProperty("IconAlignment",typeof(Control))]
public class ErrorProvider : Component,IExtenderProvider,ISupportInitialize
{
    [DllImport("MacForms")] private static extern IntPtr mf_error_attach(IntPtr view);
    [DllImport("MacForms")] private static extern void mf_error_config(IntPtr peer,string error,int alignment,int padding,int blink,int rate,ImageHandle image);
    [DllImport("MacForms")] private static extern void mf_error_release(IntPtr peer);
    private sealed class Entry
    {
        internal string Text="";
        internal int Padding;
        internal ErrorIconAlignment Alignment=ErrorIconAlignment.MiddleRight;
        internal ProviderPeer? Peer;
    }
    private readonly Dictionary<Control,Entry> entries=new();
    private int blinkRate=250;
    private ErrorBlinkStyle blink=ErrorBlinkStyle.BlinkIfDifferentError;
    private bool right,initializing;
    private ContainerControl? container;
    private object? source;
    private string member="";
    private BindingManagerBase? manager;
    private readonly HashSet<Control> bound=new();
    private readonly Icon defaultIcon=MakeIcon();
    private Icon? customIcon;
    public ErrorProvider() {}
    public ErrorProvider(IContainer container) => container.Add(this);
    public ErrorProvider(ContainerControl parentControl) => ContainerControl=parentControl;
    public object? Tag { get; set; }
    public bool CanExtend(object target) => target is Control and not Form;
    public bool HasErrors => entries.Values.Any(item=>item.Text.Length>0);
    public bool RightToLeft
    {
        get => right;
        set { if (value!=right) { right=value; Refresh(); OnRightToLeftChanged(EventArgs.Empty); } }
    }
    public event EventHandler? RightToLeftChanged;
    protected virtual void OnRightToLeftChanged(EventArgs e) => RightToLeftChanged?.Invoke(this,e);
    public int BlinkRate { get => blinkRate; set { ArgumentOutOfRangeException.ThrowIfNegative(value); blinkRate=value; Refresh(); } }
    public ErrorBlinkStyle BlinkStyle { get => blink; set { blink=value; Refresh(); } }
    private static Icon MakeIcon()
    {
        var pixels=new Bitmap(16,16);
        for (int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            float radius=(x-7.5f)*(x-7.5f)+(y-7.5f)*(y-7.5f);
            if(radius>61) continue;
            bool cross=x>=4 && x<=11 && y>=4 && y<=11 && (Math.Abs(x-y)<=1 || Math.Abs(x+y-15)<=1);
            pixels.SetPixel(x,y,cross?Color.White:radius>47?Color.DarkRed:Color.Firebrick);
        }
        return new Icon(pixels);
    }
    public Icon Icon { get => customIcon??defaultIcon; set { customIcon=value; Refresh(); } }
    private void Apply(Entry entry,IntPtr ptr)
    {
        if(ptr==0 || initializing) return;
        mf_error_config(ptr,entry.Text,(int)entry.Alignment^(right?1:0),entry.Padding,(int)blink,blinkRate,Icon.Bitmap.Handle);
    }
    private void Refresh() { foreach(var item in entries.Values) Apply(item,item.Peer?.Pointer??0); }
    private Entry Get(Control c)
    {
        if(entries.TryGetValue(c,out var entry)) return entry;
        entry=new(); entries.Add(c,entry);
        entry.Peer=new ProviderPeer(c,() => { var p=mf_error_attach(c.ViewHandle); Apply(entry,p); return p; },mf_error_release,Forget);
        return entry;
    }
    private void Forget(Control control)
    {
        bound.Remove(control);
        if(entries.Remove(control,out var entry)) entry.Peer?.Dispose();
    }
    public void Clear() { foreach(var c in entries.Keys.ToArray()) Forget(c); }
    public string GetError(Control control) => entries.TryGetValue(control,out var item) ? item.Text:"";
    public ErrorIconAlignment GetIconAlignment(Control control) => entries.TryGetValue(control,out var item) ? item.Alignment:ErrorIconAlignment.MiddleRight;
    public int GetIconPadding(Control control) => entries.TryGetValue(control,out var item) ? item.Padding:0;
    public void SetError(Control control,string? value)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (string.IsNullOrEmpty(value) && !entries.ContainsKey(control)) return;
        var item=Get(control); item.Text=value??""; Apply(item,item.Peer!.Pointer);
    }
    public void SetIconAlignment(Control control,ErrorIconAlignment value)
    {
        if ((uint)value>5) throw new InvalidEnumArgumentException(nameof(value),(int)value,typeof(ErrorIconAlignment));
        var item=Get(control); item.Alignment=value; Apply(item,item.Peer!.Pointer);
    }
    public void SetIconPadding(Control control,int padding)
    {
        var item=Get(control); item.Padding=padding; Apply(item,item.Peer!.Pointer);
    }
    public ContainerControl? ContainerControl { get => container; set { container=value; Rebind(); } }
    public object? DataSource { get => source; set => BindToDataAndErrors(value,member); }
    public string DataMember { get => member; set => BindToDataAndErrors(source,value); }
    public void BindToDataAndErrors(object? newDataSource,string? newDataMember)
    {
        source=newDataSource; member=newDataMember??""; Rebind();
    }
    private void Rebind()
    {
        if(initializing) return;
        if(manager!=null) { manager.CurrentChanged-=OnBinding; manager.CurrentItemChanged-=OnBinding; }
        manager=source!=null ? container?.BindingContext?[source,member]:null;
        if(manager!=null) { manager.CurrentChanged+=OnBinding; manager.CurrentItemChanged+=OnBinding; }
        UpdateBinding();
    }
    private void OnBinding(object? sender,EventArgs e) => UpdateBinding();
    public void UpdateBinding()
    {
        var previous=bound.ToHashSet();
        bound.Clear();
        if(container!=null && manager?.Current is IDataErrorInfo validation)
        {
            void Read(Control c)
            {
                var messages=new List<string>();
                foreach(Binding binding in c.DataBindings)
                {
                    if(!ReferenceEquals(binding.DataSource,source)) continue;
                    string message=validation[binding.BindingMemberInfo.BindingField];
                    if(!string.IsNullOrEmpty(message)) messages.Add(message);
                }
                if(messages.Count>0) { bound.Add(c); previous.Remove(c); SetError(c,string.Join(Environment.NewLine,messages)); }
                foreach(var child in c.Controls) Read(child);
            }
            Read(container);
        }
        foreach(var c in previous) SetError(c,"");
    }
    void ISupportInitialize.BeginInit() => initializing=true;
    void ISupportInitialize.EndInit() { initializing=false; Rebind(); Refresh(); }
    protected override void Dispose(bool disposing)
    {
        if(disposing)
        {
            if(manager!=null) { manager.CurrentChanged-=OnBinding; manager.CurrentItemChanged-=OnBinding; }
            Clear(); defaultIcon.Dispose();
        }
        base.Dispose(disposing);
    }
}
