using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public enum DataGridViewSelectionMode { CellSelect, FullRowSelect, FullColumnSelect, RowHeaderSelect, ColumnHeaderSelect }
public enum DataGridViewAutoSizeColumnsMode { None = 1, ColumnHeader = 2, AllCellsExceptHeader = 4, AllCells = 6, DisplayedCellsExceptHeader = 8, DisplayedCells = 10, Fill = 16 }
public enum DataGridViewAutoSizeColumnMode { NotSet, None = 1, ColumnHeader = 2, AllCellsExceptHeader = 4, AllCells = 6, DisplayedCellsExceptHeader = 8, DisplayedCells = 10, Fill = 16 }
public enum DataGridViewColumnHeadersHeightSizeMode { EnableResizing, DisableResizing, AutoSize }

public class DataGridViewColumn
{
    private DataGridView? owner;
    internal DataGridView? Owner
    {
        get => owner;
        set
        {
            if (owner == value) return;
            if (owner != null && Style != null) Style.Changed -= UpdateAppearance;
            owner = value;
            if (owner != null && Style != null) Style.Changed += UpdateAppearance;
        }
    }
    internal bool Generated;
    string header = "";
    bool visible = true;
    float fillWeight = 100;
    int width = 100;
    DataGridViewAutoSizeColumnMode autoSizeMode;
    public DataGridViewAutoSizeColumnMode AutoSizeMode
    {
        get => autoSizeMode;
        set { autoSizeMode = value; Owner?.RenderGrid(); }
    }
    public int Width
    {
        get => width;
        set { width = Math.Clamp(value, 2, ushort.MaxValue); Owner?.RenderGrid(); }
    }
    public string Name { get; set; } = "";
    public string DataPropertyName { get; set; } = "";
    public string HeaderText { get => header; set { header = value; Owner?.RenderGrid(); } }
    public bool Visible { get => visible; set { visible = value; Owner?.RenderGrid(); } }
    public float FillWeight
    {
        get => fillWeight;
        set
        {
            if (!(value > 0 && value <= ushort.MaxValue)) throw new ArgumentOutOfRangeException(nameof(value));
            fillWeight = value;
            Owner?.RenderGrid();
        }
    }
    bool readOnly;
    public bool ReadOnly { get => readOnly; set { readOnly = value; Owner?.RenderGrid(); } }
    public Type? ValueType { get; set; }
    internal DataGridViewCellStyle? Style;
    private void UpdateAppearance() => Owner?.RenderGrid();
    public DataGridViewCellStyle DefaultCellStyle
    {
        get
        {
            if (Style == null)
            {
                Style = new();
                if (Owner != null) Style.Changed += UpdateAppearance;
            }
            return Style;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (Owner != null && Style != null) Style.Changed -= UpdateAppearance;
            Style = value;
            if (Owner != null) Style.Changed += UpdateAppearance;
            UpdateAppearance();
        }
    }
    internal PropertyDescriptor? Property { get; set; }
}
public class DataGridViewTextBoxColumn : DataGridViewColumn { }
public class DataGridViewCheckBoxColumn : DataGridViewColumn { }

public sealed class DataGridViewColumnCollection : IReadOnlyList<DataGridViewColumn>
{
    readonly DataGridView owner;
    internal readonly List<DataGridViewColumn> Items = new();
    internal DataGridViewColumnCollection(DataGridView grid) => owner = grid;
    public int Count => Items.Count;
    public DataGridViewColumn this[int index] => Items[index];
    public DataGridViewColumn this[string name] => Items.First(c => Matches(c, name));
    static bool Matches(DataGridViewColumn column, string name) => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase);
    public bool Contains(string name) => Items.Any(c => Matches(c, name));
    public IEnumerator<DataGridViewColumn> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void AddRange(params DataGridViewColumn[] columns)
    {
        if (columns.Any(c => c == null || c.Owner != null) || columns.Distinct().Count() != columns.Length)
            throw new InvalidOperationException("Each column must be unique and belong to no other DataGridView.");
        foreach (var column in columns)
        {
            Items.Add(column);
            column.Owner = owner;
        }
        owner.ColumnsChanged();
    }
    public int Add(DataGridViewColumn column) { AddRange(column); return Count - 1; }
    public int Add(string columnName, string headerText) => Add(new DataGridViewTextBoxColumn { Name = columnName, HeaderText = headerText });
}

