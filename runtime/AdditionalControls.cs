using System.Drawing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace System.Windows.Forms;

internal static class AdditionalNative
{
    private const string Lib = "MacForms";
    [DllImport(Lib)] internal static extern void mf_progress(IntPtr h, int min, int max, int value, int marquee);
    [DllImport(Lib)] internal static extern void mf_track(IntPtr h, int min, int max, int value, int ticks, int style, int vertical);
    [DllImport(Lib)] internal static extern int mf_track_value(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_tree_appearance(IntPtr h,int indent,int height,int lines,int rootLines,int plusMinus,int fullRow,int hideSelection,int lineArgb,int borderStyle);
    [DllImport(Lib)] internal static extern void mf_tree_nodes(IntPtr h, string nodes);
    [DllImport(Lib)] internal static extern void mf_tree_select(IntPtr h, int id);
    [DllImport(Lib)] internal static extern int mf_tree_selected(IntPtr h);
    [DllImport(Lib)] internal static extern void mf_tree_expand(IntPtr h, int id, int expand, int recursive);
    [DllImport(Lib)] internal static extern int mf_tree_expanded(IntPtr h, int id);
}

public enum ProgressBarStyle { Blocks, Continuous, Marquee }
public class ProgressBar : Control
{
    private int minimum, maximum = 100, position;
    private ProgressBarStyle style;
    internal override int Kind => 20;

    public int Minimum
    {
        get => minimum;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); minimum = value; maximum = Math.Max(maximum, value); UpdateRange(); }
    }
    public int Maximum
    {
        get => maximum;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); maximum = value; minimum = Math.Min(minimum, value); UpdateRange(); }
    }
    public int Value
    {
        get => position;
        set
        {
            if (value < Minimum || value > Maximum) throw new ArgumentOutOfRangeException(nameof(value));
            position = value;
            UpdateRange();
        }
    }
    public int Step { get; set; } = 10;
    public ProgressBarStyle Style { get => style; set { style = value; UpdateRange(); } }
    public void Increment(int value) => Value = (int)Math.Clamp((long)position + value, minimum, maximum);
    public void PerformStep() => Increment(Step);
    private void UpdateRange()
    {
        position = Math.Clamp(position, minimum, maximum);
        if (NativeHandle != 0) ApplyNative();
    }
    internal override void ApplyNative() => AdditionalNative.mf_progress(NativeHandle, minimum, maximum, position, style == ProgressBarStyle.Marquee ? 1 : 0);
}

