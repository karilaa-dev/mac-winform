using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Windows.Forms;

internal static class TextNative
{
    [DllImport("MacForms")] internal static extern void mf_edit_selection(IntPtr h,out int start,out int length);
    [DllImport("MacForms")] internal static extern void mf_edit_select(IntPtr h,int start,int length);
    [DllImport("MacForms")] internal static extern void mf_edit_replace(IntPtr h,string text);
    [DllImport("MacForms")] internal static extern int mf_edit_history(IntPtr h,int command);
    [DllImport("MacForms")] internal static extern void mf_edit_scroll(IntPtr h);
    [DllImport("MacForms")] internal static extern void mf_edit_wrap(IntPtr h,int enabled);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate int MaskInput(IntPtr h,int start,int length,string replacement);
    [DllImport("MacForms")] internal static extern void mf_mask_install(IntPtr h,MaskInput input);
    [DllImport("MacForms")] internal static extern void mf_rich_color(IntPtr h,int color,int background);
    [DllImport("MacForms")] internal static extern int mf_rich_get_color(IntPtr h,int background);
    [DllImport("MacForms")] internal static extern void mf_rich_font(IntPtr h,string name,float size,int style);
    [DllImport("MacForms")] internal static extern IntPtr mf_rich_get_font(IntPtr h,out float size,out int style);
    [DllImport("MacForms")] internal static extern IntPtr mf_rich_get_rtf(IntPtr h,int selected);
    [DllImport("MacForms")] internal static extern int mf_rich_set_rtf(IntPtr h,string rtf,int selected);
    [DllImport("MacForms")] private static extern void mf_free(IntPtr data);
    internal static string? TakeString(IntPtr value)
    {
        if (value == 0) return null;
        try { return Marshal.PtrToStringUTF8(value); } finally { mf_free(value); }
    }
}