public class DataGridViewCell
{
    internal readonly DataGridViewRow Row;
    internal readonly DataGridViewColumn Column;
    object? value;
    internal DataGridViewCell(DataGridViewRow row, DataGridViewColumn column, object? value)
    {
        Row = row; Column = column; this.value = value;
    }
    public bool Visible => Column.Visible;
    internal void CacheValue(object? item) => value = item;
    public object? Value { get => value; set => Row.Grid.SetValue(this, value); }
    public Type? ValueType => Column.Property?.PropertyType ?? Column.ValueType;
    public bool ReadOnly => Row.Grid.ReadOnly || Column.ReadOnly || Column.Property?.IsReadOnly == true;
    public int ColumnIndex => Row.Grid.Columns.Items.IndexOf(Column);
    public int RowIndex => Row.Index;
}

public sealed class DataGridViewCellCollection : IReadOnlyList<DataGridViewCell>
{
    readonly DataGridViewRow row;
    readonly List<DataGridViewCell> cells = new();
    internal DataGridViewCellCollection(DataGridViewRow row) => this.row = row;
    internal void Append(DataGridViewColumn column, object? value) => cells.Add(column switch {
        DataGridViewCheckBoxColumn => new DataGridViewCheckBoxCell(row,column,value),
        DataGridViewComboBoxColumn => new DataGridViewComboBoxCell(row,column,value),
        _ => new DataGridViewTextBoxCell(row,column,value)
    });
    public DataGridViewCell this[int index] => cells[index];
    public DataGridViewCell this[string name] => cells[row.Grid.Columns.Items.IndexOf(row.Grid.Columns[name])];
    public int Count => cells.Count;
    public IEnumerator<DataGridViewCell> GetEnumerator() => cells.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DataGridViewRow
{
    internal readonly DataGridView Grid;
    internal DataGridViewRow(DataGridView grid, object? item, int index)
    {
        Grid = grid; DataBoundItem = item; Index = index; Cells = new(this);
    }
    public object? DataBoundItem { get; internal set; }
    public bool IsNewRow { get; internal set; }
    public int Index { get; internal set; }
    public DataGridViewCellCollection Cells { get; }
    public bool Selected
    {
        get => Grid.SelectedRow == this && Grid.EntireRowSelected;
        set
        {
            if (value) Grid.SelectRow(this,true);
            else if (Selected) Grid.ClearSelection();
        }
    }
}
public sealed class DataGridViewRowCollection : IReadOnlyList<DataGridViewRow>
{
    readonly DataGridView grid;
    internal readonly List<DataGridViewRow> Items = new();
    internal DataGridViewRowCollection(DataGridView grid) => this.grid = grid;
    public int Count => Items.Count;
    public DataGridViewRow this[int index] => Items[index];
    public IEnumerator<DataGridViewRow> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int Add(params object?[] values)
    {
        if (grid.DataSource != null) throw new InvalidOperationException("Cannot manually add rows when DataSource is set.");
        if (grid.Columns.Count == 0) throw new InvalidOperationException("Add columns first.");
        if (values.Length > grid.Columns.Count) throw new ArgumentException("Too many values for this grid.");
        int at = grid.NewRowIndex >= 0 ? grid.NewRowIndex : Count;
        var row = new DataGridViewRow(grid, null, at);
        foreach (var column in grid.Columns) row.Cells.Append(column, row.Cells.Count < values.Length ? values[row.Cells.Count] : null);
        Items.Insert(at,row);
        if (Count > at+1) Items[^1].Index = Count-1;
        grid.SyncNewRow();
        grid.RenderGrid();
        if (at == 0) grid.SelectRow(row);
        return row.Index;
    }
    public void Remove(DataGridViewRow row)
    {
        int index = Items.IndexOf(row);
        if (index < 0) throw new ArgumentException("The row must belong to this grid.",nameof(row));
        RemoveAt(index);
    }
    public void RemoveAt(int index)
    {
        if (Items[index].IsNewRow) throw new InvalidOperationException("The placeholder row cannot be removed.");
        if (grid.DataSource != null)
        {
            grid.RemoveBoundRow(index);
            return;
        }
        var deleted = Items[index];
        Items.RemoveAt(index);
        deleted.Index = -1;
        for (int i = index; i < Count; i++) Items[i].Index = i;
        if (grid.CurrentRow == deleted) grid.CurrentCell = Items.ElementAtOrDefault(Math.Min(index,Count-1))?.Cells.FirstOrDefault(c => c.Visible);
        grid.RenderGrid();
    }
    public void Clear()
    {
        if (grid.DataSource != null) throw new InvalidOperationException("Cannot manually clear bound rows.");
        Items.Clear();
        grid.CurrentCell = null;
        grid.SyncNewRow();
        grid.RenderGrid();
    }
}

public delegate void DataGridViewCellFormattingEventHandler(object? sender, DataGridViewCellFormattingEventArgs e);
public class DataGridViewCellFormattingEventArgs : EventArgs
{
    internal DataGridViewCellFormattingEventArgs(int column, int row, object? value)
    {
        ColumnIndex = column; RowIndex = row; Value = value;
    }
    public int ColumnIndex { get; }
    public int RowIndex { get; }
    public object? Value { get; set; }
    public bool FormattingApplied { get; set; }
}

public class DataGridViewRowCancelEventArgs(DataGridViewRow row) : CancelEventArgs { public DataGridViewRow Row { get; } = row; }
public class DataGridViewRowEventArgs(DataGridViewRow row) : EventArgs { public DataGridViewRow Row { get; } = row; }
public delegate void DataGridViewRowCancelEventHandler(object? sender, DataGridViewRowCancelEventArgs e);
public delegate void DataGridViewRowEventHandler(object? sender, DataGridViewRowEventArgs e);

public class DataGridViewCellEventArgs(int columnIndex,int rowIndex) : EventArgs
{
    public int ColumnIndex { get; } = columnIndex;
    public int RowIndex { get; } = rowIndex;
}
public class DataGridViewCellCancelEventArgs(int columnIndex,int rowIndex) : CancelEventArgs
{
    public int ColumnIndex { get; } = columnIndex;
    public int RowIndex { get; } = rowIndex;
}
public class DataGridViewCellValidatingEventArgs : CancelEventArgs
{
    internal DataGridViewCellValidatingEventArgs(int col,int row,object? value)
    { ColumnIndex = col; RowIndex = row; FormattedValue = value; }
    public int ColumnIndex { get; }
    public int RowIndex { get; }
    public object? FormattedValue { get; }
}
[Flags] public enum DataGridViewDataErrorContexts { Formatting=1, Display=2, PreferredSize=4, RowDeletion=8, Parsing=256, Commit=512, InitialValueRestoration=1024, LeaveControl=2048, CurrentCellChange=4096, Scroll=8192, ClipboardContent=16384 }
public class DataGridViewDataErrorEventArgs(Exception exception,int column,int row,DataGridViewDataErrorContexts context) : DataGridViewCellCancelEventArgs(column,row)
{
    public Exception Exception { get; } = exception;
    public DataGridViewDataErrorContexts Context { get; } = context;
    public bool ThrowException { get; set; }
}
public delegate void DataGridViewCellEventHandler(object? sender,DataGridViewCellEventArgs e);
public delegate void DataGridViewCellCancelEventHandler(object? sender,DataGridViewCellCancelEventArgs e);
public delegate void DataGridViewCellValidatingEventHandler(object? sender,DataGridViewCellValidatingEventArgs e);
public delegate void DataGridViewDataErrorEventHandler(object? sender,DataGridViewDataErrorEventArgs e);

public partial class DataGridView : Control, ISupportInitialize
{
    internal override int Kind => 17;
    object? dataSource;
    IEnumerable? sourceList;
    DataGridViewCell? current;
    internal DataGridViewRow? SelectedRow { get; private set; }
    public DataGridViewColumnCollection Columns { get; }
    public DataGridViewRowCollection Rows { get; }
    public IReadOnlyList<DataGridViewRow> SelectedRows => SelectedRow != null && EntireRowSelected &&
        SelectionMode is DataGridViewSelectionMode.FullRowSelect or DataGridViewSelectionMode.RowHeaderSelect
        ? new[] { SelectedRow } : Array.Empty<DataGridViewRow>();
    private delegate int NativeEditCallback(int phase,int row,int visibleColumn,IntPtr text);
    [DllImport("MacForms")] private static extern void mf_grid_set_editor(IntPtr grid,NativeEditCallback callback);
    [DllImport("MacForms")] private static extern int mf_grid_begin_edit(IntPtr grid,int row,int column,int selectAll);
    [DllImport("MacForms")] private static extern int mf_grid_end_edit(IntPtr grid,int cancel);
    private readonly NativeEditCallback editCallback;
    private DataGridViewCell? editingCell;
    private bool writingValue;
    public DataGridView()
    {
        editCallback = NativeEdit;
        defaultStyle.Changed += RenderGrid;
        Columns = new(this);
        Rows = new(this);
    }

