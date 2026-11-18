using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Forms.VisualStyles;
using Align = System.Drawing.ContentAlignment;

namespace System.Windows.Forms;

public interface IButtonControl
{
    DialogResult DialogResult { get; set; }
    void NotifyDefault(bool value);
    void PerformClick();
}

public sealed class FlatButtonAppearance
{
    private readonly ButtonBase button;
    private Color border, down, over, selected;
    private int width = 1;
    internal FlatButtonAppearance(ButtonBase button) => this.button = button;
    public Color BorderColor { get => border; set { border=value; button.UpdateButton(); } }
    public Color MouseDownBackColor { get => down; set { down=value; button.UpdateButton(); } }
    public Color MouseOverBackColor { get => over; set { over=value; button.UpdateButton(); } }
    public Color CheckedBackColor { get => selected; set { selected=value; button.UpdateButton(); } }
    public int BorderSize
    {
        get => width;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); width=value; button.UpdateButton(); }
    }
}

public partial class Control
{
    public static bool IsMnemonic(char charCode,string? text)
    {
        if (text==null || charCode=='&') return false;
        for (int i=0; i<text.Length-1; i++)
            if (text[i]=='&' && text[++i]!='&' && char.ToUpperInvariant(text[i])==char.ToUpperInvariant(charCode)) return true;
        return false;
    }
    private IEnumerable<Control> MnemonicChildren() =>
        Controls.Where(c=>c.Visible && c.Enabled && (c is not TabPage page || this is not TabControl tabs || tabs.SelectedTab==page))
            .OrderBy(c=>c.TabIndex);
    protected virtual bool ProcessMnemonic(char charCode)
    {
        foreach (var child in MnemonicChildren())
            if (child.ProcessMnemonic(charCode)) return true;
        return false;
    }
    private IEnumerable<Control> ControlsInTabOrder()
    {
        foreach (var child in MnemonicChildren())
        {
            yield return child;
            foreach (var descendant in child.ControlsInTabOrder()) yield return descendant;
        }
    }
    internal bool FocusPast(Control? after = null)
    {
        var controls = ControlsInTabOrder();
        if (after != null) controls = controls.SkipWhile(c=>c!=after).Skip(1);
        foreach (var c in controls)
        {
            if (c is ContainerControl { ActiveControl: {} active } && active.Focus()) return true;
            if (!c.TabStop || c is Panel or GroupBox or SplitContainer or ProgressBar or PictureBox or Splitter or ToolStrip ||
                c is Label and not LinkLabel) continue;
            if (c.Focus()) return true;
        }
        return false;
    }
}
public class Label : Control
{
    [DllImport("MacForms")] private static extern void mf_label_title(nint view,string text);
    internal override int Kind => 3;
    private bool mnemonic=true;
    public Label() { TabStop=false; }
    public bool AutoEllipsis { get; set; }
    public bool UseMnemonic
    {
        get => mnemonic;
        set { mnemonic=value; if (NativeHandle!=0) ApplyTitle(); }
    }
    private void ApplyTitle()
    {
        if (!mnemonic) mf_label_title(ViewHandle,Text);
        else Native.mf_text(NativeHandle,Text);
    }
    protected override void OnTextChanged(EventArgs e) { if (!mnemonic && NativeHandle!=0) ApplyTitle(); base.OnTextChanged(e); }
    internal override void ApplyNative() { base.ApplyNative(); if (!mnemonic) ApplyTitle(); }
    protected override bool ProcessMnemonic(char c)
    {
        if (UseMnemonic && IsMnemonic(c,Text) && Visible && Enabled)
            return FindForm()?.FocusPast(this) ?? Parent?.FocusPast(this) ?? false;
        return base.ProcessMnemonic(c);
    }
}

