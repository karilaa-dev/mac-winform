// Event contracts from dotnet/winforms v8.0.20, .NET Foundation, MIT. See notices/dotnet-winforms.txt.
using System.ComponentModel;
using System.Drawing;
namespace System.Windows.Forms;

public class BindingManagerDataErrorEventArgs : EventArgs
{
    public BindingManagerDataErrorEventArgs(Exception exception)
    {
        Exception = exception;
    }

    public Exception Exception { get; }
}

public delegate void BindingManagerDataErrorEventHandler(object? sender, BindingManagerDataErrorEventArgs e);

public class CacheVirtualItemsEventArgs : EventArgs
{
    public CacheVirtualItemsEventArgs(int startIndex, int endIndex)
    {
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    public int StartIndex { get; }

    public int EndIndex { get; }
}

public delegate void CacheVirtualItemsEventHandler(object? sender, CacheVirtualItemsEventArgs e);

public class ColumnReorderedEventArgs : CancelEventArgs
{
    public ColumnReorderedEventArgs(int oldDisplayIndex, int newDisplayIndex, ColumnHeader? header) : base()
    {
        OldDisplayIndex = oldDisplayIndex;
        NewDisplayIndex = newDisplayIndex;
        Header = header;
    }

    public int OldDisplayIndex { get; }

    public int NewDisplayIndex { get; }

    public ColumnHeader? Header { get; }
}

public delegate void ColumnReorderedEventHandler(object? sender, ColumnReorderedEventArgs e);

public class ColumnWidthChangedEventArgs : EventArgs
{
    public ColumnWidthChangedEventArgs(int columnIndex)
    {
        ColumnIndex = columnIndex;
    }

    public int ColumnIndex { get; }
}

public delegate void ColumnWidthChangedEventHandler(object? sender, ColumnWidthChangedEventArgs e);

public class ColumnWidthChangingEventArgs : CancelEventArgs
{
    public ColumnWidthChangingEventArgs(int columnIndex, int newWidth) : this(columnIndex, newWidth, false)
    {
    }

    public ColumnWidthChangingEventArgs(int columnIndex, int newWidth, bool cancel) : base(cancel)
    {
        ColumnIndex = columnIndex;
        NewWidth = newWidth;
    }

    public int ColumnIndex { get; }

    public int NewWidth { get; set; }
}

public delegate void ColumnWidthChangingEventHandler(object? sender, ColumnWidthChangingEventArgs e);

public class ContentsResizedEventArgs : EventArgs
{

    public ContentsResizedEventArgs(Rectangle newRectangle)
    {
        NewRectangle = newRectangle;
    }

    public Rectangle NewRectangle { get; }
}

public delegate void ContentsResizedEventHandler(object? sender, ContentsResizedEventArgs e);

public class DataGridViewAutoSizeColumnModeEventArgs : EventArgs
{
    public DataGridViewAutoSizeColumnModeEventArgs(DataGridViewColumn? dataGridViewColumn, DataGridViewAutoSizeColumnMode previousMode)
    {
        Column = dataGridViewColumn;
        PreviousMode = previousMode;
    }

    public DataGridViewColumn? Column { get; }

    public DataGridViewAutoSizeColumnMode PreviousMode { get; }
}

public class DataGridViewAutoSizeColumnsModeEventArgs : EventArgs
{
    public DataGridViewAutoSizeColumnsModeEventArgs(DataGridViewAutoSizeColumnMode[]? previousModes)
    {
        PreviousModes = previousModes;
    }

    public DataGridViewAutoSizeColumnMode[]? PreviousModes { get; }
}

public class DataGridViewAutoSizeModeEventArgs : EventArgs
{
    public DataGridViewAutoSizeModeEventArgs(bool previousModeAutoSized)
    {
        PreviousModeAutoSized = previousModeAutoSized;
    }