public enum Orientation { Horizontal, Vertical }
public enum TickStyle { None, TopLeft, BottomRight, Both }
public class TrackBar : Control, ISupportInitialize
{
    private int minimum, maximum = 10, position, frequency = 1;
    private TickStyle ticks = TickStyle.BottomRight;
    private Orientation orientation;
    private bool initializing;
    internal override int Kind => 21;
    public TrackBar() => Size = new(104, 45);
    public int Minimum { get => minimum; set => SetRange(value, Math.Max(value, maximum)); }
    public int Maximum { get => maximum; set => SetRange(Math.Min(value, minimum), value); }
    public int Value
    {
        get => position;
        set
        {
            if (!initializing && (value < minimum || value > maximum)) throw new ArgumentOutOfRangeException(nameof(value));
            SetValue(value);
        }
    }
    public int TickFrequency { get => frequency; set { frequency = Math.Max(1, value); Sync(); } }
    public TickStyle TickStyle { get => ticks; set { ticks = value; Sync(); } }
    public Orientation Orientation { get => orientation; set { orientation = value; Sync(); } }
    public event EventHandler? ValueChanged, Scroll;
    public void SetRange(int min, int max)
    {
        minimum = min;
        maximum = Math.Max(min, max);
        if (!initializing) SetValue(Math.Clamp(position, minimum, maximum));
        Sync();
    }
    public void BeginInit() => initializing = true;
    public void EndInit() { initializing = false; SetRange(minimum, maximum); }
    private void SetValue(int value)
    {
        if (position == value) return;
        position = value;
        Sync();
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Sync() { if (!initializing && NativeHandle != 0) ApplyNative(); }
    internal override void ApplyNative() => AdditionalNative.mf_track(NativeHandle, minimum, maximum, position,
        frequency, (int)ticks, orientation == Orientation.Vertical ? 1 : 0);
    internal override void OnNative(int ev)
    {
        if (ev == 7)
        {
            SetValue(AdditionalNative.mf_track_value(NativeHandle));
            Scroll?.Invoke(this, EventArgs.Empty);
        }
        base.OnNative(ev);
    }
}

public enum TreeViewAction { Unknown, ByKeyboard, ByMouse, Collapse, Expand }
public class TreeViewEventArgs(TreeNode node, TreeViewAction action = TreeViewAction.Unknown) : EventArgs
{
    public TreeNode Node { get; } = node;
    public TreeViewAction Action { get; } = action;
}
public delegate void TreeViewEventHandler(object? sender, TreeViewEventArgs e);

public class TreeNode
{
    private static int nextId;
    internal int Id { get; } = Interlocked.Increment(ref nextId);
    internal bool Expanded { get; set; }
    internal TreeNodeCollection? Collection { get; set; }
    private string text;
    public TreeNode() : this("") { }
    public TreeNode(string text) { this.text = text; Nodes = new TreeNodeCollection(this); }
    public TreeNode(string text, TreeNode[] children) : this(text) => Nodes.AddRange(children);
    public string Name { get; set; } = "";
    public string Text { get => text; set { text = value ?? ""; TreeView?.RefreshNodes(); } }
    public object? Tag { get; set; }
    public TreeNode? Parent => Collection?.Parent;
    public TreeView? TreeView => Collection?.Tree;
    public TreeNodeCollection Nodes { get; }
    public int Level => Parent == null ? 0 : Parent.Level + 1;
    public string FullPath => Parent == null ? Text : Parent.FullPath + (TreeView?.PathSeparator ?? "\\") + Text;
    public bool IsExpanded => TreeView is { NativeHandle: var h } && h != 0 ? AdditionalNative.mf_tree_expanded(h, Id) != 0 : Expanded;
    public void Remove() => Collection?.Remove(this);
    public void Expand() => Expand(true, false);
    public void ExpandAll() => Expand(true, true);
    public void Collapse() => Expand(false, false);
    internal void Expand(bool open, bool recursive)
    {
        Expanded = open;
        if (recursive) foreach (var child in Nodes.Walk()) child.Expanded = open;
        if (TreeView is { NativeHandle: var h } && h != 0)
            AdditionalNative.mf_tree_expand(h, Id, open ? 1 : 0, recursive ? 1 : 0);
    }
}

public sealed class TreeNodeCollection : Collection<TreeNode>
{
    private readonly TreeView? root;
    internal TreeNode? Parent { get; }
    internal TreeView? Tree => root ?? Parent?.TreeView;
    internal TreeNodeCollection(TreeView root) => this.root = root;
    internal TreeNodeCollection(TreeNode parent) => Parent = parent;
    public TreeNode? this[string key] => this.FirstOrDefault(n => string.Equals(n.Name, key, StringComparison.OrdinalIgnoreCase));
    public new int Add(TreeNode node) { base.Add(node); return Count - 1; }
    public TreeNode Add(string text) => Add("", text);
    public TreeNode Add(string key, string text)
    {
        var node = new TreeNode(text) { Name = key };
        Add(node);
        return node;
    }
    public void AddRange(TreeNode[] nodes)
    {
        var tree = Tree;
        tree?.BeginUpdate();
        try { foreach (var node in nodes) Add(node); }
        finally { tree?.EndUpdate(); }
    }
    internal IEnumerable<TreeNode> Walk()
    {
        foreach (var node in this)
        {
            yield return node;
            foreach (var child in node.Nodes.Walk()) yield return child;
        }
    }
    private void Validate(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Collection != null) throw new ArgumentException("This node already belongs to a tree.");
        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
            if (node == ancestor) throw new ArgumentException("A tree node cannot contain itself.");
    }
    protected override void InsertItem(int index, TreeNode item)
    {
        Validate(item);
        base.InsertItem(index, item);
        item.Collection = this;
        Tree?.RefreshNodes();
    }
    protected override void RemoveItem(int index)
    {
        this[index].Collection = null;
        base.RemoveItem(index);
        Tree?.RefreshNodes();
    }
    protected override void SetItem(int index, TreeNode item)
    {
        if (this[index] == item) return;
        Validate(item);
        this[index].Collection = null;
        base.SetItem(index, item);
        item.Collection = this;
        Tree?.RefreshNodes();
    }
    protected override void ClearItems()
    {
        foreach (var node in this) node.Collection = null;
        base.ClearItems();
        Tree?.RefreshNodes();
    }
}