public abstract class ButtonBase : Control
{
    [DllImport("MacForms")] private static extern void mf_button_drawing(nint view,int managed,int threeState,string caption);
    [DllImport("MacForms")] private static extern int mf_button_flags(nint view);
    private Image? image;
    private ImageList? list;
    private int index=-1;
    private string key="";
    private Align imageAlign=Align.MiddleCenter;
    private TextImageRelation relation;
    private FlatStyle flat=FlatStyle.Standard;
    private bool mnemonic=true,ellipsis,visual=true,compatible,isDefault;
    private ICommand? command;
    private object? parameter;
    private bool? beforeCommand;
    protected ButtonBase() { FlatAppearance=new(this); base.TextAlign=Align.MiddleCenter; }
    public FlatButtonAppearance FlatAppearance { get; }
    public ICommand? Command
    {
        get=>command;
        set
        {
            if (Equals(command,value)) return;
            if (command!=null) command.CanExecuteChanged-=RefreshCommand;
            command=value;
            OnCommandChanged(EventArgs.Empty);
            if (command!=null)
            {
                beforeCommand??=Enabled;
                command.CanExecuteChanged+=RefreshCommand;
                Enabled=command.CanExecute(parameter);
            }
            else if (beforeCommand is bool previous) { Enabled=previous; beforeCommand=null; }
        }
    }
    public object? CommandParameter
    {
        get=>parameter;
        set { if (!Equals(parameter,value)) { parameter=value; OnCommandParameterChanged(EventArgs.Empty); } }
    }
    public event EventHandler? CommandChanged,CommandParameterChanged,CommandCanExecuteChanged;
    protected virtual void OnCommandChanged(EventArgs e)=>CommandChanged?.Invoke(this,e);
    protected virtual void OnCommandParameterChanged(EventArgs e)=>CommandParameterChanged?.Invoke(this,e);
    protected virtual void OnCommandCanExecuteChanged(EventArgs e)=>CommandCanExecuteChanged?.Invoke(this,e);
    protected virtual void OnRequestCommandExecute(EventArgs e)=>command?.Execute(parameter);
    private void RefreshCommand(object? sender,EventArgs e)
    {
        if (command!=null) Enabled=command.CanExecute(parameter);
        OnCommandCanExecuteChanged(e);
    }
    protected override void OnClick(EventArgs e) { base.OnClick(e); OnRequestCommandExecute(e); }
    public FlatStyle FlatStyle
    {
        get=>flat;
        set { ValidateEnum(value); flat=value; UpdateButton(); }
    }
    public bool AutoEllipsis { get=>ellipsis; set { ellipsis=value; UpdateButton(); } }
    public bool UseCompatibleTextRendering { get=>compatible; set { compatible=value; UpdateButton(); } }
    public bool UseMnemonic { get=>mnemonic; set { mnemonic=value; UpdateButton(); } }
    public bool UseVisualStyleBackColor { get=>visual; set { visual=value; UpdateButton(); } }
    protected internal bool IsDefault { get=>isDefault; set { isDefault=value; UpdateButton(); } }
    public override Color BackColor
    {
        get => base.BackColor;
        set { base.BackColor=value; visual=value.IsEmpty; UpdateButton(); }
    }
    public override Align TextAlign
    {
        get=>base.TextAlign;
        set { ValidateEnum(value); base.TextAlign=value; }
    }
    public Align ImageAlign { get=>imageAlign; set { ValidateEnum(value); imageAlign=value; UpdateButton(); } }
    public TextImageRelation TextImageRelation { get=>relation; set { ValidateEnum(value); relation=value; UpdateButton(); } }
    public Image? Image
    {
        get
        {
            if (image!=null || list==null) return image;
            if (key.Length>0) return list.Images[key];
            return ImageIndex>=0 ? list.Images[ImageIndex] : null;
        }
        set
        {
            if (value!=null) { index=-1; key=""; ImageList=null; }
            image=value; UpdateButton();
        }
    }
    public ImageList? ImageList
    {
        get => list;
        set
        {
            if (list==value) return;
            if (list!=null) { list.Changed-=ImagesChanged; list.Disposed-=ImagesDisposed; }
            list=value;
            if (list!=null) { image=null; list.Changed+=ImagesChanged; list.Disposed+=ImagesDisposed; }
            UpdateButton();
        }
    }
    public int ImageIndex
    {
        get=>list==null ? index : Math.Min(index,list.Images.Count-1);
        set { if (value < -1) throw new ArgumentOutOfRangeException(nameof(value)); index=value; key=""; if (value>=0) image=null; UpdateButton(); }
    }
    public string ImageKey
    {
        get => key;
        set { key=value ?? ""; index=-1; if (key.Length>0) image=null; UpdateButton(); }
    }
    private void ImagesChanged(object? sender,EventArgs e) => UpdateButton();
    private void ImagesDisposed(object? sender,EventArgs e) => ImageList=null;
    protected override void Dispose(bool disposing)
    {
        if (disposing && command!=null) command.CanExecuteChanged-=RefreshCommand;
        if (disposing && list!=null) { list.Changed-=ImagesChanged; list.Disposed-=ImagesDisposed; list=null; }
        base.Dispose(disposing);
    }
    internal static void ValidateEnum<T>(T value) where T:struct,Enum
    {
        if (!Enum.IsDefined(value)) throw new InvalidEnumArgumentException(nameof(value),Convert.ToInt32(value),typeof(T));
    }
    private string Caption => UseMnemonic ? Text.Replace("&&","\0").Replace("&","").Replace('\0','&') : Text;
    protected override void OnTextChanged(EventArgs e) { UpdateButton(); base.OnTextChanged(e); }
    internal override void ApplyNative() { base.ApplyNative(); UpdateButton(); }
    internal virtual Appearance Look => Appearance.Normal;
    internal virtual Align MarkAlign => Align.MiddleLeft;
    internal virtual bool Selected => false;
    private bool IsOption => this is CheckBox or RadioButton;
    internal void FitContents()
    {
        if (!AutoSize) return;
        var text=TextRenderer.MeasureText(Text,Font,Size.Empty,UseMnemonic ? TextFormatFlags.Default : TextFormatFlags.NoPrefix);
        var size=Image is {} icon ? list?.ImageSize ?? icon.Size : Size.Empty;
        bool apart=text.Width!=0 && size.Width!=0;
        int w=relation is TextImageRelation.ImageBeforeText or TextImageRelation.TextBeforeImage ?
            text.Width+size.Width+(apart ? 3 : 0) : Math.Max(text.Width,size.Width);
        int h=relation is TextImageRelation.ImageAboveText or TextImageRelation.TextAboveImage ?
            text.Height+size.Height+(apart ? 3 : 0) : Math.Max(text.Height,size.Height);
        if (IsOption && Look==Appearance.Normal) { w+=16; h=Math.Max(h,13); }
        var preferred=new Size(w+Padding.Horizontal()+8,h+Padding.Vertical()+8);
        if (this is Button { AutoSizeMode:AutoSizeMode.GrowOnly }) preferred=new Size(Math.Max(preferred.Width,Width),Math.Max(preferred.Height,Height));
        Size=preferred;
    }
    internal void UpdateButton()
    {
        FitContents();
        if (NativeHandle==0 || IsDisposed) return;
        bool custom = flat is FlatStyle.Flat or FlatStyle.Popup || Look==Appearance.Button || MarkAlign!=Align.MiddleLeft ||
            Image!=null || !ForeColor.IsEmpty || !BackColor.IsEmpty || !visual || compatible || ellipsis ||
            TextAlign!=(IsOption ? Align.MiddleLeft : Align.MiddleCenter);
        mf_button_drawing(ViewHandle,custom ? 1 : 0,this is CheckBox { ThreeState:true } ? 1 : 0,Caption);
    }
    protected override bool ProcessMnemonic(char c)
    {
        if (!UseMnemonic || !IsMnemonic(c,Text) || !Visible || !Enabled) return false;
        Focus();
        OnClick(EventArgs.Empty);
        return true;
    }
    internal override void OnNative(int ev)
    {
        if (ev!=72) { base.OnNative(ev); return; }
        using var g=new Graphics(ClientRectangle);
        DrawFace(g,mf_button_flags(ViewHandle));
        OnPaint(new(g,ClientRectangle));
    }
    private static Rectangle Aligned(Rectangle bounds,Size size,Align align)
    {
        int a=(int)align;
        int x=(a&0x444)!=0 ? bounds.Right-size.Width : (a&0x222)!=0 ? bounds.Left+(bounds.Width-size.Width)/2 : bounds.Left;
        int y=(a&0x700)!=0 ? bounds.Bottom-size.Height : (a&0x70)!=0 ? bounds.Top+(bounds.Height-size.Height)/2 : bounds.Top;
        return new(x,y,size.Width,size.Height);
    }
    private static void DrawBorder(Graphics g,Rectangle area,Color ink,int border)
    {
        int w=Math.Min(border,Math.Min(area.Width,area.Height)/2);
        if (w<1) return;
        using var brush=new SolidBrush(ink);
        g.FillRectangle(brush,area.Left,area.Top,area.Width,w);
        g.FillRectangle(brush,area.Left,area.Bottom-w,area.Width,w);
        g.FillRectangle(brush,area.Left,area.Top,w,area.Height);
        g.FillRectangle(brush,area.Right-w,area.Top,w,area.Height);
    }
    private void DrawFace(Graphics g,int flags)
    {
        var r=ClientRectangle;
        int state=!Enabled ? 4 : (flags&1)!=0 ? 3 : (flags&2)!=0 ? 2 : 1;
        Color ink=Enabled ? ForeColor.IsEmpty ? Color.Black : ForeColor : Color.Gray;
        bool glyph=IsOption && Look==Appearance.Normal;
        var content=Rectangle.Inflate(r,-2,-2);
        if (glyph)
        {
            if (!BackColor.IsEmpty) { using var brush=new SolidBrush(BackColor); g.FillRectangle(brush,r); }
            int value=this is CheckBox cb ? (int)cb.CheckState : Selected ? 1 : 0;
            var theme=new VisualStyleRenderer("BUTTON",this is CheckBox ? 3 : 2,state+4*value);
            var spot=Aligned(content,theme.GetPartSize(g,ThemeSizeType.True),MarkAlign);
            theme.DrawBackground(g,spot);
            if (((int)MarkAlign&0x111)!=0) { content.Width-=spot.Width+3; content.X+=spot.Width+3; }
            else if (((int)MarkAlign&0x444)!=0) content.Width-=spot.Width+3;
        }
        else if (flat is FlatStyle.Flat or FlatStyle.Popup || !visual)
        {
            Color fill=(flags&1)!=0 ? FlatAppearance.MouseDownBackColor : (flags&2)!=0 ? FlatAppearance.MouseOverBackColor : Selected ? FlatAppearance.CheckedBackColor : BackColor;
            if (fill.IsEmpty) fill=BackColor.IsEmpty ? state==3 ? Color.Silver : Color.FromArgb(240,240,240) : BackColor;
            using var brush=new SolidBrush(fill); g.FillRectangle(brush,r);
            if (flat!=FlatStyle.Popup || state is 2 or 3)
                DrawBorder(g,r,FlatAppearance.BorderColor.IsEmpty ? ink : FlatAppearance.BorderColor,FlatAppearance.BorderSize);
        }
        else new VisualStyleRenderer("BUTTON",1,state==1 && ((flags&8)!=0 || IsDefault) ? 5 : Selected ? 3 : state).DrawBackground(g,r);
        DrawContent(g,content,ink);
        if ((flags&4)!=0) ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(r,-3,-3),ink,Color.White);
    }
    private void DrawContent(Graphics g,Rectangle r,Color ink)
    {
        if (r.Width<=0 || r.Height<=0) return;
        TextFormatFlags format=UseMnemonic ? TextFormatFlags.HidePrefix : TextFormatFlags.NoPrefix;
        format|=TextFormatFlags.NoPadding|TextFormatFlags.WordBreak;
        if (AutoEllipsis) format|=TextFormatFlags.EndEllipsis;
        var textRect=r;
        if (Image is {} icon)
        {
            var sz=list?.ImageSize ?? icon.Size;
            var spot=Aligned(r,sz,ImageAlign);
            if (TextImageRelation!=TextImageRelation.Overlay && Text.Length!=0)
            {
                var size=TextRenderer.MeasureText(g,Text,Font,r.Size,format);
                bool horiz=relation is TextImageRelation.ImageBeforeText or TextImageRelation.TextBeforeImage;
                bool first=relation is TextImageRelation.ImageBeforeText or TextImageRelation.ImageAboveText;
                int gap=3;
                var combined=horiz ? new Size(Math.Min(r.Width,sz.Width+gap+size.Width),Math.Max(sz.Height,size.Height))
                    : new Size(Math.Max(sz.Width,size.Width),Math.Min(r.Height,sz.Height+gap+size.Height));
                var block=Aligned(r,combined,TextAlign);
                if (horiz)
                {
                    spot.X=first ? block.Left : block.Right-sz.Width;
                    textRect=Rectangle.FromLTRB(first ? spot.Right+gap : block.Left,r.Top,first ? block.Right : spot.Left-gap,r.Bottom);
                }
                else
                {
                    spot.Y=first ? block.Top : block.Bottom-sz.Height;
                    textRect=Rectangle.FromLTRB(r.Left,first ? spot.Bottom+gap : block.Top,r.Right,first ? block.Bottom : spot.Top-gap);
                }
            }
            g.DrawImage(icon,spot);
        }
        if (textRect.Width<=0 || textRect.Height<=0 || Text.Length==0) return;
        if (((int)TextAlign&0x222)!=0) format|=TextFormatFlags.HorizontalCenter;
        if (((int)TextAlign&0x444)!=0) format|=TextFormatFlags.Right;
        if (((int)TextAlign&0x70)!=0) format|=TextFormatFlags.VerticalCenter;
        if (((int)TextAlign&0x700)!=0) format|=TextFormatFlags.Bottom;
        if (compatible)
        {
            var size=Size.Ceiling(g.MeasureString(Caption,Font,textRect.Width));
            using var brush=new SolidBrush(ink);
            g.DrawString(Caption,Font,brush,Aligned(textRect,new Size(Math.Min(size.Width,textRect.Width),Math.Min(size.Height,textRect.Height)),TextAlign));
        }
        else TextRenderer.DrawText(g,Text,Font,textRect,ink,format);
    }
}

