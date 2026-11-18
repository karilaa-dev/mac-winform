using System.ComponentModel;
using System.Drawing;
using System.Globalization;

namespace System.Windows.Forms;

public class BindingNavigator : ToolStrip, ISupportInitialize
{
    private BindingSource? source;
    private ToolStripItem? first, previous, next, last, add, delete, position, count;
    private bool initializing;
    private string countFormat = "of {0}";
    public BindingNavigator() {}
    public BindingNavigator(bool addStandardItems) { if (addStandardItems) AddStandardItems(); }
    public BindingNavigator(BindingSource? bindingSource) : this(true) { BindingSource = bindingSource; }
    public BindingNavigator(IContainer container) { ArgumentNullException.ThrowIfNull(container); container.Add(this); }

    public BindingSource? BindingSource
    {
        get => source;
        set
        {
            if (ReferenceEquals(value,source)) return;
            if (source != null)
            {
                source.PositionChanged -= Changed;
                source.ListChanged -= ListChanged;
                source.Disposed -= SourceDisposed;
            }
            source = value;
            if (source != null)
            {
                source.PositionChanged += Changed;
                source.ListChanged += ListChanged;
                source.Disposed += SourceDisposed;
            }
            RefreshState();
        }
    }
    private void Changed(object? sender,EventArgs e) => RefreshState();
    private void ListChanged(object? sender,ListChangedEventArgs e) => RefreshState();
    private void SourceDisposed(object? sender,EventArgs e) => BindingSource = null;

    public ToolStripItem? MoveFirstItem { get => first; set => SetButton(ref first,value,MoveFirst); }
    public ToolStripItem? MovePreviousItem { get => previous; set => SetButton(ref previous,value,MovePrevious); }
    public ToolStripItem? MoveNextItem { get => next; set => SetButton(ref next,value,MoveNext); }
    public ToolStripItem? MoveLastItem { get => last; set => SetButton(ref last,value,MoveLast); }
    public ToolStripItem? AddNewItem { get => add; set => SetButton(ref add,value,AddNew); }
    public ToolStripItem? DeleteItem { get => delete; set => SetButton(ref delete,value,Delete); }
    private void SetButton(ref ToolStripItem? field,ToolStripItem? item,EventHandler handler)
    {
        if (ReferenceEquals(field,item)) return;
        if (field != null) field.Click -= handler;
        field = item;
        if (field != null) field.Click += handler;
        RefreshState();
    }
    public ToolStripItem? PositionItem
    {
        get => position;
        set
        {
            if (ReferenceEquals(position,value)) return;
            var old = Editor(position);
            if (old != null) { old.KeyDown -= PositionKey; old.LostFocus -= CommitPosition; }
            position = value;
            var editor = Editor(value);
            if (editor != null) { editor.KeyDown += PositionKey; editor.LostFocus += CommitPosition; }
            RefreshState();
        }
    }
    private static Control? Editor(ToolStripItem? item) => (item as ToolStripControlHost)?.Control ?? item;
    public ToolStripItem? CountItem { get => count; set { count = value; RefreshState(); } }
    public string CountItemFormat { get => countFormat; set { countFormat = value ?? "of {0}"; RefreshState(); } }
    public event EventHandler? RefreshItems;
    public void BeginInit() => initializing = true;
    public void EndInit() { initializing = false; RefreshState(); }
    private void RefreshState() { if (!initializing) OnRefreshItems(); }
    protected virtual void OnRefreshItems()
    {
        RefreshItemsCore();
        RefreshItems?.Invoke(this,EventArgs.Empty);
    }
    protected virtual void RefreshItemsCore()
    {
        int total = source?.Count ?? 0;
        int current = (source?.Position ?? -1) + 1;
        if (first != null) first.Enabled = current > 1;
        if (previous != null) previous.Enabled = current > 1;
        if (next != null) next.Enabled = current < total;
        if (last != null) last.Enabled = current < total;
        if (add != null) add.Enabled = source?.AllowNew == true;
        if (delete != null) delete.Enabled = source?.AllowRemove == true && total > 0;
        if (position != null)
        {
            position.Enabled = current > 0;
            position.Text = current.ToString(CultureInfo.CurrentCulture);
        }
        if (count != null)
        {
            count.Enabled = total > 0;
            count.Text = string.Format(CultureInfo.CurrentCulture,countFormat,total);
        }
    }
    public bool Validate() => FindForm()?.Validate() ?? true;
    private void MoveFirst(object? s,EventArgs e) { if (Validate()) source?.MoveFirst(); }
    private void MovePrevious(object? s,EventArgs e) { if (Validate()) source?.MovePrevious(); }
    private void MoveNext(object? s,EventArgs e) { if (Validate()) source?.MoveNext(); }
    private void MoveLast(object? s,EventArgs e) { if (Validate()) source?.MoveLast(); }
    private void AddNew(object? s,EventArgs e) { if (Validate()) source?.AddNew(); }
    private void Delete(object? s,EventArgs e) { if (Validate()) source?.RemoveCurrent(); }
    private void PositionKey(object? sender,KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter && e.KeyCode != Keys.Escape) return;
        if (e.KeyCode == Keys.Enter) CommitPosition(sender,e);
        else RefreshState();
        e.SuppressKeyPress = true;
    }
    private void CommitPosition(object? sender,EventArgs e)
    {
        if (source != null && int.TryParse(position?.Text,out int n)) source.Position = Math.Max(1,n) - 1;
        RefreshState();
    }

    public virtual void AddStandardItems()
    {
        string prefix = Name.Length > 0 && char.IsUpper(Name[0]) ? "BindingNavigator" : "bindingNavigator";
        ToolStripButton Button(string id,string text)
        {
            using var icon = typeof(BindingNavigator).Assembly.GetManifestResourceStream("BindingNavigator." + id + ".ico");
            return new ToolStripButton(text) {
                Name = prefix + id + "Item", ToolTipText = text, AutoSize = false, Width = 23,
                DisplayStyle = ToolStripItemDisplayStyle.Image, Image = Image.FromStream(icon!)
            };
        }
        ToolStripSeparator Separator() => new() { Name = prefix + "Separator", AutoSize = false, Width = 6 };
        MoveFirstItem = Button("MoveFirst","Move first");
        MovePreviousItem = Button("MovePrevious","Move previous");
        MoveNextItem = Button("MoveNext","Move next");
        MoveLastItem = Button("MoveLast","Move last");
        AddNewItem = Button("AddNew","Add new");
        DeleteItem = Button("Delete","Delete");
        PositionItem = new ToolStripTextBox { Name = prefix+"PositionItem", Width = 50, ToolTipText = "Current position" };
        CountItem = new ToolStripLabel { Name = prefix+"CountItem", ToolTipText = "Total number of items" };
        Items.AddRange(new[] {first!,previous!,Separator(),position!,count!,Separator(),next!,last!,Separator(),add!,delete!});
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            initializing = true;
            BindingSource = null;
            MoveFirstItem = MovePreviousItem = MoveNextItem = MoveLastItem = AddNewItem = DeleteItem = PositionItem = CountItem = null;
        }
        base.Dispose(disposing);
    }
}