    public bool PreviousModeAutoSized { get; }
}

public class DataGridViewBindingCompleteEventArgs : EventArgs
{
    public DataGridViewBindingCompleteEventArgs(ListChangedType listChangedType)
    {
        ListChangedType = listChangedType;
    }

    public ListChangedType ListChangedType { get; }
}

public class DataGridViewCellContextMenuStripNeededEventArgs : DataGridViewCellEventArgs
{
    public DataGridViewCellContextMenuStripNeededEventArgs(int columnIndex, int rowIndex)
        : base(columnIndex, rowIndex)
    {
    }

    internal DataGridViewCellContextMenuStripNeededEventArgs(int columnIndex, int rowIndex, ContextMenuStrip? contextMenuStrip)
        : base(columnIndex, rowIndex)
    {
        ContextMenuStrip = contextMenuStrip;
    }

    public ContextMenuStrip? ContextMenuStrip { get; set; }
}

public class DataGridViewCellErrorTextNeededEventArgs : DataGridViewCellEventArgs
{
    internal DataGridViewCellErrorTextNeededEventArgs(int columnIndex, int rowIndex, string? errorText)
        : base(columnIndex, rowIndex)
    {
        ErrorText = errorText;
    }

    public string? ErrorText { get; set; }
}

public class DataGridViewCellParsingEventArgs : ConvertEventArgs
{
    public DataGridViewCellParsingEventArgs(
        int rowIndex,
        int columnIndex,
        object? value,
        Type? desiredType,
        DataGridViewCellStyle? inheritedCellStyle)
        : base(value, desiredType)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        InheritedCellStyle = inheritedCellStyle;
    }

    public int RowIndex { get; }

    public int ColumnIndex { get; }

    public DataGridViewCellStyle? InheritedCellStyle { get; set; }

    public bool ParsingApplied { get; set; }
}

internal class DataGridViewCellStyleChangedEventArgs : EventArgs
{
    public DataGridViewCellStyleChangedEventArgs()
    {
    }

    public bool ChangeAffectsPreferredSize { get; set; }
}

public class DataGridViewCellToolTipTextNeededEventArgs : DataGridViewCellEventArgs
{
    internal DataGridViewCellToolTipTextNeededEventArgs(int columnIndex, int rowIndex, string? toolTipText)
        : base(columnIndex, rowIndex)
    {
        ToolTipText = toolTipText;
    }

    public string? ToolTipText { get; set; }
}

public class DataGridViewColumnStateChangedEventArgs : EventArgs
{
    public DataGridViewColumnStateChangedEventArgs(DataGridViewColumn dataGridViewColumn, DataGridViewElementStates stateChanged)
    {
        ArgumentNullException.ThrowIfNull(dataGridViewColumn);

        Column = dataGridViewColumn;
        StateChanged = stateChanged;
    }

    public DataGridViewColumn Column { get; }

    public DataGridViewElementStates StateChanged { get; }
}

public class DataGridViewRowContextMenuStripNeededEventArgs : EventArgs
{
    public DataGridViewRowContextMenuStripNeededEventArgs(int rowIndex)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowIndex, -1);

        RowIndex = rowIndex;
    }

    internal DataGridViewRowContextMenuStripNeededEventArgs(int rowIndex, ContextMenuStrip? contextMenuStrip)
        : this(rowIndex)
    {
        ContextMenuStrip = contextMenuStrip;
    }

    public int RowIndex { get; }

    public ContextMenuStrip? ContextMenuStrip { get; set; }
}

public class DataGridViewRowsAddedEventArgs : EventArgs
{
    public DataGridViewRowsAddedEventArgs(int rowIndex, int rowCount)
    {
        RowIndex = rowIndex;
        RowCount = rowCount;
    }

    public int RowIndex { get; }

    public int RowCount { get; }
}

public class DateBoldEventArgs : EventArgs
{
    internal DateBoldEventArgs(DateTime start, int size)
    {
        StartDate = start;
        Size = size;
    }

    public DateTime StartDate { get; }

