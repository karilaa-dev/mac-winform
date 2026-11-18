using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public class DataGridViewCellStyle
{
    internal event Action? Changed;
    private Font? font;
    private Color back,fore,selectionBack,selectionFore;
    private string format = "";
    private IFormatProvider provider = CultureInfo.CurrentCulture;
    private object? nullValue = "";
    private void Set<T>(ref T target,T value)
    {
        if (EqualityComparer<T>.Default.Equals(target,value)) return;
        target = value;
        Changed?.Invoke();
    }
    public Font? Font { get => font; set => Set(ref font,value); }
    public Color BackColor { get => back; set => Set(ref back,value); }
    public Color ForeColor { get => fore; set => Set(ref fore,value); }
    public Color SelectionBackColor { get => selectionBack; set => Set(ref selectionBack,value); }
    public Color SelectionForeColor { get => selectionFore; set => Set(ref selectionFore,value); }
    public string Format { get => format; set => Set(ref format,value); }
    public IFormatProvider FormatProvider { get => provider; set => Set(ref provider,value); }
    public object? NullValue { get => nullValue; set => Set(ref nullValue,value); }
    internal void Apply(Control control)
    {
        if (Font != null) control.Font = Font;
        if (!BackColor.IsEmpty) control.BackColor = BackColor;
        if (!ForeColor.IsEmpty) control.ForeColor = ForeColor;
    }
}
public interface IDataGridViewEditingControl
{
    DataGridView? EditingControlDataGridView { get; set; }
    object EditingControlFormattedValue { get; set; }
    int EditingControlRowIndex { get; set; }
    bool EditingControlValueChanged { get; set; }
    Cursor EditingPanelCursor { get; }
    bool RepositionEditingControlOnValueChange { get; }
    void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle);
    bool EditingControlWantsInputKey(Keys keyData,bool dataGridViewWantsInputKey);
    object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context);
    void PrepareEditingControlForEdit(bool selectAll);
}
public class DataGridViewEditingControlShowingEventArgs(Control control,DataGridViewCellStyle cellStyle) : EventArgs
{
    private DataGridViewCellStyle style = cellStyle ?? throw new ArgumentNullException(nameof(cellStyle));
    public Control Control { get; } = control ?? throw new ArgumentNullException(nameof(control));
    public DataGridViewCellStyle CellStyle { get => style; set => style = value ?? throw new ArgumentNullException(nameof(value)); }
}
public delegate void DataGridViewEditingControlShowingEventHandler(object? sender,DataGridViewEditingControlShowingEventArgs e);
public class DataGridViewTextBoxEditingControl : TextBox, IDataGridViewEditingControl
{
    public virtual DataGridView? EditingControlDataGridView { get; set; }
    public virtual object EditingControlFormattedValue { get => Text; set => Text = (string)value; }
    public virtual int EditingControlRowIndex { get; set; } = -1;
    public virtual bool EditingControlValueChanged { get; set; }
    public virtual Cursor EditingPanelCursor => Cursors.Default;
    public virtual bool RepositionEditingControlOnValueChange => false;
    public virtual void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle) => dataGridViewCellStyle.Apply(this);
    public virtual bool EditingControlWantsInputKey(Keys keyData,bool dataGridViewWantsInputKey) => (keyData & Keys.KeyCode) switch
    {
        Keys.Left => SelectionLength != 0 || SelectionStart != 0,
        Keys.Right => SelectionLength != 0 || SelectionStart < TextLength,
        Keys.Home or Keys.End => true,
        _ => !dataGridViewWantsInputKey
    };
    public virtual object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) => Text;
    public virtual void PrepareEditingControlForEdit(bool selectAll) { if (selectAll) SelectAll(); else Select(TextLength,0); }
    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        EditingControlDataGridView?.NotifyCurrentCellDirty(true);
    }
}
public class DataGridViewComboBoxEditingControl : ComboBox, IDataGridViewEditingControl
{
    public DataGridViewComboBoxEditingControl()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        SelectedIndexChanged += (_,_) => EditingControlDataGridView?.NotifyCurrentCellDirty(true);
    }
    public virtual DataGridView? EditingControlDataGridView { get; set; }
    public virtual object EditingControlFormattedValue
    {
        get => Text;
        set { var list = Values(); SelectedIndex = list.FindIndex(x => Display(x) == value?.ToString()); }
    }
    public virtual int EditingControlRowIndex { get; set; } = -1;
    public virtual bool EditingControlValueChanged { get; set; }
    public virtual Cursor EditingPanelCursor => Cursors.Default;
    public virtual bool RepositionEditingControlOnValueChange => false;
    public virtual void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle) => dataGridViewCellStyle.Apply(this);
    public virtual bool EditingControlWantsInputKey(Keys keyData,bool dataGridViewWantsInputKey) =>
        (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Home or Keys.End || !dataGridViewWantsInputKey;
    public virtual object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) => Text;
    public virtual void PrepareEditingControlForEdit(bool selectAll) {}
}
public class DataGridViewComboBoxColumn : DataGridViewColumn
{
    public DataGridViewComboBoxColumn() => Items = new DataGridViewComboBoxCell.ObjectCollection(() => Owner?.RenderGrid());
    public DataGridViewComboBoxCell.ObjectCollection Items { get; }
    private object? source;
    private string display = "", member = "";
    public object? DataSource { get => source; set { source = value; Owner?.RenderGrid(); } }
    public string DisplayMember { get => display; set { display = value ?? ""; Owner?.RenderGrid(); } }
    public string ValueMember { get => member; set { member = value ?? ""; Owner?.RenderGrid(); } }
    internal object[] Choices() => ((source is IListSource ls ? ls.GetList() : source as IEnumerable) ?? Items).Cast<object>().ToArray();
    internal object? ValueOf(object item) => member.Length == 0 ? item : TypeDescriptor.GetProperties(item).Find(member,true)?.GetValue(item);
    internal string Caption(object item) => Convert.ToString(display.Length == 0 ? item : TypeDescriptor.GetProperties(item).Find(display,true)?.GetValue(item),CultureInfo.CurrentCulture) ?? "";
    internal string Format(object? value) => Choices().FirstOrDefault(i => Equals(ValueOf(i),value)) is {} item ? Caption(item) : "";
}
public class DataGridViewTextBoxCell : DataGridViewCell
{
    internal DataGridViewTextBoxCell(DataGridViewRow r,DataGridViewColumn c,object? v) : base(r,c,v) {}
}
public class DataGridViewCheckBoxCell : DataGridViewCell
{
    internal DataGridViewCheckBoxCell(DataGridViewRow r,DataGridViewColumn c,object? v) : base(r,c,v) {}
}
public class DataGridViewComboBoxCell : DataGridViewCell
{
    internal DataGridViewComboBoxCell(DataGridViewRow r,DataGridViewColumn c,object? v) : base(r,c,v) {}
    public ObjectCollection Items => ((DataGridViewComboBoxColumn)Column).Items;
    public class ObjectCollection : Collection<object>
    {
        private readonly Action changed;
        internal ObjectCollection(Action changed) => this.changed = changed;
        public new int Add(object item) { base.Add(item); return Count-1; }
        public void AddRange(params object[] items) { foreach (var item in items) Items.Add(item); changed(); }
        protected override void InsertItem(int index,object item) { base.InsertItem(index,item); changed(); }
        protected override void RemoveItem(int index) { base.RemoveItem(index); changed(); }
        protected override void SetItem(int index,object item) { base.SetItem(index,item); changed(); }
        protected override void ClearItems() { base.ClearItems(); changed(); }
    }
}
public partial class DataGridView
{
    [DllImport("MacForms")] private static extern void mf_grid_edit_control(IntPtr grid,IntPtr peer,int row,int visibleColumn);
    private DataGridViewCellStyle defaultStyle = new() {
        BackColor = Color.White, ForeColor = Color.Black, SelectionBackColor = Color.FromArgb(0,120,215), SelectionForeColor = Color.White
    };
    public DataGridViewCellStyle DefaultCellStyle
    {
        get => defaultStyle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            defaultStyle.Changed -= RenderGrid;
            defaultStyle = value;
            defaultStyle.Changed += RenderGrid;
            RenderGrid();
        }
    }
    public Control? EditingControl { get; private set; }
    public event DataGridViewEditingControlShowingEventHandler? EditingControlShowing;
    public event EventHandler? CurrentCellDirtyStateChanged;
    public bool IsCurrentCellDirty => (EditingControl as IDataGridViewEditingControl)?.EditingControlValueChanged ?? false;
    public void NotifyCurrentCellDirty(bool dirty)
    {
        if (EditingControl is not IDataGridViewEditingControl control || control.EditingControlValueChanged == dirty) return;
        control.EditingControlValueChanged = dirty;
        CurrentCellDirtyStateChanged?.Invoke(this,EventArgs.Empty);
    }
    private DataGridViewTextBoxEditingControl? textBox;
    private DataGridViewComboBoxEditingControl? comboBox;
    private void StartEditor(DataGridViewCell cell,int visible)
    {
        if (cell.Column is DataGridViewCheckBoxColumn) return;
        Control peer;
        if (cell.Column is DataGridViewComboBoxColumn column)
        {
            comboBox ??= new DataGridViewComboBoxEditingControl();
            comboBox.DataSource = null;
            comboBox.Items.Clear();
            comboBox.DisplayMember = column.DisplayMember;
            comboBox.ValueMember = column.ValueMember;
            comboBox.Items.AddRange(column.Choices());
            comboBox.SelectedValue = cell.Value;
            peer = comboBox;
        }
        else
        {
            textBox ??= new DataGridViewTextBoxEditingControl();
            textBox.Text = Convert.ToString(Format(cell.ColumnIndex,cell.Row),CultureInfo.CurrentCulture) ?? "";
            peer = textBox;
        }
        EditingControl = peer;
        Controls.Add(peer);
        // The AppKit table owns the visible cell field. Bind this Control's handle to
        // that field for this editing session so selection, key events and text share one view.
        mf_grid_edit_control(Handle,peer.Handle,cell.RowIndex,visible);
        var editor = (IDataGridViewEditingControl)peer;
        editor.EditingControlRowIndex = cell.RowIndex;
        editor.EditingControlValueChanged = false;
        editor.EditingControlDataGridView = this;
        var showing = new DataGridViewEditingControlShowingEventArgs(peer,cell.Column.Style ?? DefaultCellStyle);
        EditingControlShowing?.Invoke(this,showing);
        editor.ApplyCellStyleToEditingControl(showing.CellStyle);
    }
    private void FinishEditor()
    {
        if (EditingControl is not IDataGridViewEditingControl editor) return;
        mf_grid_edit_control(NativeHandle,0,0,0);
        editor.EditingControlDataGridView = null;
        editor.EditingControlRowIndex = -1;
        editor.EditingControlValueChanged = false;
        var peer = EditingControl;
        EditingControl = null;
        Controls.Remove(peer);
    }
    public bool CommitEdit(DataGridViewDataErrorContexts context)
    {
        if (editingCell == null || EditingControl is not IDataGridViewEditingControl editor) return false;
        try
        {
            object? newValue = EditingControl is DataGridViewComboBoxEditingControl combo ? combo.SelectedValue :
                ParseValue(editingCell,Convert.ToString(editor.GetEditingControlFormattedValue(context),CultureInfo.CurrentCulture) ?? "");
            SetValue(editingCell,newValue);
            NotifyCurrentCellDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            var e = new DataGridViewDataErrorEventArgs(ex,editingCell.ColumnIndex,editingCell.RowIndex,context);
            DataError?.Invoke(this,e);
            if (e.ThrowException) throw;
            return false;
        }
    }
}
