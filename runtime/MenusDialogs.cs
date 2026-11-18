using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

public sealed class ToolStripItemCollection : IEnumerable<ToolStripItem>
{
    private readonly Control.ControlCollection controls;
    internal ToolStripItemCollection(Control owner) => controls = owner.Controls;
    public int Count => controls.Count;
    public ToolStripItem this[int index] => (ToolStripItem)controls[index];
    public ToolStripItem? this[string name] => this.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
    public int Add(ToolStripItem item) { controls.Add(item); return controls.IndexOf(item); }
    public ToolStripItem Add(string text) { var item = new ToolStripMenuItem(text); Add(item); return item; }
    public ToolStripItem Add(string text, object? image, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text, image, onClick);
        Add(item);
        return item;
    }
    public void AddRange(ToolStripItem[] items) => controls.AddRange(items);
    public void Insert(int index, ToolStripItem item) => controls.Insert(index, item);
    public void Remove(ToolStripItem item) => controls.Remove(item);
    public void RemoveAt(int index) => controls.RemoveAt(index);
    public void Clear() => controls.Clear();
    public bool Contains(ToolStripItem item) => controls.Contains(item);
    public IEnumerator<ToolStripItem> GetEnumerator() => controls.Cast<ToolStripItem>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class MenuStrip : ToolStrip
{
    public MenuStrip()
    {
        Dock = DockStyle.Top;
        Height = 25;
        Padding = new Padding(3, 0, 0, 0);
    }

    internal override void Arrange(Size delta)
    {
        int x = Padding.Left;
        foreach (ToolStripItem item in Items)
        {
            int width = item.AutoSize ? (int)Math.Ceiling(item.Text.Replace("&", "").Length * item.Font.Size * 0.95) + 16 : item.Width;
            item.Bounds = new Rectangle(x, Padding.Top, width, Height - Padding.Vertical());
            x += width;
        }
    }
}

public class ToolStripMenuItem : ToolStripItem
{
    private bool check;
    internal override int Kind => 18;

