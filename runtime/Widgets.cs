using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms;

public class Panel : ScrollableControl { }
public class GroupBox : Control { internal override int Kind => 11; }
public class TextBox : TextBoxBase
{
    internal override int Kind => UseSystemPasswordChar || PasswordChar != 0 ? 12 : Multiline ? 9 : 5;
    private bool secure;
    private char passwordChar;
    public bool UseSystemPasswordChar { get => secure; set { if (secure != value) { secure = value; RecreateWidget(); } } }
    public char PasswordChar { get => passwordChar; set { if (passwordChar != value) { passwordChar = value; RecreateWidget(); } } }
}
public enum CheckState { Unchecked, Checked, Indeterminate }
public abstract class ListControl : Control
{
    public class ObjectCollection : Collection<object>
    {
        private readonly ListControl owner;
        internal ObjectCollection(ListControl owner) { this.owner = owner; }
        public new int Add(object item) { base.Add(item); return Count-1; }
        public void AddRange(object[] items) { foreach (var item in items) Add(item); }
        protected override void InsertItem(int index, object item) { base.InsertItem(index,item); owner.RefreshItems(); }
        protected override void RemoveItem(int index) { base.RemoveItem(index); owner.RefreshItems(); }
        protected override void ClearItems() { base.ClearItems(); owner.RefreshItems(); }
        protected override void SetItem(int index,object item) { base.SetItem(index,item); owner.RefreshItems(); }
    }
    private int index = -1;
    private object? source;
    private string displayMember = "";
    public ListControl() { Items = CreateItems(); }
    internal virtual ObjectCollection CreateItems() => new(this);
    public ObjectCollection Items { get; }
    internal List<object> Values() => ((source as IEnumerable) ?? Items).Cast<object>().ToList();
    public object? DataSource { get => source; set { source = value; index = -1; RefreshItems(); SelectedIndex = Values().Count == 0 ? -1 : 0; } }
    public string DisplayMember { get => displayMember; set { displayMember = value; RefreshItems(); } }
    public bool FormattingEnabled { get; set; }
    public string ValueMember { get; set; } = "";
    internal string Display(object item) => (TypeDescriptor.GetProperties(item)[DisplayMember]?.GetValue(item) ?? item).ToString() ?? "";
    public int SelectedIndex
    {
        get => index;
        set
        {
            if (value < -1 || value >= Values().Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (index == value) return;
            index = value;
            base.Text = index >= 0 ? Display(Values()[index]) : "";
            if (NativeHandle != 0) SelectNative(index);
            SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    public object? SelectedItem { get => Values().ElementAtOrDefault(index); set => SelectedIndex = value == null ? -1 : Values().IndexOf(value); }
    public object? SelectedValue
    {
        get => SelectedItem is {} item ? ValueOf(item) : null;
        set => SelectedIndex = Values().FindIndex(item => Equals(ValueOf(item),value));
    }
    private object? ValueOf(object item) => TypeDescriptor.GetProperties(item)[ValueMember]?.GetValue(item) ?? item;
    public event EventHandler? SelectedIndexChanged;
    internal virtual void SelectNative(int selected) => Native.mf_select(NativeHandle,selected);
    internal virtual void RefreshItems()
    {
        if (NativeHandle == 0) return;
        var values = Values();
        Native.mf_clear_items(NativeHandle);
        foreach (var item in values) Native.mf_add_item(NativeHandle,Display(item));
        if (index >= values.Count) SelectedIndex = -1;
        Native.mf_select(NativeHandle,index);
    }
    internal override void ApplyNative() => RefreshItems();
    internal override void OnNative(int ev)
    {
        if (ev == 4) SelectedIndex = Native.mf_get_selected(NativeHandle);
        if (ev == 2 && index >= 0 && this is ComboBox)
        {
            index = -1;
            SelectedIndexChanged?.Invoke(this,EventArgs.Empty);
        }
        base.OnNative(ev);
    }
}
public class ComboBox : ListControl
{
    internal override int Kind => style == ComboBoxStyle.DropDownList ? 7 : 23;
    private ComboBoxStyle style = ComboBoxStyle.DropDown;
    public ComboBoxStyle DropDownStyle
    {
        get => style;
        set { if (style != value) { style = value; RecreateWidget(); } }
    }
    public int DropDownWidth { get; set; }
}
public class ListBox : ListControl
{
    internal override int Kind => 8;
    public bool IntegralHeight { get; set; } = true;
}
public class DateTimePicker : Control
{
    internal override int Kind => 15;
    private DateTime value = DateTime.Now;
    public DateTime Value
    {
        get => value;
        set { if (this.value == value) return; this.value = value; if (NativeHandle != 0) ApplyNative(); ValueChanged?.Invoke(this,EventArgs.Empty); }
    }
    public event EventHandler? ValueChanged;
    internal override void ApplyNative() => Native.mf_date_value(NativeHandle,new DateTimeOffset(value).ToUnixTimeSeconds());
    internal override void OnNative(int ev) { if (ev == 7) Value = DateTimeOffset.FromUnixTimeSeconds(Native.mf_get_date_value(NativeHandle)).LocalDateTime; base.OnNative(ev); }
}