    bool readOnly;
    public bool ReadOnly
    {
        get => readOnly;
        set { if (value && editingCell != null) CancelEdit(); readOnly = value; RenderGrid(); }
    }
    public event DataGridViewRowCancelEventHandler? UserDeletingRow;
    public event DataGridViewRowEventHandler? UserDeletedRow;
    public event DataGridViewCellCancelEventHandler? CellBeginEdit;
    public event DataGridViewCellEventHandler? CellEndEdit, CellValueChanged, CellValidated;
    public event DataGridViewCellValidatingEventHandler? CellValidating;
    public event DataGridViewDataErrorEventHandler? DataError;
    public bool IsCurrentCellInEditMode => editingCell != null;
    public bool BeginEdit(bool selectAll)
    {
        if (current == null || current.ReadOnly) return false;
        int visibleColumn = Columns.Where(c => c.Visible).ToList().IndexOf(current.Column);
        return mf_grid_begin_edit(Handle,current.RowIndex,visibleColumn,selectAll ? 1 : 0) != 0;
    }
    public bool EndEdit() => editingCell == null || mf_grid_end_edit(NativeHandle,0) != 0;
    public bool CancelEdit() => editingCell == null || mf_grid_end_edit(NativeHandle,1) != 0;

    private int NativeEdit(int phase,int row,int visibleColumn,IntPtr text)
    {
        if (row < 0 || row >= Rows.Count || IsDisposed || Disposing) return 0;
        if (phase == 4)
        {
            if (!AllowUserToDeleteRows || Rows[row].IsNewRow || (sourceList is IBindingList b && !b.AllowRemove)
                || sourceList is IList { IsFixedSize:true } or IList { IsReadOnly:true }) return 0;
            var removing = new DataGridViewRowCancelEventArgs(Rows[row]);
            UserDeletingRow?.Invoke(this,removing);
            if (removing.Cancel) return 1;
            Rows.RemoveAt(row);
            UserDeletedRow?.Invoke(this,new(removing.Row));
            return 1;
        }
        var col = Columns.Where(c => c.Visible).ElementAtOrDefault(visibleColumn);
        if (col == null) return 0;
        var cell = Rows[row].Cells[Columns.Items.IndexOf(col)];
        if (phase == 5)
        {
            CurrentCell = cell;
            return current == cell ? 1 : 0;
        }
        var address = new DataGridViewCellEventArgs(cell.ColumnIndex,row);
        if (phase == 0)
        {
            if (cell.ReadOnly || !Enabled) return 0;
            if (editingCell == cell) return 1;
            var start = new DataGridViewCellCancelEventArgs(cell.ColumnIndex,row);
            CellBeginEdit?.Invoke(this,start);
            if (start.Cancel) return 0;
            CurrentCell = cell;
            editingCell = cell;
            StartEditor(cell,visibleColumn);
            return 1;
        }
        if (editingCell != cell) return 0;
        if (phase == 1)
        {
            string formatted = Marshal.PtrToStringUTF8(text) ?? "";
            var validating = new DataGridViewCellValidatingEventArgs(cell.ColumnIndex,row,formatted);
            CellValidating?.Invoke(this,validating);
            if (validating.Cancel) return 0;
            try { SetValue(cell, cell.Column is DataGridViewComboBoxColumn && EditingControl is ComboBox box
                ? box.SelectedValue : ParseValue(cell,formatted)); }
            catch (Exception error)
            {
                var args = new DataGridViewDataErrorEventArgs(error,cell.ColumnIndex,row,
                    DataGridViewDataErrorContexts.Parsing | DataGridViewDataErrorContexts.Commit);
                DataError?.Invoke(this,args);
                if (args.ThrowException) throw;
                return 0;
            }
            CellValidated?.Invoke(this,address);
            return 1;
        }
        FinishEditor();
        editingCell = null;
        CellEndEdit?.Invoke(this,address);
        RenderGrid();
        return 1;
    }