    public ToolStripMenuItem() => DropDownItems = new ToolStripItemCollection(this);
    public ToolStripMenuItem(string text) : this() => Text = text;
    public ToolStripMenuItem(string text, object? image, EventHandler? onClick) : this(text) { Click += onClick; }
    public ToolStripMenuItem(string text, object? image, params ToolStripItem[] items) : this(text) { DropDownItems.AddRange(items); }
    public ToolStripItemCollection DropDownItems { get; }
    public Keys ShortcutKeys { get; set; }
    public bool CheckOnClick { get; set; }
    public bool Checked
    {
        get => check;
        set
        {
            if (check == value) return;
            check = value;
            if (NativeHandle != IntPtr.Zero) Native.mf_checked(NativeHandle, check ? 1 : 0);
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler? CheckedChanged;
    protected override void OnClick(EventArgs e)
    {
        if (CheckOnClick) Checked = !Checked;
        base.OnClick(e);
    }
    internal override void ApplyNative() { base.ApplyNative(); Native.mf_checked(NativeHandle, check ? 1 : 0); }
}

public class ToolStripSeparator : ToolStripItem { internal override int Kind => 19; }

public abstract class CommonDialog : Component
{
    private sealed record Selection(string[] Paths, int FilterIndex);
    [DllImport("MacForms")] private static extern IntPtr mf_pick_file(IntPtr ownerView, int kind, string options);
    [DllImport("MacForms")] private static extern void mf_free(IntPtr pointer);

    internal static (string[] paths, int filterIndex)? Pick(IntPtr hwndOwner, int kind, object options)
    {
        Application.Initialize();
        IntPtr result = mf_pick_file(OwnerView(hwndOwner), kind, JsonSerializer.Serialize(options));
        if (result == IntPtr.Zero) return null;
        try
        {
            var selected = JsonSerializer.Deserialize<Selection>(Marshal.PtrToStringUTF8(result)!);
            return selected == null || selected.Paths.Length == 0 ? null : (selected.Paths, selected.FilterIndex);
        }
        finally { mf_free(result); }
    }
    public DialogResult ShowDialog() => ShowDialog(null);
    public DialogResult ShowDialog(IWin32Window? owner)
    {
        Application.Initialize();
        return RunDialog(owner?.Handle ?? IntPtr.Zero) ? DialogResult.OK : DialogResult.Cancel;
    }
    internal static IntPtr OwnerView(IntPtr handle) => Control.FromHandle(handle)?.ViewHandle ?? IntPtr.Zero;
    public object? Tag { get; set; }
    public event EventHandler? HelpRequest;
    protected virtual void OnHelpRequest(EventArgs e) => HelpRequest?.Invoke(this,e);
    protected abstract bool RunDialog(IntPtr hwndOwner);
    public abstract void Reset();
}

public abstract class FileDialog : CommonDialog
{
    private string filter = "";
    private string[] names = Array.Empty<string>();

    public string Title { get; set; } = "";
    public string InitialDirectory { get; set; } = "";
    public int FilterIndex { get; set; } = 1;
    public bool AddExtension { get; set; } = true;
    public string DefaultExt { get; set; } = "";
    public bool RestoreDirectory { get; set; }
    public string Filter
    {
        get => filter;
        set
        {
            value ??= "";
            if (value.Length > 0 && value.Split('|').Length % 2 != 0)
                throw new ArgumentException("File filters require description and pattern pairs.", nameof(value));
            filter = value;
        }
    }
    public string FileName
    {
        get => names.FirstOrDefault() ?? "";
        set => names = string.IsNullOrEmpty(value) ? Array.Empty<string>() : new[] { value };
    }
    public string[] FileNames => (string[])names.Clone();
    public override void Reset()
    {
        Title = InitialDirectory = FileName = Filter = DefaultExt = "";
        FilterIndex = 1;
        AddExtension = true;
        RestoreDirectory = false;
        if (this is OpenFileDialog open) open.Multiselect = false;
    }
    public event CancelEventHandler? FileOk;
    protected override bool RunDialog(IntPtr hwndOwner)
    {
        bool save = this is SaveFileDialog;
        while (true)
        {
            var selected = Pick(hwndOwner, save ? 1 : 0, new
            {
                Title, Directory = InitialDirectory, FileName, Filter, FilterIndex,
                DefaultExt = AddExtension ? DefaultExt.TrimStart('.') : "",
                Multiselect = this is OpenFileDialog { Multiselect: true }, CreateFolders = true
            });
            if (selected == null) return false;
            var previousNames = names;
            int previousIndex = FilterIndex;
            (names, FilterIndex) = selected.Value;
            var e = new CancelEventArgs();
            FileOk?.Invoke(this, e);
            if (!e.Cancel) return true;
            names = previousNames;
            FilterIndex = previousIndex;
        }
    }
}

public sealed class OpenFileDialog : FileDialog
{
    public bool Multiselect { get; set; }
    public string SafeFileName => Path.GetFileName(FileName);
    public string[] SafeFileNames => FileNames.Select(path => Path.GetFileName(path)).ToArray();
    public Stream OpenFile() => File.OpenRead(FileName);
}

public sealed class SaveFileDialog : FileDialog
{
    public Stream OpenFile() => File.Open(FileName, FileMode.Create, FileAccess.ReadWrite);
}

public sealed class FolderBrowserDialog : CommonDialog
{
    public string Description { get; set; } = "";
    public string SelectedPath { get; set; } = "";
    public bool ShowNewFolderButton { get; set; } = true;
    public bool UseDescriptionForTitle { get; set; }
    public override void Reset()
    {
        Description = SelectedPath = "";
        ShowNewFolderButton = true;
        UseDescriptionForTitle = false;
    }
    protected override bool RunDialog(IntPtr hwndOwner)
    {
        var selected = Pick(hwndOwner, 2, new
        {
            Title = UseDescriptionForTitle ? Description : "", Message = UseDescriptionForTitle ? "" : Description,
            Directory = SelectedPath, CreateFolders = ShowNewFolderButton
        });
        if (selected == null) return false;
        SelectedPath = selected.Value.paths[0];
        return true;
    }
}