    public int Size { get; }

    public int[]? DaysToBold { get; set; }
}

public delegate void DateBoldEventHandler(object? sender, DateBoldEventArgs e);

public class GiveFeedbackEventArgs : EventArgs
{

    public GiveFeedbackEventArgs(DragDropEffects effect, bool useDefaultCursors)
        : this(effect, useDefaultCursors, dragImage: default!, cursorOffset: default, useDefaultDragImage: false)
    {
    }

    public GiveFeedbackEventArgs(DragDropEffects effect, bool useDefaultCursors, Bitmap? dragImage, Point cursorOffset, bool useDefaultDragImage)
    {
        Effect = effect;
        UseDefaultCursors = useDefaultCursors;
        DragImage = dragImage;
        CursorOffset = cursorOffset;
        UseDefaultDragImage = useDefaultDragImage;
    }

    public DragDropEffects Effect { get; }

    public bool UseDefaultCursors { get; set; }

    public Bitmap? DragImage { get; set; }

    public Point CursorOffset { get; set; }

    public bool UseDefaultDragImage { get; set; }

    internal GiveFeedbackEventArgs Clone()
    {
        return (GiveFeedbackEventArgs)MemberwiseClone();
    }

    internal bool Equals(GiveFeedbackEventArgs? giveFeedbackEventArgs)
    {
        if (giveFeedbackEventArgs == this)
        {
            return true;
        }

        return giveFeedbackEventArgs is not null
            && giveFeedbackEventArgs.Effect == Effect
            && giveFeedbackEventArgs.UseDefaultCursors == UseDefaultCursors
            && ((giveFeedbackEventArgs.DragImage is null && DragImage is null)
                || (giveFeedbackEventArgs.DragImage is not null && giveFeedbackEventArgs.DragImage.Equals(DragImage)))
            && giveFeedbackEventArgs.CursorOffset.Equals(CursorOffset)
            && giveFeedbackEventArgs.UseDefaultDragImage == UseDefaultDragImage;
    }
}

public delegate void GiveFeedbackEventHandler(object? sender, GiveFeedbackEventArgs e);

public class HelpEventArgs : EventArgs
{

    public HelpEventArgs(Point mousePos)
    {
        MousePos = mousePos;
    }

    public Point MousePos { get; }

    public bool Handled { get; set; }
}

public delegate void HelpEventHandler(object? sender, HelpEventArgs hlpevent);

public sealed class HtmlElementErrorEventArgs : EventArgs
{
    private readonly string _urlString;
    private Uri? _url;

    internal HtmlElementErrorEventArgs(string description, string urlString, int lineNumber)
    {
        Description = description;
        _urlString = urlString;
        LineNumber = lineNumber;
    }

    public string Description { get; }

    public bool Handled { get; set; }

    public int LineNumber { get; }

    public Uri Url => _url ??= new Uri(_urlString);
}

public delegate void HtmlElementErrorEventHandler(object? sender, HtmlElementErrorEventArgs e);

public class InvalidateEventArgs : EventArgs
{

    public InvalidateEventArgs(Rectangle invalidRect)
    {
        InvalidRect = invalidRect;
    }

    public Rectangle InvalidRect { get; }
}

public delegate void InvalidateEventHandler(object? sender, InvalidateEventArgs e);

public class ItemChangedEventArgs : EventArgs
{
    internal ItemChangedEventArgs(int index)
    {
        Index = index;
    }

    public int Index { get; }
}

public delegate void ItemChangedEventHandler(object? sender, ItemChangedEventArgs e);

public class ItemDragEventArgs : EventArgs
{
    public ItemDragEventArgs(MouseButtons button)
        : this(button, null)
    {
    }

    public ItemDragEventArgs(MouseButtons button, object? item)
    {
        Button = button;
        Item = item;
    }

    public MouseButtons Button { get; }

    public object? Item { get; }
}

public delegate void ItemDragEventHandler(object? sender, ItemDragEventArgs e);