    static object? ParseValue(DataGridViewCell cell,string formatted)
    {
        Type? destination = cell.ValueType ?? cell.Value?.GetType();
        if (destination == null || destination == typeof(string) || destination == typeof(object)) return formatted;
        var nullable = Nullable.GetUnderlyingType(destination);
        if (formatted.Length == 0 && (nullable != null || !destination.IsValueType)) return null;
        var converter = TypeDescriptor.GetConverter(nullable ?? destination);
        return converter.ConvertFrom(null,CultureInfo.CurrentCulture,formatted);
    }
    internal void SetValue(DataGridViewCell cell,object? value)
    {
        if (Equals(cell.Value,value)) return;
        if (cell.Row.IsNewRow) MaterializeNewRow(cell.Row);
        var model = cell.Row.DataBoundItem;
        if (model != null && cell.Column.Property is { } property)
        {
            if (property.IsReadOnly) throw new InvalidOperationException($"'{property.Name}' is read-only.");
            writingValue = true;
            try
            {
                property.SetValue(model,value);
                foreach (var sibling in cell.Row.Cells)
                    if (sibling.Column.Property is {} bound) sibling.CacheValue(bound.GetValue(model));
                value = property.GetValue(model);
            }
            finally { writingValue = false; }
        }
        cell.CacheValue(value);
        CellValueChanged?.Invoke(this,new DataGridViewCellEventArgs(cell.ColumnIndex,cell.RowIndex));
        RenderGrid();
    }
    internal void RemoveBoundRow(int index)
    {
        if (sourceList is not IList list || list.IsReadOnly || list.IsFixedSize)
            throw new InvalidOperationException("The bound list does not support removing a row.");
        list.RemoveAt(index);
        if (list is not IBindingList { SupportsChangeNotification: true }) Bind();
    }
    private bool allowAdd = true;
    public bool AllowUserToAddRows
    {
        get => allowAdd;
        set { if (value == allowAdd) return; allowAdd = value; SyncNewRow(); RenderGrid(); }
    }
    public int NewRowIndex => Rows.Count != 0 && Rows[Rows.Count-1].IsNewRow ? Rows.Count-1 : -1;
    public DataGridViewCell this[int columnIndex,int rowIndex] => Rows[rowIndex].Cells[columnIndex];
    public DataGridViewCell this[string columnName,int rowIndex] => Rows[rowIndex].Cells[columnName];
    internal void SyncNewRow()
    {
        bool required = allowAdd && Columns.Count > 0 && (sourceList == null || sourceList is IBindingList { AllowNew:true });
        bool hasRow = NewRowIndex >= 0;
        if (required == hasRow) return;
        if (required)
        {
            var row = new DataGridViewRow(this,null,Rows.Count) { IsNewRow = true };
            foreach (var column in Columns) row.Cells.Append(column,null);
            Rows.Items.Add(row);
        }
        else
        {
            var last = Rows.Items[^1];
            Rows.Items.RemoveAt(Rows.Count-1);
            if (SelectedRow == last || CurrentRow == last)
                SetSelection(Rows.LastOrDefault(),Rows.LastOrDefault()?.Cells.FirstOrDefault(c => c.Visible));
        }
    }
    void MaterializeNewRow(DataGridViewRow row)
    {
        if (sourceList is IBindingList binding)
        {
            writingValue = true;
            try
            {
                row.DataBoundItem = binding.AddNew() ?? throw new InvalidOperationException("The data source could not create an item.");
                foreach (var cell in row.Cells)
                    if (cell.Column.Property is {} prop) cell.CacheValue(prop.GetValue(row.DataBoundItem));
            }
            finally { writingValue = false; }
        }
        row.IsNewRow = false;
        SyncNewRow();
    }
    public bool AllowUserToDeleteRows { get; set; } = true;
    public bool AllowUserToResizeRows { get; set; } = true;
    public bool MultiSelect { get; set; } = true;
    public bool RowHeadersVisible { get; set; } = true;
    public bool AutoGenerateColumns { get; set; } = true;
    int headerHeight = 23;
    DataGridViewColumnHeadersHeightSizeMode headerHeightMode;
    public int ColumnHeadersHeight
    {
        get => headerHeight;
        set { headerHeight = Math.Clamp(value, 4, 32768); ApplyHeaderHeight(); }
    }
    public DataGridViewColumnHeadersHeightSizeMode ColumnHeadersHeightSizeMode
    {
        get => headerHeightMode;
        set { headerHeightMode = value; ApplyHeaderHeight(); }
    }
    void ApplyHeaderHeight()
    {
        if (NativeHandle != IntPtr.Zero)
            Native.mf_grid_header_height(NativeHandle, headerHeightMode == DataGridViewColumnHeadersHeightSizeMode.AutoSize ? 0 : headerHeight);
    }
    private DataGridViewSelectionMode selectionMode = DataGridViewSelectionMode.RowHeaderSelect;
    private bool wholeRow;
    internal bool EntireRowSelected => wholeRow || SelectionMode == DataGridViewSelectionMode.FullRowSelect;
    public DataGridViewSelectionMode SelectionMode
    {
        get => selectionMode;
        set { selectionMode = value; wholeRow = false; SyncSelection(); }
    }
    DataGridViewAutoSizeColumnsMode autoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
    public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode
    {
        get => autoSizeColumnsMode;
        set { autoSizeColumnsMode = value; RenderGrid(); }
    }
    private Color background = Color.FromArgb(160,160,160), lines = Color.FromArgb(100,100,100);
    public Color BackgroundColor { get => background; set { background = value; ApplyPalette(); } }
    public Color GridColor { get => lines; set { lines = value; ApplyPalette(); } }
    private void ApplyPalette()
    {
        if (NativeHandle != IntPtr.Zero) mf_grid_palette(NativeHandle,background.ToArgb(),lines.ToArgb());
    }
    [DllImport("MacForms")] private static extern void mf_grid_palette(IntPtr h,int background,int border);
    [DllImport("MacForms")] private static extern void mf_grid_selection(IntPtr h,int row,int column,int mode);
    public event EventHandler? SelectionChanged;
    public event DataGridViewCellFormattingEventHandler? CellFormatting;
    public void BeginInit() => initializing = true;
    public void EndInit() { initializing = false; RenderGrid(); }
    bool initializing;
    bool rendering;
    string? displayedColumns;