public class Button : ButtonBase,IButtonControl
{
    internal override int Kind => 4;
    public Button() { Size=new(75,23); }
    private AutoSizeMode sizing=AutoSizeMode.GrowOnly;
    public AutoSizeMode AutoSizeMode { get=>sizing; set { ValidateEnum(value); sizing=value; FitContents(); } }
    public virtual DialogResult DialogResult { get; set; }
    public void NotifyDefault(bool value) => IsDefault=value;
    public void PerformClick() { if (Enabled && Visible) OnClick(EventArgs.Empty); }
    protected override void OnClick(EventArgs e)
    {
        if (DialogResult!=DialogResult.None && FindForm() is {} f) f.DialogResult=DialogResult;
        base.OnClick(e);
    }
}

public class CheckBox : ButtonBase
{
    internal override int Kind => 6;
    private CheckState state;
    private bool three;
    private Appearance appearance;
    private Align mark=Align.MiddleLeft;
    public CheckBox() { TextAlign=Align.MiddleLeft; Size=new(104,24); }
    public bool AutoCheck { get; set; } = true;
    public bool ThreeState { get=>three; set { three=value; UpdateButton(); } }
    public Appearance Appearance
    {
        get=>appearance;
        set { ValidateEnum(value); if (value==appearance) return; appearance=value; UpdateButton(); OnAppearanceChanged(EventArgs.Empty); }
    }
    public Align CheckAlign { get=>mark; set { ValidateEnum(value); mark=value; UpdateButton(); } }
    internal override Appearance Look=>appearance;
    internal override Align MarkAlign=>mark;
    internal override bool Selected=>Checked;
    public bool Checked { get=>state!=CheckState.Unchecked; set=>CheckState=value ? CheckState.Checked : CheckState.Unchecked; }
    public CheckState CheckState
    {
        get=>state;
        set
        {
            ValidateEnum(value);
            if (state==value) return;
            bool was=Checked;
            state=value;
            Sync();
            if (was!=Checked) OnCheckedChanged(EventArgs.Empty);
            OnCheckStateChanged(EventArgs.Empty);
        }
    }
    public event EventHandler? CheckedChanged,CheckStateChanged,AppearanceChanged;
    protected virtual void OnCheckedChanged(EventArgs e)=>CheckedChanged?.Invoke(this,e);
    protected virtual void OnCheckStateChanged(EventArgs e)=>CheckStateChanged?.Invoke(this,e);
    protected virtual void OnAppearanceChanged(EventArgs e)=>AppearanceChanged?.Invoke(this,e);
    private void Sync() { if (NativeHandle!=0) { Native.mf_checked(NativeHandle,(int)state); Invalidate(); } }
    internal override void ApplyNative() { base.ApplyNative(); Sync(); }
    protected override void OnClick(EventArgs e)
    {
        if (AutoCheck) CheckState=state switch { CheckState.Unchecked=>CheckState.Checked,
            CheckState.Checked when ThreeState=>CheckState.Indeterminate,_=>CheckState.Unchecked };
        else Sync();
        base.OnClick(e);
    }
}