public class LabelEditEventArgs : EventArgs
{

    public LabelEditEventArgs(int item) : this(item, null)
    {
    }

    public LabelEditEventArgs(int item, string? label)
    {
        Item = item;
        Label = label;
    }

    public int Item { get; }

    public string? Label { get; }

    public bool CancelEdit { get; set; }
}

public delegate void LabelEditEventHandler(object? sender, LabelEditEventArgs e);

public sealed class LayoutEventArgs : EventArgs
{
    private readonly WeakReference<IComponent>? _affectedComponent;

    public LayoutEventArgs(IComponent? affectedComponent, string? affectedProperty)
    {
        _affectedComponent = affectedComponent is not null ? new(affectedComponent) : null;
        AffectedProperty = affectedProperty;
    }

    public LayoutEventArgs(Control? affectedControl, string? affectedProperty)
        : this((IComponent?)affectedControl, affectedProperty)
    {
    }

    public IComponent? AffectedComponent
    {
        get
        {
            IComponent? target = null;
            _affectedComponent?.TryGetTarget(out target);
            return target;
        }
    }

    public Control? AffectedControl => AffectedComponent as Control;

    public string? AffectedProperty { get; }
}

public delegate void LayoutEventHandler(object? sender, LayoutEventArgs e);

public class LinkClickedEventArgs : EventArgs
{

    public LinkClickedEventArgs(string? linkText) : this(linkText, 0, 0) { }

    public LinkClickedEventArgs(string? linkText, int linkStart, int linkLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(linkStart);

        if (linkLength < 0 || linkStart + linkLength < 0)
            throw new ArgumentOutOfRangeException(nameof(linkLength));

        LinkText = linkText;
        LinkStart = linkStart;
        LinkLength = linkLength;
    }

    public int LinkLength { get; }

    public int LinkStart { get; }

    public string? LinkText { get; }
}

public delegate void LinkClickedEventHandler(object? sender, LinkClickedEventArgs e);

public class ListControlConvertEventArgs : ConvertEventArgs
{
    public ListControlConvertEventArgs(object? value, Type? desiredType, object? listItem)
        : base(value, desiredType)
    {
        ListItem = listItem;
    }

    public object? ListItem { get; }
}

public delegate void ListControlConvertEventHandler(object? sender, ListControlConvertEventArgs e);

public class ListViewGroupEventArgs : EventArgs
{

    public ListViewGroupEventArgs(int groupIndex)
    {
        GroupIndex = groupIndex;
    }

    public int GroupIndex { get; }
}

public class ListViewItemMouseHoverEventArgs : EventArgs
{
    public ListViewItemMouseHoverEventArgs(ListViewItem? item)
    {
        Item = item;
    }

    public ListViewItem? Item { get; }
}

public delegate void ListViewItemMouseHoverEventHandler(object? sender, ListViewItemMouseHoverEventArgs e);

public class MeasureItemEventArgs : EventArgs
{
    public MeasureItemEventArgs(Graphics graphics, int index)
        : this(graphics, index, 0)
    {
    }

    public MeasureItemEventArgs(Graphics graphics, int index, int itemHeight)
    {
        Graphics = graphics;
        Index = index;
        ItemHeight = itemHeight;
        ItemWidth = 0;
    }

    public Graphics Graphics { get; }

    public int Index { get; }

    public int ItemHeight { get; set; }

    public int ItemWidth { get; set; }
}

public delegate void MeasureItemEventHandler(object? sender, MeasureItemEventArgs e);

public class NavigateEventArgs : EventArgs
{
    public NavigateEventArgs(bool isForward)
    {
        Forward = isForward;
    }

    public bool Forward { get; }
}

public delegate void NavigateEventHandler(object? sender, NavigateEventArgs ne);

public class NodeLabelEditEventArgs : EventArgs
{
    public NodeLabelEditEventArgs(TreeNode? node)
        : this(node, null)
    {
    }