    public object? DataSource
    {
        get => dataSource;
        set
        {
            var list = value is IListSource source ? source.GetList() : value as IEnumerable;
            if (value != null && list == null) throw new ArgumentException("The grid needs an enumerable data source.");
            if (editingCell != null) CancelEdit();
            if (sourceList is IBindingList oldBinding) oldBinding.ListChanged -= ListChanged;
            if (dataSource is BindingSource oldSource) oldSource.PositionChanged -= SourcePositionChanged;
            dataSource = value;
            sourceList = list;
            if (sourceList is IBindingList binding) binding.ListChanged += ListChanged;
            if (dataSource is BindingSource bindingSource) bindingSource.PositionChanged += SourcePositionChanged;
            Bind();
        }
    }
    void SourcePositionChanged(object? sender,EventArgs e)
    {
        if (!writingValue && dataSource is BindingSource source && source.Position >= 0 && source.Position < Rows.Count &&
            source.Position != SelectedRow?.Index)
            SelectRow(Rows[source.Position]);
    }
    void ListChanged(object? sender,ListChangedEventArgs e)
    {
        if (writingValue) return;
        if (e.ListChangedType == ListChangedType.ItemChanged && e.NewIndex >= 0 && e.NewIndex < Rows.Count
            && sourceList is IList items && ReferenceEquals(items[e.NewIndex],Rows[e.NewIndex].DataBoundItem))
        {
            var row = Rows[e.NewIndex];
            foreach (var cell in row.Cells)
                if (cell.Column.Property is {} prop) cell.CacheValue(prop.GetValue(row.DataBoundItem));
            RenderGrid();
            return;
        }
        if (editingCell != null) CancelEdit();
        Bind();
    }
    internal void ColumnsChanged()
    {
        if (editingCell != null) CancelEdit();
        if (sourceList != null) { Bind(); return; }
        foreach (var row in Rows)
            while (row.Cells.Count < Columns.Count) row.Cells.Append(Columns[row.Cells.Count], null);
        SyncNewRow();
        RenderGrid();
    }