public class RadioButton : ButtonBase
{
    internal override int Kind => 10;
    private bool selected;
    private Appearance appearance;
    private Align mark=Align.MiddleLeft;
    public RadioButton() { TextAlign=Align.MiddleLeft; Size=new(104,24); }
    public bool AutoCheck { get; set; } = true;
    public Appearance Appearance
    {
        get=>appearance;
        set { ValidateEnum(value); if (appearance==value) return; appearance=value; UpdateButton(); OnAppearanceChanged(EventArgs.Empty); }
    }
    public Align CheckAlign { get=>mark; set { ValidateEnum(value); mark=value; UpdateButton(); } }
    internal override Appearance Look=>appearance;
    internal override Align MarkAlign=>mark;
    internal override bool Selected=>selected;
    public bool Checked
    {
        get=>selected;
        set
        {
            if (value==selected) return;
            selected=value;
            Sync();
            if (AutoCheck)
            {
                TabStop=value;
                if (value && Parent!=null)
                    foreach (var sibling in Parent.Controls.OfType<RadioButton>())
                        if (sibling!=this && sibling.AutoCheck) sibling.Checked=false;
            }
            OnCheckedChanged(EventArgs.Empty);
        }
    }
    public event EventHandler? CheckedChanged,AppearanceChanged;
    protected virtual void OnCheckedChanged(EventArgs e)=>CheckedChanged?.Invoke(this,e);
    protected virtual void OnAppearanceChanged(EventArgs e)=>AppearanceChanged?.Invoke(this,e);
    private void Sync() { if (NativeHandle!=0) { Native.mf_checked(NativeHandle,selected ? 1 : 0); Invalidate(); } }
    internal override void ApplyNative() { base.ApplyNative(); Sync(); }
    public void PerformClick() { if (Enabled && Visible) OnClick(EventArgs.Empty); }
    protected override void OnClick(EventArgs e)
    {
        if (AutoCheck) Checked=true;
        if (Parent!=null) foreach (var sibling in Parent.Controls.OfType<RadioButton>()) sibling.Sync();
        else Sync();
        base.OnClick(e);
    }
}
