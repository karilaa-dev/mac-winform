namespace ControlCatalog;

// The concrete public Control subclasses with a public parameterless constructor in .NET 8.
// A referenced constructor missing from the Mac source API fails at build time.
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Func<Control>[] factories =
        {
            () => new BindingNavigator(),
            () => new Button(),
            () => new CheckBox(),
            () => new CheckedListBox(),
            () => new ComboBox(),
            () => new ContainerControl(),
            () => new ContextMenuStrip(),
            () => new Control(),
            () => new DataGridView(),
            () => new DataGridViewComboBoxEditingControl(),
            () => new DataGridViewTextBoxEditingControl(),
            () => new DateTimePicker(),
            () => new DomainUpDown(),
            () => new FlowLayoutPanel(),
            () => new Form(),
            () => new GroupBox(),
            () => new HScrollBar(),
            () => new Label(),
            () => new LinkLabel(),
            () => new ListBox(),
            () => new ListView(),
            () => new MaskedTextBox(),
            () => new MdiClient(),
            () => new MenuStrip(),
            () => new MonthCalendar(),
            () => new NumericUpDown(),
            () => new Panel(),
            () => new PictureBox(),
            () => new PrintPreviewControl(),
            () => new PrintPreviewDialog(),
            () => new ProgressBar(),
            () => new PropertyGrid(),
            () => new RadioButton(),
            () => new RichTextBox(),
            () => new ScrollableControl(),
            () => new SplitContainer(),
            () => new Splitter(),
            () => new StatusStrip(),
            () => new TabControl(),
            () => new TableLayoutPanel(),
            () => new TabPage(),
            () => new TextBox(),
            () => new ToolStrip(),
            () => new ToolStripContainer(),
            () => new ToolStripContentPanel(),
            () => new ToolStripDropDown(),
            () => new ToolStripDropDownMenu(),
            () => new ToolStripPanel(),
            () => new TrackBar(),
            () => new TreeView(),
            () => new UserControl(),
            () => new VScrollBar(),
            () => new WebBrowser(),
        };
        foreach (var factory in factories)
        {
            using Control control = factory();
            control.CreateControl();
            if (control.Handle == IntPtr.Zero) throw new InvalidOperationException(control.GetType().FullName);
        }
        Console.WriteLine($"{factories.Length} control constructors and handles passed");
    }
}