    void Bind()
    {
        var oldItem = current?.Row.DataBoundItem;
        var items = sourceList?.Cast<object?>().ToList() ?? new List<object?>();
        var sample = items.FirstOrDefault(item => item != null);
        var elementType = sourceList?.GetType().GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))?.GetGenericArguments()[0];
        var properties = sourceList is ITypedList typed ? typed.GetItemProperties(null)
            : sample != null ? TypeDescriptor.GetProperties(sample)
            : elementType != null ? TypeDescriptor.GetProperties(elementType)
            : new PropertyDescriptorCollection(null);

        foreach (var old in Columns.Items.Where(c => c.Generated && (!AutoGenerateColumns || properties.Find(c.DataPropertyName,true) == null)).ToArray())
        {
            old.Owner = null;
            Columns.Items.Remove(old);
        }
        if (AutoGenerateColumns)
        {
            foreach (PropertyDescriptor property in properties)
            {
                if (!property.IsBrowsable || Columns.Any(c => string.Equals(c.DataPropertyName, property.Name, StringComparison.OrdinalIgnoreCase))) continue;
                DataGridViewColumn column = property.PropertyType == typeof(bool) ? new DataGridViewCheckBoxColumn() : new DataGridViewTextBoxColumn();
                column.Generated = true;
                column.Name = column.DataPropertyName = property.Name;
                column.HeaderText = property.DisplayName;
                column.Owner = this;
                Columns.Items.Add(column);
            }
        }