    public NodeLabelEditEventArgs(TreeNode? node, string? label)
    {
        Node = node;
        Label = label;
    }

    public TreeNode? Node { get; }

    public string? Label { get; }

    public bool CancelEdit { get; set; }
}

public delegate void NodeLabelEditEventHandler(object? sender, NodeLabelEditEventArgs e);

public class QueryAccessibilityHelpEventArgs : EventArgs
{
    public QueryAccessibilityHelpEventArgs()
    {
    }

    public QueryAccessibilityHelpEventArgs(string? helpNamespace, string? helpString, string? helpKeyword)
    {
        HelpNamespace = helpNamespace;
        HelpString = helpString;
        HelpKeyword = helpKeyword;
    }

    public string? HelpNamespace { get; set; }

    public string? HelpString { get; set; }

    public string? HelpKeyword { get; set; }
}

public delegate void QueryAccessibilityHelpEventHandler(object? sender, QueryAccessibilityHelpEventArgs e);

public class QueryContinueDragEventArgs : EventArgs
{

    public QueryContinueDragEventArgs(int keyState, bool escapePressed, DragAction action)
    {
        KeyState = keyState;
        EscapePressed = escapePressed;
        Action = action;
    }

    public int KeyState { get; }

    public bool EscapePressed { get; }

    public DragAction Action { get; set; }
}

public delegate void QueryContinueDragEventHandler(object? sender, QueryContinueDragEventArgs e);

public class QuestionEventArgs : EventArgs
{
    public QuestionEventArgs()
    {
    }

    public QuestionEventArgs(bool response)
    {
        Response = response;
    }

    public bool Response { get; set; }
}

public delegate void QuestionEventHandler(object? sender, QuestionEventArgs e);

public class RetrieveVirtualItemEventArgs : EventArgs
{
    public RetrieveVirtualItemEventArgs(int itemIndex)
    {
        ItemIndex = itemIndex;
    }

    public int ItemIndex { get; }

    public ListViewItem? Item { get; set; }
}

public delegate void RetrieveVirtualItemEventHandler(object? sender, RetrieveVirtualItemEventArgs e);

public class SearchForVirtualItemEventArgs : EventArgs
{
    public SearchForVirtualItemEventArgs(
        bool isTextSearch,
        bool isPrefixSearch,
        bool includeSubItemsInSearch,
        string? text,
        Point startingPoint,
        SearchDirectionHint direction,
        int startIndex)
    {
        IsTextSearch = isTextSearch;
        IsPrefixSearch = isPrefixSearch;
        IncludeSubItemsInSearch = includeSubItemsInSearch;
        Text = text;
        StartingPoint = startingPoint;
        Direction = direction;
        StartIndex = startIndex;
    }

    public bool IsTextSearch { get; }

    public bool IsPrefixSearch { get; }

    public bool IncludeSubItemsInSearch { get; }

    public int Index { get; set; } = -1;

    public string? Text { get; }

    public Point StartingPoint { get; }

    public SearchDirectionHint Direction { get; }

    public int StartIndex { get; }
}

public delegate void SearchForVirtualItemEventHandler(object? sender, SearchForVirtualItemEventArgs e);

public class TabControlCancelEventArgs : CancelEventArgs
{
    public TabControlCancelEventArgs(
        TabPage? tabPage,
        int tabPageIndex,
        bool cancel,
        TabControlAction action)
        : base(cancel)
    {
        TabPage = tabPage;
        TabPageIndex = tabPageIndex;
        Action = action;
    }

    public TabPage? TabPage { get; }

    public int TabPageIndex { get; }

    public TabControlAction Action { get; }
}

public delegate void TabControlCancelEventHandler(object? sender, TabControlCancelEventArgs e);

public class TabControlEventArgs : EventArgs
{

    public TabControlEventArgs(TabPage? tabPage, int tabPageIndex, TabControlAction action)
    {
        TabPage = tabPage;
        TabPageIndex = tabPageIndex;
        Action = action;
    }

