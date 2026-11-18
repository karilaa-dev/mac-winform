using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public enum LeftRightAlignment { Left, Right }
public abstract class UpDownBase : ContainerControl
{
    [DllImport("MacForms")] private static extern void mf_updown_options(IntPtr h,int readOnly,int side,int align);
    private bool readOnly;
    private HorizontalAlignment alignment;
    private LeftRightAlignment side=LeftRightAlignment.Right;
    internal override int Kind=>33;
    public UpDownBase() { Size=new(120,23); }
    public bool ReadOnly { get=>readOnly; set { readOnly=value; Options(); } }
    public bool InterceptArrowKeys { get; set; }=true;
    public new HorizontalAlignment TextAlign { get=>alignment; set { alignment=value; Options(); } }
    public LeftRightAlignment UpDownAlign { get=>side; set { side=value; Options(); } }
    protected bool ChangingText { get; set; }
    protected bool UserEdit { get; set; }
    public abstract void UpButton();
    public abstract void DownButton();
    protected abstract void UpdateEditText();
    protected virtual void ValidateEditText() {}
    public override string Text
    {
        get=>base.Text;
        set { base.Text=value; if (!ChangingText) { UserEdit=true; ValidateEditText(); } }
    }
    protected void DisplayValue(string value)
    {
        ChangingText=true;
        try { base.Text=value; UserEdit=false; }
        finally { ChangingText=false; }
    }
    private void Options()
    {
        if (NativeHandle!=0) mf_updown_options(NativeHandle,readOnly ? 1 : 0,(int)side,(int)alignment);
    }
    internal override void ApplyNative() { base.ApplyNative(); Options(); UpdateEditText(); }
    internal override void OnNative(int ev)
    {
        if (ev==2) { UserEdit=true; base.OnNative(ev); return; }
        if (ev==34) UpButton();
        else if(ev==35) DownButton();
        else if(ev==36) { base.OnNative(2); UserEdit=true; ValidateEditText(); }
        else base.OnNative(ev);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if(!InterceptArrowKeys || e.Handled) return;
        if(e.KeyCode==Keys.Up) { UpButton(); e.Handled=true; }
        else if(e.KeyCode==Keys.Down) { DownButton(); e.Handled=true; }
    }
}
public class NumericUpDown : UpDownBase, ISupportInitialize
{
    internal override int Kind=>16;
    private decimal position,minimum,maximum=100,increment=1;
    private bool initializing,thousands,hex;
    private int places;
    public NumericUpDown() { UpdateEditText(); }
    public decimal Minimum
    {
        get=>minimum;
        set { minimum=value; if(maximum<minimum) maximum=minimum; if(!initializing && position<minimum) Value=minimum; }
    }
    public decimal Maximum
    {
        get=>maximum;
        set { maximum=value; if(minimum>maximum) minimum=maximum; if(!initializing && position>maximum) Value=maximum; }
    }
    public decimal Increment { get=>increment; set { ArgumentOutOfRangeException.ThrowIfNegative(value); increment=value; } }
    public int DecimalPlaces { get=>places; set { if((uint)value>99) throw new ArgumentOutOfRangeException(nameof(value)); places=value; UpdateEditText(); } }
    public bool Hexadecimal { get=>hex; set { hex=value; UpdateEditText(); } }
    public bool ThousandsSeparator { get=>thousands; set { thousands=value; UpdateEditText(); } }
    public decimal Value
    {
        get { if(UserEdit) ValidateEditText(); return position; }
        set
        {
            if(!initializing && (value<minimum || value>maximum)) throw new ArgumentOutOfRangeException(nameof(value));
            if(position==value) return;
            position=value; ValueChanged?.Invoke(this,EventArgs.Empty); UpdateEditText();
        }
    }
    public event EventHandler? ValueChanged;
    public override void UpButton()
    {
        if(UserEdit) ValidateEditText();
        decimal next; try { next=position+increment; } catch (OverflowException) { next=decimal.MaxValue; }
        Value=Math.Min(maximum,next);
    }
    public override void DownButton()
    {
        if(UserEdit) ValidateEditText();
        decimal next; try { next=position-increment; } catch (OverflowException) { next=decimal.MinValue; }
        Value=Math.Max(minimum,next);
    }
    protected override void ValidateEditText()
    {
        UserEdit=false;
        if(hex)
        {
            if(long.TryParse(Text,NumberStyles.HexNumber,CultureInfo.CurrentCulture,out long raw))
                Value=Math.Clamp(raw,minimum,maximum);
        }
        else if(decimal.TryParse(Text,NumberStyles.Number,CultureInfo.CurrentCulture,out decimal raw))
            Value=Math.Clamp(raw,minimum,maximum);
        UpdateEditText();
    }
    protected override void UpdateEditText()
    {
        string rendered=hex ? ((long)Math.Clamp(position,long.MinValue,long.MaxValue)).ToString("X",CultureInfo.CurrentCulture)
            : position.ToString((thousands ? "N" : "F")+places,CultureInfo.CurrentCulture);
        DisplayValue(rendered);
    }
    public void BeginInit()=>initializing=true;
    public void EndInit() { initializing=false; Value=Math.Clamp(position,minimum,maximum); UpdateEditText(); }
}