        var getters = Columns.Select(c => properties.Find(c.DataPropertyName, true)).ToArray();
        for (int i = 0; i < getters.Length; i++) Columns[i].Property = getters[i];
        Rows.Items.Clear();
        foreach (var item in items)
        {
            var row = new DataGridViewRow(this, item, Rows.Count);
            for (int i = 0; i < Columns.Count; i++)
                row.Cells.Append(Columns[i], item == null ? null : getters[i]?.GetValue(item));
            Rows.Items.Add(row);
        }
        SyncNewRow();
        var selected = oldItem == null ? Rows.FirstOrDefault()
            : Rows.FirstOrDefault(row => ReferenceEquals(row.DataBoundItem, oldItem)) ?? Rows.FirstOrDefault();
        if (dataSource is BindingSource source && source.Position >= 0 && source.Position < Rows.Count) selected = Rows[source.Position];
        int oldColumn = current?.ColumnIndex ?? -1;
        SelectedRow = selected;
        current = selected?.Cells.FirstOrDefault(c => c.Visible && c.ColumnIndex == oldColumn) ?? selected?.Cells.FirstOrDefault(c => c.Visible);
        RenderGrid();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public DataGridViewRow? CurrentRow => current?.Row;
    public DataGridViewCell? CurrentCell
    {
        get => current;
        set
        {
            if (value != null && (!Rows.Items.Contains(value.Row) || !value.Visible)) throw new ArgumentException("Cell must be visible and belong to the grid.");
            SetSelection(value?.Row, value);
        }
    }
    internal void SelectRow(DataGridViewRow row,bool entire = false)
    {
        if (!Rows.Items.Contains(row)) return;
        var cell = row.Cells.FirstOrDefault(c => c.Visible && c.Column == current?.Column) ?? row.Cells.FirstOrDefault(c => c.Visible);
        SetSelection(row,cell,entire);
    }
    private void SyncSelection()
    {
        if (NativeHandle == 0) return;
        int column = current == null ? -1 : Columns.Where(c=>c.Visible).ToList().IndexOf(current.Column);
        int mode = selectionMode == DataGridViewSelectionMode.FullColumnSelect ? 2 :
            EntireRowSelected ? 1 : 0;
        mf_grid_selection(NativeHandle,SelectedRow?.Index ?? -1,column,mode);
    }
    public void ClearSelection() => SetSelection(null, current);
    void SetSelection(DataGridViewRow? row, DataGridViewCell? cell,bool entire = false)
    {
        if (row == SelectedRow && cell == current && entire == wholeRow) return;
        if (editingCell != null && cell != editingCell && !EndEdit()) return;
        SelectedRow = row;
        wholeRow = entire;
        current = cell;
        if (row != null && !row.IsNewRow && dataSource is BindingSource source && source.Position != row.Index) source.Position = row.Index;
        SyncSelection();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    internal override void ApplyNative() { base.ApplyNative(); mf_grid_set_editor(NativeHandle,editCallback); ApplyHeaderHeight(); ApplyPalette(); RenderGrid(); }
    internal override void OnNative(int ev)
    {
        if (ev == 4 && !rendering)
        {
            int index = Native.mf_get_selected(NativeHandle);
            if (index >= 0 && index < Rows.Count) SelectRow(Rows[index]);
            else ClearSelection();
        }
        base.OnNative(ev);
    }

    internal void RenderGrid()
    {
        if (NativeHandle == IntPtr.Zero || initializing || rendering || editingCell != null) return;
        rendering = true;
        try
        {
            var visible = Columns.Select((c, i) => (column: c, index: i)).Where(c => c.column.Visible).ToArray();
            var displayed = Rows.Select(row => visible.Select(c => Format(c.index, row)).ToArray()).ToArray();
            var columns = JsonSerializer.Serialize(visible.Select((c, i) => new {
                title = c.column.HeaderText,
                weight = c.column.FillWeight,
                back = ResolveColor(c.column.Style?.BackColor,defaultStyle.BackColor,Color.White),
                fore = ResolveColor(c.column.Style?.ForeColor,defaultStyle.ForeColor,Color.Black),
                selectedBack = ResolveColor(c.column.Style?.SelectionBackColor,defaultStyle.SelectionBackColor,Color.FromArgb(0,120,215)),
                selectedFore = ResolveColor(c.column.Style?.SelectionForeColor,defaultStyle.SelectionForeColor,Color.White),
                font = (c.column.Style?.Font ?? defaultStyle.Font)?.Name,
                size = (c.column.Style?.Font ?? defaultStyle.Font)?.SizeInPoints,
                bold = (c.column.Style?.Font ?? defaultStyle.Font)?.Bold,
                italic = (c.column.Style?.Font ?? defaultStyle.Font)?.Italic,
                width = ColumnWidth(c.column, displayed, i),
                editable = !ReadOnly && !c.column.ReadOnly && c.column.Property?.IsReadOnly != true
                    && sourceList is not IBindingList { AllowEdit:false },
                check = c.column is DataGridViewCheckBoxColumn,
                choices = c.column is DataGridViewComboBoxColumn combo ? combo.Choices().Select(combo.Caption).ToArray() : null
            }));
            if (columns != displayedColumns)
            {
                Native.mf_grid_columns(NativeHandle, columns);
                displayedColumns = columns;
            }
            Native.mf_grid_rows(NativeHandle, JsonSerializer.Serialize(displayed));
            SyncSelection();
        }
        finally { rendering = false; }
    }
    static int ResolveColor(Color? column,Color grid,Color fallback) =>
        column is { IsEmpty:false } color ? color.ToArgb() : (grid.IsEmpty ? fallback : grid).ToArgb();

    int ColumnWidth(DataGridViewColumn column, object[][] rows, int visibleIndex)
    {
        var mode = column.AutoSizeMode == DataGridViewAutoSizeColumnMode.NotSet
            ? (DataGridViewAutoSizeColumnMode)AutoSizeColumnsMode : column.AutoSizeMode;
        if (mode == DataGridViewAutoSizeColumnMode.Fill) return 0;
        if (mode == DataGridViewAutoSizeColumnMode.None) return column.Width;
        bool includeHeader = mode is not (DataGridViewAutoSizeColumnMode.AllCellsExceptHeader or DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader);
        int characters = includeHeader ? column.HeaderText.Length : 0;
        if (mode != DataGridViewAutoSizeColumnMode.ColumnHeader)
            foreach (var row in rows) characters = Math.Max(characters, row[visibleIndex] is string s ? s.Length : 2);
        return Math.Clamp((int)Math.Ceiling(characters * Font.Size * .85) + 16, 24, ushort.MaxValue);
    }
    object Format(int column, DataGridViewRow row)
    {
        var args = new DataGridViewCellFormattingEventArgs(column, row.Index, row.Cells[column].Value);
        CellFormatting?.Invoke(this, args);
        if (Columns[column] is DataGridViewCheckBoxColumn && args.Value is bool check) return check;
        if (args.FormattingApplied) return Convert.ToString(args.Value,CultureInfo.CurrentCulture) ?? "";
        if (Columns[column] is DataGridViewComboBoxColumn combo) return combo.Format(args.Value);
        var style = Columns[column].Style ?? DefaultCellStyle;
        if (args.Value == null) return style.NullValue?.ToString() ?? "";
        return args.Value is IFormattable f ? f.ToString(style.Format,style.FormatProvider) ?? "" :
            Convert.ToString(args.Value,style.FormatProvider) ?? "";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (EditingControl != null) FinishEditor();
            textBox?.Dispose();
            comboBox?.Dispose();
            defaultStyle.Changed -= RenderGrid;
            foreach (var column in Columns) column.Owner = null;
        }
        if (disposing && sourceList is IBindingList binding) binding.ListChanged -= ListChanged;
        if (disposing && dataSource is BindingSource source) source.PositionChanged -= SourcePositionChanged;
        base.Dispose(disposing);
    }
}
