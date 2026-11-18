using System.ComponentModel;
using System.Windows.Forms;
using PropertyBrowser = System.Windows.Forms.PropertyGrid;

namespace PropertyGrid;

internal static class Program
{
    private sealed class Record
    {
        [Category("Data")]
        public DisplayOptions Options { get; } = new();
        [Category("Appearance"), DisplayName("Caption"), Description("The visible title.")]
        public string Title { get; set; } = "Untitled";
        [Category("Data"), DefaultValue(9)]
        public int Count { get; set; }
        [Category("Data"), ReadOnly(true)]
        public bool Locked => true;
        [Browsable(false)]
        public string InternalCode => "test";
        [MergableProperty(false)]
        public int IndividualOnly { get; set; }
    }
    [TypeConverter(typeof(ExpandableObjectConverter))]
    private sealed class DisplayOptions
    {
        public int Padding { get; set; }
    }

    [STAThread]
    public static int Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "PropertyGrid editing", ClientSize = new(420, 360) };
        var browser = new PropertyBrowser { Dock = DockStyle.Fill };
        form.Controls.Add(browser);
        var first = new Record { Count = 47 };
        var second = new Record { Count = 23 };
        int changes = 0;
        Exception? failure = null;
        browser.PropertyValueChanged += (_, e) =>
        {
            if (e.ChangedItem?.PropertyDescriptor?.Name == nameof(Record.Count)) changes++;
        };
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Delay(100);
                browser.SelectedObjects = [first, second];
                GridItem root = Root(browser);
                var data = root.GridItems["Data"]!;
                var count = data.GridItems["Count"]!;
                var title = root.GridItems["Appearance"]!.GridItems["Caption"]!;
                Check(data.GridItemType == GridItemType.Category && count.GridItemType == GridItemType.Property, "categories");
                Check(root.GridItems.Cast<GridItem>().All(group => group.GridItems["InternalCode"] == null && group.GridItems["IndividualOnly"] == null), "browsable and mergable filters");
                Check(title.PropertyDescriptor?.Name == nameof(Record.Title), "display name");
                browser.SelectedGridItem = count;
                browser.ResetSelectedProperty();
                Check(first.Count == 9 && second.Count == 9, "multi-object reset");
                if (OperatingSystem.IsMacOS())
                {
                    var editor = browser.Controls.OfType<DataGridView>().Single();
                    NativeInput.WindowTitle = form.Text;
                    Check(editor.BeginEdit(true), "begin AppKit edit");
                    NativeInput.ReplaceText("27");
                    NativeInput.LeaveField();
                    Check(first.Count == 27 && second.Count == 27 && changes > 0, "typed multi-object edit and notification");
                    Check(editor.BeginEdit(true), "begin invalid edit");
                    NativeInput.ReplaceText("invalid");
                    Check(!editor.EndEdit() && first.Count == 27 && second.Count == 27, "reject invalid number");
                    editor.CancelEdit();
                    browser.SelectedGridItem = data.GridItems["Locked"]!;
                    Check(!editor.BeginEdit(true), "read-only descriptor");
                    var options = data.GridItems["Options"]!;
                    Check(options.Expandable && options.GridItems["Padding"]!.Select(), "nested converter descriptors");
                    Check(editor.BeginEdit(true), "begin nested reference property edit");
                    NativeInput.ReplaceText("12");
                    NativeInput.LeaveField();
                    Check(first.Options.Padding == 12 && second.Options.Padding == 12, "nested multi-object edit");
                    title.Select();
                    Check(browser.Controls.OfType<Label>().Any(l => l.Text.Contains("The visible title.")), "help pane");
                }
                browser.CollapseAllGridItems();
                Check(!data.Expanded, "collapse categories");
                browser.ExpandAllGridItems();
                Check(data.Expanded, "expand categories");
                browser.PropertySort = PropertySort.Alphabetical;
                var labels = Root(browser).GridItems.Cast<GridItem>().Select(i => i.Label).ToArray();
                Check(labels.SequenceEqual(new[] { "Caption", "Count", "Locked", "Options" }), "alphabetical view");
                browser.SelectedObject = first;
                Check(Root(browser).GridItems["IndividualOnly"] != null, "single-object property");
                browser.SelectedObject = null;
                Check(browser.SelectedGridItem == null, "clear selection");
            }
            catch (Exception e) { failure = e; }
            finally { form.Close(); }
        };
        Application.Run(form);
        if (failure != null) { Console.Error.WriteLine(failure); return 1; }
        Console.WriteLine("PASS: PropertyGrid categories, sorting, selection, descriptor filters, typed editing, multi-object reset and help.");
        return 0;
    }
    private static GridItem Root(PropertyBrowser browser)
    {
        var item = browser.SelectedGridItem ?? throw new InvalidOperationException("No selection.");
        while (item.Parent != null) item = item.Parent;
        return item;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