    public TabPage? TabPage { get; }

    public int TabPageIndex { get; }

    public TabControlAction Action { get; }
}

public delegate void TabControlEventHandler(object? sender, TabControlEventArgs e);

public class ToolStripDropDownClosingEventArgs : CancelEventArgs
{
    public ToolStripDropDownClosingEventArgs(ToolStripDropDownCloseReason reason)
    {
        CloseReason = reason;
    }

    public ToolStripDropDownCloseReason CloseReason { get; }
}

public delegate void ToolStripDropDownClosingEventHandler(object? sender, ToolStripDropDownClosingEventArgs e);

public class ToolStripItemClickedEventArgs : EventArgs
{

    public ToolStripItemClickedEventArgs(ToolStripItem? clickedItem)
    {
        ClickedItem = clickedItem;
    }

    public ToolStripItem? ClickedItem { get; }
}

public delegate void ToolStripItemClickedEventHandler(object? sender, ToolStripItemClickedEventArgs e);

public class ToolStripItemEventArgs : EventArgs
{
    public ToolStripItemEventArgs(ToolStripItem? item)
    {
        Item = item;
    }

    public ToolStripItem? Item { get; }
}

public delegate void ToolStripItemEventHandler(object? sender, ToolStripItemEventArgs e);

internal class ToolStripLocationCancelEventArgs : CancelEventArgs
{

    public ToolStripLocationCancelEventArgs(Point newLocation, bool value) : base(value)
    {
        NewLocation = newLocation;
    }

    public Point NewLocation { get; }
}

internal delegate void ToolStripLocationCancelEventHandler(object? sender, ToolStripLocationCancelEventArgs e);

public class TreeNodeMouseHoverEventArgs : EventArgs
{
    public TreeNodeMouseHoverEventArgs(TreeNode? node)
    {
        Node = node;
    }

    public TreeNode? Node { get; }
}

public delegate void TreeNodeMouseHoverEventHandler(object? sender, TreeNodeMouseHoverEventArgs e);

public class TreeViewCancelEventArgs : CancelEventArgs
{
    public TreeViewCancelEventArgs(TreeNode? node, bool cancel, TreeViewAction action)
        : base(cancel)
    {
        Node = node;
        Action = action;
    }

    public TreeNode? Node { get; }

    public TreeViewAction Action { get; }
}

public delegate void TreeViewCancelEventHandler(object? sender, TreeViewCancelEventArgs e);

public class UICuesEventArgs : EventArgs
{
    private readonly UICues _uicues;

    public UICuesEventArgs(UICues uicues)
    {
        _uicues = uicues;
    }

    public bool ShowFocus => (_uicues & UICues.ShowFocus) != 0;

    public bool ShowKeyboard => (_uicues & UICues.ShowKeyboard) != 0;

    public bool ChangeFocus => (_uicues & UICues.ChangeFocus) != 0;

    public bool ChangeKeyboard => (_uicues & UICues.ChangeKeyboard) != 0;

    public UICues Changed => (_uicues & UICues.Changed);
}

public delegate void UICuesEventHandler(object? sender, UICuesEventArgs e);

public class UpDownEventArgs : EventArgs
{
    public UpDownEventArgs(int buttonPushed)
    {
        ButtonID = buttonPushed;
    }

    public int ButtonID { get; }
}

public delegate void UpDownEventHandler(object? source, UpDownEventArgs e);

public class WebBrowserProgressChangedEventArgs : EventArgs
{

    public WebBrowserProgressChangedEventArgs(long currentProgress, long maximumProgress)
    {
        CurrentProgress = currentProgress;
        MaximumProgress = maximumProgress;
    }

    public long CurrentProgress { get; }

    public long MaximumProgress { get; }
}

public delegate void WebBrowserProgressChangedEventHandler(object? sender, WebBrowserProgressChangedEventArgs e);