public abstract class TextBoxBase : Control
{
    private bool readOnly, multiline, wordWrap=true;
    private int start, length;
    public bool ReadOnly
    {
        get => readOnly;
        set
        {
            if (readOnly==value) return;
            readOnly=value;
            if (NativeHandle!=0) Native.mf_readonly(NativeHandle,value ? 1 : 0);
            ReadOnlyChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public virtual bool Multiline
    {
        get=>multiline;
        set
        {
            if (multiline==value) return;
            multiline=value; RecreateWidget(); MultilineChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public bool WordWrap
    {
        get=>wordWrap;
        set { wordWrap=value; if(NativeHandle!=0 && Multiline) TextNative.mf_edit_wrap(NativeHandle,value ? 1 : 0); }
    }
    public event EventHandler? ReadOnlyChanged, MultilineChanged;
    public int TextLength => Text.Length;
    public string[] Lines
    {
        get => Text.Length==0 ? Array.Empty<string>() : Text.Replace("\r\n","\n").Replace('\r','\n').Split('\n');
        set => Text=string.Join(Environment.NewLine,value ?? Array.Empty<string>());
    }
    public void Clear() => Text="";
    public void AppendText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Select(TextLength,0); SelectedText=text;
    }
    private (int start,int length) Selection()
    {
        if (NativeHandle!=0) TextNative.mf_edit_selection(NativeHandle,out start,out length);
        return (start,length);
    }
    internal virtual string SelectionBuffer => Text;
    public int SelectionStart { get=>Selection().start; set=>Select(value,SelectionLength); }
    public int SelectionLength { get=>Selection().length; set=>Select(SelectionStart,value); }
    public virtual string SelectedText
    {
        get { var (pos,count)=Selection(); string text=SelectionBuffer; pos=Math.Min(pos,text.Length); return text.Substring(pos,Math.Min(count,text.Length-pos)); }
        set
        {
            value ??= "";
            if (NativeHandle==0)
            {
                string text=Text; var (pos,count)=Selection(); pos=Math.Min(pos,text.Length);
                Text=text.Remove(pos,Math.Min(text.Length-pos,count)).Insert(pos,value); Select(pos+value.Length,0);
            }
            else
            {
                TextNative.mf_edit_replace(NativeHandle,value); RefreshText();
            }
        }
    }
    internal void RefreshText() => UpdateTextFromPeer(Marshal.PtrToStringUTF8(Native.mf_get_text(NativeHandle)) ?? "");
    public void Select(int start,int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start); ArgumentOutOfRangeException.ThrowIfNegative(length);
        this.start=Math.Min(start,SelectionBuffer.Length); this.length=Math.Min(SelectionBuffer.Length-this.start,length);
        if(NativeHandle!=0) TextNative.mf_edit_select(NativeHandle,this.start,this.length);
    }
    public void SelectAll() => Select(0,SelectionBuffer.Length);
    public void DeselectAll() => Select(SelectionStart,0);
    public virtual void Copy()
    {
        if (this is TextBox box && (box.UseSystemPasswordChar || box.PasswordChar!=0)) return;
        if (this is MaskedTextBox masked && (masked.UseSystemPasswordChar || masked.PasswordChar!=0)) return;
        if (SelectedText.Length>0) Clipboard.SetText(SelectedText);
    }
    public void Cut() { if (!ReadOnly && SelectionLength!=0) { Copy(); SelectedText=""; } }
    public virtual void Paste() { if (!ReadOnly && Clipboard.ContainsText()) SelectedText=Clipboard.GetText(); }
    public bool CanUndo => NativeHandle!=0 && TextNative.mf_edit_history(NativeHandle,0)!=0;
    public void Undo() { if (CanUndo) TextNative.mf_edit_history(NativeHandle,1); }
    public void ClearUndo() { if (NativeHandle!=0) TextNative.mf_edit_history(NativeHandle,4); }
    public void ScrollToCaret() { if(NativeHandle!=0) TextNative.mf_edit_scroll(NativeHandle); }
    internal override void ApplyNative()
    {
        Native.mf_readonly(NativeHandle,readOnly ? 1 : 0);
        if(Multiline) TextNative.mf_edit_wrap(NativeHandle,wordWrap ? 1 : 0);
        TextNative.mf_edit_select(NativeHandle,start,length);
    }
}
public enum RichTextBoxStreamType { RichText, PlainText, RichNoOleObjs, TextTextOleObjs, UnicodePlainText }
public class RichTextBox : TextBoxBase
{
    internal override int Kind=>26;
    public RichTextBox() { Multiline=true; Size=new(100,96); }
    public event EventHandler? SelectionChanged;
    public bool CanRedo => NativeHandle!=0 && TextNative.mf_edit_history(NativeHandle,2)!=0;
    public void Redo() { if (CanRedo) TextNative.mf_edit_history(NativeHandle,3); }
    public override void Copy()
    {
        if (SelectionLength==0) return;
        var data=new DataObject();
        data.SetText(SelectedText);
        data.SetText(SelectedRtf,TextDataFormat.Rtf);
        Clipboard.SetDataObject(data);
    }
    public override void Paste()
    {
        if (ReadOnly) return;
        if (Clipboard.ContainsText(TextDataFormat.Rtf)) SelectedRtf=Clipboard.GetText(TextDataFormat.Rtf);
        else base.Paste();
    }
    public string Rtf { get=>TextNative.TakeString(TextNative.mf_rich_get_rtf(Handle,0))!; set=>SetRtf(value,0); }
    public string SelectedRtf { get=>TextNative.TakeString(TextNative.mf_rich_get_rtf(Handle,1))!; set=>SetRtf(value,1); }
    private void SetRtf(string? value,int selected)
    {
        if (TextNative.mf_rich_set_rtf(Handle,value ?? "",selected)==0) throw new ArgumentException("Invalid RTF document.",nameof(value));
        RefreshText();
    }
    public Color SelectionColor { get=>Color.FromArgb(TextNative.mf_rich_get_color(Handle,0)); set=>TextNative.mf_rich_color(Handle,value.ToArgb(),0); }
    public Color SelectionBackColor { get=>Color.FromArgb(TextNative.mf_rich_get_color(Handle,1)); set=>TextNative.mf_rich_color(Handle,value.ToArgb(),1); }
    public Font? SelectionFont
    {
        get
        {
            var name=TextNative.TakeString(TextNative.mf_rich_get_font(Handle,out float size,out int style));
            return name==null ? null : new(name,size,(FontStyle)style);
        }
        set { if (value!=null) TextNative.mf_rich_font(Handle,value.Name,value.SizeInPoints,(int)value.Style); }
    }
    public void LoadFile(string path) => LoadFile(path,RichTextBoxStreamType.RichText);
    public void LoadFile(string path,RichTextBoxStreamType type) { using var file=File.OpenRead(path); LoadFile(file,type); }
    public void LoadFile(Stream data,RichTextBoxStreamType type)
    {
        bool rtf=IsRtf(type);
        using var reader=new StreamReader(data,rtf ? Encoding.Latin1 : EncodingFor(type),true,1024,true);
        var value=reader.ReadToEnd(); if(rtf) Rtf=value; else Text=value;
    }
    public void SaveFile(string path) => SaveFile(path,RichTextBoxStreamType.RichText);
    public void SaveFile(string path,RichTextBoxStreamType type) { using var file=File.Create(path); SaveFile(file,type); }
    public void SaveFile(Stream data,RichTextBoxStreamType type)
    {
        bool rtf=IsRtf(type);
        data.Write((rtf ? Encoding.Latin1 : EncodingFor(type)).GetBytes(rtf ? Rtf : Text));
    }
    private static bool IsRtf(RichTextBoxStreamType type) => type is RichTextBoxStreamType.RichText or RichTextBoxStreamType.RichNoOleObjs;
    private static Encoding EncodingFor(RichTextBoxStreamType type) => type == RichTextBoxStreamType.UnicodePlainText ? Encoding.Unicode : Encoding.UTF8;
    internal override void OnNative(int ev)
    {
        if (ev==28) SelectionChanged?.Invoke(this,EventArgs.Empty); else base.OnNative(ev);
    }
}

[Flags] public enum MaskFormat { ExcludePromptAndLiterals=0, IncludePrompt=1, IncludeLiterals=2, IncludePromptAndLiterals=3 }
public enum InsertKeyMode { Default, Insert, Overwrite }
public delegate void MaskInputRejectedEventHandler(object? sender,MaskInputRejectedEventArgs e);
public class MaskInputRejectedEventArgs(int position,MaskedTextResultHint hint) : EventArgs
{
    public int Position { get; }=position;
    public MaskedTextResultHint RejectionHint { get; }=hint;
}
public delegate void TypeValidationEventHandler(object? sender,TypeValidationEventArgs e);
public class TypeValidationEventArgs(Type? type,bool isValidInput,object? returnValue,string? message) : CancelEventArgs
{
    public Type? ValidatingType { get; }=type;
    public bool IsValidInput { get; }=isValidInput;
    public object? ReturnValue { get; }=returnValue;
    public string? Message { get; }=message;
}

public class MaskedTextBox : TextBoxBase
{
    internal override int Kind=>provider!=null ? 27 : EffectivePassword!=0 ? 12 : 5;
    private MaskedTextProvider? provider;
    private readonly TextNative.MaskInput handleEdit;
    private MaskFormat format=MaskFormat.IncludeLiterals;
    private CultureInfo culture=CultureInfo.CurrentCulture;
    private char prompt='_', password;
    private bool useSystemPassword, asciiOnly;
    public MaskedTextBox() { handleEdit=NativeEdit; }
    public MaskedTextBox(string mask) : this() { Mask=mask; }
    public MaskedTextBox(MaskedTextProvider maskedTextProvider) : this()
    {
        ArgumentNullException.ThrowIfNull(maskedTextProvider);
        provider=(MaskedTextProvider)maskedTextProvider.Clone();
        culture=provider.Culture; prompt=provider.PromptChar; asciiOnly=provider.AsciiOnly;
        password=provider.PasswordChar;
        UpdateValue();
    }
    public MaskedTextProvider? MaskedTextProvider => (MaskedTextProvider?)provider?.Clone();
    public string Mask
    {
        get=>provider?.Mask ?? "";
        set { if (Mask==value) return; Reconfigure(value); MaskChanged?.Invoke(this,EventArgs.Empty); }
    }
    private void Reconfigure(string? mask)
    {
        int oldKind=Kind;
        string data=provider?.ToString(true,false,false,0,provider.Length) ?? base.Text;
        provider=string.IsNullOrEmpty(mask) ? null : new MaskedTextProvider(mask,culture,true,prompt,EffectivePassword,asciiOnly);
        if (provider==null) base.Text=data;
        else provider.Set(data);
        if (oldKind!=Kind) RecreateWidget();
        UpdateValue();
    }
    public CultureInfo Culture { get=>culture; set { ArgumentNullException.ThrowIfNull(value); culture=value; Reconfigure(Mask); } }
    public bool AsciiOnly { get=>asciiOnly; set { asciiOnly=value; Reconfigure(Mask); } }
    public char PromptChar { get=>prompt; set { if (provider!=null) provider.PromptChar=value; prompt=value; UpdateValue(); } }
    private char EffectivePassword=>useSystemPassword ? '●' : password;
    public char PasswordChar
    {
        get=>password;
        set { int oldKind=Kind; if(provider!=null) provider.PasswordChar=useSystemPassword ? '●' : value; password=value; if(oldKind!=Kind) RecreateWidget(); UpdateValue(); }
    }
    public bool UseSystemPasswordChar
    {
        get=>useSystemPassword;
        set { int oldKind=Kind; useSystemPassword=value; if(provider!=null) provider.PasswordChar=EffectivePassword; if(oldKind!=Kind) RecreateWidget(); UpdateValue(); }
    }
    public MaskFormat TextMaskFormat { get=>format; set { format=value; UpdateValue(); } }
    public bool MaskCompleted => provider?.MaskCompleted ?? true;
    public bool MaskFull => provider?.MaskFull ?? true;
    public InsertKeyMode InsertKeyMode { get; set; }
    public Type? ValidatingType { get; set; }
    public event MaskInputRejectedEventHandler? MaskInputRejected;
    public event EventHandler? MaskChanged;
    public event TypeValidationEventHandler? TypeValidationCompleted;
    public override string Text
    {
        get => provider==null ? base.Text : provider.ToString(true,format.HasFlag(MaskFormat.IncludePrompt),format.HasFlag(MaskFormat.IncludeLiterals),0,provider.Length);
        set
        {
            if(provider==null) { base.Text=value; return; }
            if(!provider.Set(value ?? "",out int pos,out var hint))
            {
                MaskInputRejected?.Invoke(this,new(pos,hint)); return;
            }
            UpdateValue();
        }
    }
    internal override string SelectionBuffer => provider?.ToDisplayString() ?? Text;
    public override string SelectedText
    {
        get
        {
            if(provider==null) return base.SelectedText;
            return provider.ToString(true,format.HasFlag(MaskFormat.IncludePrompt),format.HasFlag(MaskFormat.IncludeLiterals),SelectionStart,SelectionLength);
        }
        set
        {
            if(provider==null) base.SelectedText=value;
            else ApplyInput(SelectionStart,SelectionLength,value ?? "");
        }
    }
    private void UpdateValue()
    {
        if(provider==null) return;
        if(NativeHandle!=0) Native.mf_text(NativeHandle,provider.ToDisplayString());
        UpdateTextFromPeer(Text);
    }
    private int NativeEdit(IntPtr handle,int start,int length,string replacement)
    {
        if(ReadOnly) return 0;
        if(provider==null) return replacement.Contains('\n') || replacement.Contains('\r') ? 0 : 1;
        ApplyInput(start,length,replacement);
        // Managed validation has already synchronized the prompt string and selection.
        return 0;
    }
    private void ApplyInput(int start,int length,string replacement)
    {
        if(provider==null) return;
        if(replacement.Length==0 && length==0) return;
        int position; MaskedTextResultHint hint;
        bool ok;
        if(replacement.Length==0)
            ok=provider.RemoveAt(start,start+length-1,out position,out hint);
        else if(length>0)
            ok=provider.Replace(replacement,start,start+length-1,out position,out hint);
        else if(InsertKeyMode==InsertKeyMode.Overwrite)
            ok=provider.Replace(replacement,start,out position,out hint);
        else
            ok=provider.InsertAt(replacement,start,out position,out hint);
        if(!ok) { MaskInputRejected?.Invoke(this,new(position,hint)); return; }
        int next = replacement.Length==0 ? start : provider.FindEditPositionFrom(position+1,true);
        UpdateValue();
        Select(next<0 ? provider.Length : next,0);
    }
    public object? ValidateText()
    {
        if(ValidatingType==null) return null;
        object? result=null; string? error=null;
        try
        {
            if(!MaskCompleted) throw new FormatException("Required mask positions must be filled.");
            result=TypeDescriptor.GetConverter(ValidatingType).ConvertFromString(null,culture,Text);
        }
        catch(Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException) { error=ex.Message; }
        var args=new TypeValidationEventArgs(ValidatingType,error==null,result,error);
        TypeValidationCompleted?.Invoke(this,args);
        return args.Cancel ? null : result;
    }
    protected override void OnValidating(CancelEventArgs e)
    {
        if(ValidatingType!=null && ValidateText()==null) e.Cancel=true;
        base.OnValidating(e);
    }
    internal override void ApplyNative()
    {
        base.ApplyNative();
        TextNative.mf_mask_install(NativeHandle,handleEdit);
        UpdateValue();
    }
}