public class TreeView : Control
{
    private TreeNode? selected;
    private int updating;
    internal override int Kind => 22;
    public TreeView() { Nodes = new TreeNodeCollection(this); Size = new(121, 97); }
    public TreeNodeCollection Nodes { get; }
    private bool lines = true, rootLines = true, plusMinus = true, fullRow, hideSelection = true;
    private int indent = 19, itemHeight;
    private Color lineColor = Color.Empty;
    private BorderStyle border = BorderStyle.Fixed3D;
    public bool ShowLines { get => lines; set { lines = value; ApplyAppearance(); } }
    public bool ShowRootLines { get => rootLines; set { rootLines = value; ApplyAppearance(); } }
    public bool ShowPlusMinus { get => plusMinus; set { plusMinus = value; ApplyAppearance(); } }
    public bool FullRowSelect { get => fullRow; set { fullRow = value; ApplyAppearance(); } }
    public bool HideSelection { get => hideSelection; set { hideSelection = value; ApplyAppearance(); } }
    public BorderStyle BorderStyle { get => border; set { border = value; ApplyAppearance(); } }
    public Color LineColor { get => lineColor; set { lineColor = value; ApplyAppearance(); } }
    public int Indent { get => indent; set { if (value < 0 || value > 32000) throw new ArgumentOutOfRangeException(nameof(value)); indent = value; ApplyAppearance(); } }
    public int ItemHeight { get => itemHeight == 0 ? Math.Max(1,(int)Math.Round(18*Font.SizeInPoints/9)) : itemHeight; set { if (value < 1 || value > 32767) throw new ArgumentOutOfRangeException(nameof(value)); itemHeight = value; ApplyAppearance(); } }
    private void ApplyAppearance()
    {
        if (NativeHandle != 0) AdditionalNative.mf_tree_appearance(NativeHandle,indent,itemHeight,lines ? 1 : 0,rootLines ? 1 : 0,
            plusMinus ? 1 : 0,fullRow ? 1 : 0,hideSelection ? 1 : 0,lineColor.IsEmpty ? 0 : lineColor.ToArgb(),(int)border);
    }
    public string PathSeparator { get; set; } = "\\";
    public event TreeViewEventHandler? AfterSelect;
    public TreeNode? SelectedNode
    {
        get => selected;
        set
        {
            if (selected == value || (value != null && value.TreeView != this)) return;
            selected = value;
            if (NativeHandle != 0) AdditionalNative.mf_tree_select(NativeHandle, selected?.Id ?? 0);
            if (selected != null) AfterSelect?.Invoke(this, new(selected));
        }
    }
    public void BeginUpdate() => updating++;
    public void EndUpdate() { if (updating > 0 && --updating == 0) RefreshNodes(); }
    public void ExpandAll() { foreach (var root in Nodes) root.Expand(true, true); }
    public void CollapseAll() { foreach (var root in Nodes) root.Expand(false, true); }
    internal void RefreshNodes()
    {
        if (updating > 0) return;
        if (selected?.TreeView != this) selected = null;
        if (NativeHandle == 0) return;
        string json = JsonSerializer.Serialize(Nodes.Walk().Select(n => new { n.Id, n.Text, Parent = n.Parent?.Id ?? 0, n.Expanded }));
        AdditionalNative.mf_tree_nodes(NativeHandle, json);
        AdditionalNative.mf_tree_select(NativeHandle, selected?.Id ?? 0);
    }
    internal override void ApplyNative() { ApplyAppearance(); RefreshNodes(); }
    internal override void OnNative(int ev)
    {
        if (ev == 4)
        {
            int id = AdditionalNative.mf_tree_selected(NativeHandle);
            SelectedNode = Nodes.Walk().FirstOrDefault(node => node.Id == id);
        }
        base.OnNative(ev);
    }
}