public class DomainUpDown : UpDownBase
{
    private int selected=-1;
    private bool sorted;
    public DomainUpDown() { Items=new(this); }
    public DomainUpDownItemCollection Items { get; }
    public bool Wrap { get; set; }
    public bool Sorted { get=>sorted; set { sorted=value; if(value) Items.Sort(); } }
    public object? SelectedItem { get=>(uint)selected<(uint)Items.Count ? Items[selected] : null; set=>SelectedIndex=Items.IndexOf(value); }
    public int SelectedIndex
    {
        get=>selected;
        set
        {
            if(value < -1 || value >= Items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if(selected==value) return;
            selected=value; UpdateEditText(); SelectedItemChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public event EventHandler? SelectedItemChanged;
    protected override void UpdateEditText() => DisplayValue(SelectedItem?.ToString() ?? "");
    protected override void ValidateEditText()
    {
        if(!UserEdit) return;
        UserEdit=false;
        string pending=Text;
        for(int i=0;i<Items.Count;i++)
        {
            if(string.Equals(Items[i]?.ToString(),pending,StringComparison.CurrentCultureIgnoreCase))
            { SelectedIndex=i; return; }
        }
        if(selected!=-1) { selected=-1; SelectedItemChanged?.Invoke(this,EventArgs.Empty); }
    }
    public override void UpButton()
    {
        if(UserEdit) ValidateEditText();
        if(Items.Count==0) return;
        if(selected>0) SelectedIndex=selected-1;
        else if(Wrap) SelectedIndex=Items.Count-1;
    }
    public override void DownButton()
    {
        if(UserEdit) ValidateEditText();
        if(selected<Items.Count-1) SelectedIndex=selected+1;
        else if(Wrap && Items.Count>0) SelectedIndex=0;
    }
    public class DomainUpDownItemCollection : ArrayList
    {
        private readonly DomainUpDown owner;
        internal DomainUpDownItemCollection(DomainUpDown owner) => this.owner=owner;
        private void Changed(object? old)
        {
            if(owner.sorted) base.Sort(new TextComparer());
            int index=old==null ? -1 : IndexOf(old);
            if(index!=owner.selected) owner.SelectedIndex=index;
            else if(index>=0) owner.UpdateEditText();
        }
        private sealed class TextComparer : IComparer
        {
            public int Compare(object? a,object? b)=>string.Compare(a?.ToString(),b?.ToString(),StringComparison.CurrentCulture);
        }
        public override int Add(object? value) { var old=owner.SelectedItem; int i=base.Add(value); Changed(old); return owner.sorted ? IndexOf(value) : i; }
        public override void Insert(int index,object? value) { var old=owner.SelectedItem; base.Insert(index,value); Changed(old); }
        public override void RemoveAt(int index) { var old=owner.SelectedItem; base.RemoveAt(index); Changed(old); }
        public override void Clear() { base.Clear(); owner.SelectedIndex=-1; }
        public override void AddRange(ICollection c) { foreach(var item in c) Add(item); }
        public override void Sort() { var old=owner.SelectedItem; base.Sort(new TextComparer()); Changed(old); }
        public override object? this[int index]
        {
            get=>base[index];
            set { var old=owner.SelectedItem; base[index]=value; Changed(old); }
        }
    }
}
