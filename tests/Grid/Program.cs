using System.ComponentModel;

namespace Grid;

internal static class Program
{
    private sealed class Album : INotifyPropertyChanged
    {
        public string Title { get; set; } = "Untitled";
        public int Year { get; set; }
        public bool Available { get; set; }
        public int Category { get; set; } = 10;
        public string Summary => $"{Title} ({Year})";
        public event PropertyChangedEventHandler? PropertyChanged;
        public void Notify(string property) => PropertyChanged?.Invoke(this,new(property));
    }
    private sealed record Category(int Id,string Name);

    [STAThread]
    static int Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "DataGridView binding and editing", ClientSize = new(500,250) };
        var source = new BindingSource();
        var items = new BindingList<Album> { new() { Title = "First" }, new() { Title = "Second" } };
        source.DataSource = items;
        var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, DataSource = source };
        form.Controls.Add(grid);
        var title = new TextBox { Dock = DockStyle.Bottom };
        var year = new TextBox { Dock = DockStyle.Bottom };
        var available = new CheckBox { Dock = DockStyle.Bottom };
        form.Controls.AddRange(new Control[] { title,year,available });
        title.DataBindings.Add("Text",source,"Title",true, DataSourceUpdateMode.OnPropertyChanged);
        var yearBinding = year.DataBindings.Add("Text",source,"Year",true,DataSourceUpdateMode.OnValidation);
        available.DataBindings.Add("Checked",source,"Available",true,DataSourceUpdateMode.OnPropertyChanged);
        Exception? failure = null;
        form.Shown += async (_,_) =>
        {
            try
            {
                await Task.Delay(100);
                Check(grid.Rows.Count == 2 && title.Text == "First","row and control binding");
                Check(grid.SelectionMode == DataGridViewSelectionMode.RowHeaderSelect &&
                    !grid.Rows[0].Selected && grid.SelectedRows.Count == 0,"default selection highlights only the current cell");
                grid.Rows[1].Selected = true;
                Check(grid.Rows[1].Selected && grid.SelectedRows.Contains(grid.Rows[1]),"explicit row selection");
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.CurrentCell = grid.Rows[0].Cells["Title"];
                Check(grid.Rows[0].Selected && grid.SelectedRows.Contains(grid.Rows[0]),"full-row selection follows the current cell");
                grid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
                if (OperatingSystem.IsMacOS())
                {
                    grid.ReadOnly = true;
                    var selectionColor = System.Drawing.Color.FromArgb(90,40,60);
                    grid.DefaultCellStyle.SelectionForeColor = selectionColor;
                    Check(NativeInput.CellTextColor(0,0).ToArgb() == selectionColor.ToArgb() &&
                        NativeInput.CellTextColor(0,1).ToArgb() == System.Drawing.Color.Black.ToArgb(),"native text honors current-cell style");
                    NativeInput.MoveRight();
                    Check(grid.CurrentCell == grid.Rows[0].Cells["Year"] && !grid.Rows[0].Selected &&
                        NativeInput.CellTextColor(0,1).ToArgb() == selectionColor.ToArgb(),"native arrow selects and recolors the adjacent read-only cell");
                    Check(NativeInput.AccessibilitySelectionCount("accessibilitySelectedCells") == 1 &&
                        NativeInput.AccessibilitySelectionCount("accessibilitySelectedRows") == 0,"AppKit accessibility reports current-cell selection");
                    int formatted = 0;
                    DataGridViewCellFormattingEventHandler countFormatting = (_,_) => formatted++;
                    grid.CellFormatting += countFormatting;
                    grid.GridColor = System.Drawing.Color.SlateGray;
                    Check(formatted == 0,"changing a grid line color does not reformat the dataset");
                    grid.CellFormatting -= countFormatting;
                    grid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
                    grid.ReadOnly = false;
                }
                source.Position = 1;
                Check(grid.CurrentRow?.DataBoundItem == items[1] && title.Text == "Second","shared control and grid currency");
                title.Text = "Second (edited)";
                Check(items[1].Title == title.Text,"control to model on property change");
                items[1].Title = "External";
                items[1].Notify(nameof(Album.Title));
                Check(title.Text == "External","model to control on property change");
                year.Text = "2021";
                Check(items[1].Year == 0,"deferred property edit");
                Check(form.ValidateChildren() && items[1].Year == 2021,"validation write-back");
                available.Checked = true;
                Check(items[1].Available,"checked binding");
                bool bindingError = false;
                yearBinding.BindingComplete += (_,e) => { if (e.BindingCompleteState == BindingCompleteState.Exception) bindingError = true; };
                year.Text = "invalid";
                Check(!form.ValidateChildren() && bindingError && items[1].Year == 2021,"invalid binding conversion");
                year.Text = "2022";
                form.ValidateChildren();
                title.DataBindings.RemoveAt(0);
                title.Text = "Unbound";
                Check(items[1].Title == "External","removed binding");
                grid.CurrentCell = grid.Rows[0].Cells["Year"];
                Check(source.Position == 0,"grid to source currency");
                int valueChanges = 0;
                grid.CellValueChanged += (_,_) => valueChanges++;
                grid.CurrentCell.Value = 1998;
                Check(items[0].Year == 1998,"programmatic edit write-through");
                items[1].Title = "Revised";
                items[1].Notify(nameof(Album.Title));
                Check(Equals(grid.Rows[1].Cells["Title"].Value,"Revised"),"item notification");
                grid.Columns["Summary"].Visible = false;
                items.Add(new Album { Title = "Third" });
                Check(grid.Rows.Count == 3 && !grid.Columns["Summary"].Visible, "collection change preserving column configuration");

                if (OperatingSystem.IsMacOS())
                {
                    grid.CurrentCell = grid.Rows[0].Cells["Year"];
                    int showingCount = 0;
                    grid.EditingControlShowing += (_,e) => {
                        showingCount++;
                        Check(ReferenceEquals(e.Control,grid.EditingControl),"EditingControlShowing shares the displayed editor");
                    };
                    Check(grid.BeginEdit(true),"begin native editing");
                    var textEditor = grid.EditingControl as DataGridViewTextBoxEditingControl;
                    Check(textEditor != null && textEditor.EditingControlRowIndex==0 && textEditor.EditingControlDataGridView==grid &&
                        textEditor.Text=="1998" && textEditor.SelectionLength==4 && showingCount==1,"live field and selection");
                    textEditor!.SelectAll();
                    textEditor.SelectedText="2005";
                    Check(textEditor.Text=="2005" && textEditor.EditingControlValueChanged && grid.IsCurrentCellDirty,"managed replacement updates field");
                    NativeInput.ReplaceText("2007");
                    Check(textEditor.Text=="2007" && Equals(textEditor.EditingControlFormattedValue,"2007"),"native text updates managed editor");
                    NativeInput.LeaveField();
                    Check(items[0].Year == 2007 && !grid.IsCurrentCellInEditMode && grid.EditingControl==null,"native end-edit and typed write-back");
                    Check(grid.BeginEdit(true),"begin cancel case");
                    NativeInput.ReplaceText("2010");
                    Check(grid.CancelEdit() && items[0].Year == 2007,"cancel");
                    bool errored = false;
                    grid.DataError += (_,e) => { errored = true; e.ThrowException = false; };
                    Check(grid.BeginEdit(true),"begin invalid input");
                    NativeInput.ReplaceText("not an integer");
                    Check(!grid.EndEdit() && errored && items[0].Year == 2007,"invalid numeric input");
                    grid.CancelEdit();
                    NativeInput.ToggleCheckbox(0,2);
                    Check(items[0].Available,"native checkbox update");
                    grid.ReadOnly = true;
                    Check(!grid.BeginEdit(true),"read-only grid");
                    grid.ReadOnly = false;
                }
                Check(valueChanges > 0,"value-change event");
                source.MoveLast();
                source.RemoveCurrent();
                Check(grid.Rows.Count == 2 && source.Position == 1,"remove and currency");
                source.AddNew();
                Check(grid.Rows.Count == 3,"add new");
                source.CancelEdit();
                Check(grid.Rows.Count == 2,"cancel new");
                grid.AllowUserToAddRows = true;
                Check(grid.NewRowIndex == 2 && grid.Rows[2].IsNewRow,"new row placeholder");
                if (OperatingSystem.IsMacOS())
                {
                    grid.CurrentCell = grid.Rows[grid.NewRowIndex].Cells["Title"];
                    Check(grid.BeginEdit(true),"edit new row");
                    NativeInput.ReplaceText("AppKit added");
                    NativeInput.LeaveField();
                    Check(items.Count == 3 && items[^1].Title == "AppKit added" && grid.NewRowIndex == 3,"create from native editing");
                    bool deleted = false;
                    grid.UserDeletedRow += (_,_) => deleted = true;
                    NativeInput.DeleteSelectedRow();
                    Check(deleted && items.Count == 2 && grid.NewRowIndex == 2,"native Delete key");
                    var category = new DataGridViewComboBoxColumn {
                        Name="CategoryChoice", HeaderText="Category", DataPropertyName="Category",
                        ValueMember="Id", DisplayMember="Name", DataSource=new[] { new Category(10,"EP"),new Category(20,"Album") }
                    };
                    grid.Columns.Add(category);
                    grid.CurrentCell = grid.Rows[0].Cells["CategoryChoice"];
                    Check(grid.CurrentCell is DataGridViewComboBoxCell && grid.BeginEdit(false),"begin combo cell editing");
                    var choice = grid.EditingControl as DataGridViewComboBoxEditingControl;
                    Check(choice != null && Equals(choice.SelectedValue,10) && choice.Text=="EP","bound combo editor initializes by value");
                    int selectedEvents=0;
                    choice!.SelectedIndexChanged += (_,_) => selectedEvents++;
                    int visibleColumn=grid.Columns.Cast<DataGridViewColumn>().Count(c=>c.Visible)-1;
                    NativeInput.SelectChoice(0,visibleColumn,1);
                    Check(Equals(choice.SelectedValue,20) && choice.Text=="Album" && selectedEvents==1 && grid.IsCurrentCellDirty,"AppKit popup updates managed editing control");
                    Check(grid.EndEdit() && items[0].Category==20 && grid.EditingControl==null,"native combo write-back by ValueMember");
                    Check(grid.BeginEdit(false),"reopen bound combo");
                    ((ComboBox)grid.EditingControl!).SelectedIndex=0;
                    Check(grid.CancelEdit() && items[0].Category==20,"cancel combo edit");
                }
            }
            catch (Exception e) { failure = e; }
            finally { form.Close(); source.Dispose(); }
        };
        Application.Run(form);
        if (failure != null) { Console.Error.WriteLine(failure); return 1; }
        Console.WriteLine("PASS: text and checkbox editing, conversion, cancellation, currency and list notifications.");
        return 0;
    }
    static void Check(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
