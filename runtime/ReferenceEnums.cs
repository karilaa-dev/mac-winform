// Generated from Microsoft's .NET Windows Desktop reference pack by tools/ApiInventory.
// See notices/dotnet-winforms.txt.
namespace System.Windows.Forms
{
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AccessibleEvents : global::System.Int32
    {
        @SystemSound = 1,
        @SystemAlert = 2,
        @SystemForeground = 3,
        @SystemMenuStart = 4,
        @SystemMenuEnd = 5,
        @SystemMenuPopupStart = 6,
        @SystemMenuPopupEnd = 7,
        @SystemCaptureStart = 8,
        @SystemCaptureEnd = 9,
        @SystemMoveSizeStart = 10,
        @SystemMoveSizeEnd = 11,
        @SystemContextHelpStart = 12,
        @SystemContextHelpEnd = 13,
        @SystemDragDropStart = 14,
        @SystemDragDropEnd = 15,
        @SystemDialogStart = 16,
        @SystemDialogEnd = 17,
        @SystemScrollingStart = 18,
        @SystemScrollingEnd = 19,
        @SystemSwitchStart = 20,
        @SystemSwitchEnd = 21,
        @SystemMinimizeStart = 22,
        @SystemMinimizeEnd = 23,
        @Create = 32768,
        @Destroy = 32769,
        @Show = 32770,
        @Hide = 32771,
        @Reorder = 32772,
        @Focus = 32773,
        @Selection = 32774,
        @SelectionAdd = 32775,
        @SelectionRemove = 32776,
        @SelectionWithin = 32777,
        @StateChange = 32778,
        @LocationChange = 32779,
        @NameChange = 32780,
        @DescriptionChange = 32781,
        @ValueChange = 32782,
        @ParentChange = 32783,
        @HelpChange = 32784,
        @DefaultActionChange = 32785,
        @AcceleratorChange = 32786,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AccessibleNavigation : global::System.Int32
    {
        @Down = 2,
        @FirstChild = 7,
        @LastChild = 8,
        @Left = 3,
        @Next = 5,
        @Previous = 6,
        @Right = 4,
        @Up = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AccessibleRole : global::System.Int32
    {
        @Default = -1,
        @None = 0,
        @TitleBar = 1,
        @MenuBar = 2,
        @ScrollBar = 3,
        @Grip = 4,
        @Sound = 5,
        @Cursor = 6,
        @Caret = 7,
        @Alert = 8,
        @Window = 9,
        @Client = 10,
        @MenuPopup = 11,
        @MenuItem = 12,
        @ToolTip = 13,
        @Application = 14,
        @Document = 15,
        @Pane = 16,
        @Chart = 17,
        @Dialog = 18,
        @Border = 19,
        @Grouping = 20,
        @Separator = 21,
        @ToolBar = 22,
        @StatusBar = 23,
        @Table = 24,
        @ColumnHeader = 25,
        @RowHeader = 26,
        @Column = 27,
        @Row = 28,
        @Cell = 29,
        @Link = 30,
        @HelpBalloon = 31,
        @Character = 32,
        @List = 33,
        @ListItem = 34,
        @Outline = 35,
        @OutlineItem = 36,
        @PageTab = 37,
        @PropertyPage = 38,
        @Indicator = 39,
        @Graphic = 40,
        @StaticText = 41,
        @Text = 42,
        @PushButton = 43,
        @CheckButton = 44,
        @RadioButton = 45,
        @ComboBox = 46,
        @DropList = 47,
        @ProgressBar = 48,
        @Dial = 49,
        @HotkeyField = 50,
        @Slider = 51,
        @SpinButton = 52,
        @Diagram = 53,
        @Animation = 54,
        @Equation = 55,
        @ButtonDropDown = 56,
        @ButtonMenu = 57,
        @ButtonDropDownGrid = 58,
        @WhiteSpace = 59,
        @PageTabList = 60,
        @Clock = 61,
        @SplitButton = 62,
        @IpAddress = 63,
        @OutlineButton = 64,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum AccessibleSelection : global::System.Int32
    {
        @None = 0,
        @TakeFocus = 1,
        @TakeSelection = 2,
        @ExtendSelection = 4,
        @AddSelection = 8,
        @RemoveSelection = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum AccessibleStates : global::System.Int32
    {
        @None = 0,
        @Unavailable = 1,
        @Selected = 2,
        @Focused = 4,
        @Pressed = 8,
        @Checked = 16,
        @Mixed = 32,
        @Indeterminate = 32,
        @ReadOnly = 64,
        @HotTracked = 128,
        @Default = 256,
        @Expanded = 512,
        @Collapsed = 1024,
        @Busy = 2048,
        @Floating = 4096,
        @Marqueed = 8192,
        @Animated = 16384,
        @Invisible = 32768,
        @Offscreen = 65536,
        @Sizeable = 131072,
        @Moveable = 262144,
        @SelfVoicing = 524288,
        @Focusable = 1048576,
        @Selectable = 2097152,
        @Linked = 4194304,
        @Traversed = 8388608,
        @MultiSelectable = 16777216,
        @ExtSelectable = 33554432,
        @AlertLow = 67108864,
        @AlertMedium = 134217728,
        @AlertHigh = 268435456,
        @Protected = 536870912,
        @HasPopup = 1073741824,
        @Valid = 1073741823,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum Appearance : global::System.Int32
    {
        @Normal = 0,
        @Button = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ArrangeDirection : global::System.Int32
    {
        @Down = 4,
        @Left = 0,
        @Right = 0,
        @Up = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ArrangeStartingPosition : global::System.Int32
    {
        @BottomLeft = 0,
        @BottomRight = 1,
        @Hide = 8,
        @TopLeft = 2,
        @TopRight = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ArrowDirection : global::System.Int32
    {
        @Up = 1,
        @Down = 17,
        @Left = 0,
        @Right = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutoCompleteMode : global::System.Int32
    {
        @None = 0,
        @Suggest = 1,
        @Append = 2,
        @SuggestAppend = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutoCompleteSource : global::System.Int32
    {
        @FileSystem = 1,
        @HistoryList = 2,
        @RecentlyUsedList = 4,
        @AllUrl = 6,
        @AllSystemSources = 7,
        @FileSystemDirectories = 32,
        @CustomSource = 64,
        @None = 128,
        @ListItems = 256,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutoSizeMode : global::System.Int32
    {
        @GrowAndShrink = 0,
        @GrowOnly = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutoValidate : global::System.Int32
    {
        @Disable = 0,
        @EnablePreventFocusChange = 1,
        @EnableAllowFocusChange = 2,
        @Inherit = -1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum BatteryChargeStatus : global::System.Int32
    {
        @High = 1,
        @Low = 2,
        @Critical = 4,
        @Charging = 8,
        @NoSystemBattery = 128,
        @Unknown = 255,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum BootMode : global::System.Int32
    {
        @Normal = 0,
        @FailSafe = 1,
        @FailSafeWithNetwork = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum Border3DSide : global::System.Int32
    {
        @Left = 1,
        @Top = 2,
        @Right = 4,
        @Bottom = 8,
        @Middle = 2048,
        @All = 2063,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum Border3DStyle : global::System.Int32
    {
        @Adjust = 8192,
        @Bump = 9,
        @Etched = 6,
        @Flat = 16394,
        @Raised = 5,
        @RaisedInner = 4,
        @RaisedOuter = 1,
        @Sunken = 10,
        @SunkenInner = 8,
        @SunkenOuter = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum BorderStyle : global::System.Int32
    {
        @None = 0,
        @FixedSingle = 1,
        @Fixed3D = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum BoundsSpecified : global::System.Int32
    {
        @X = 1,
        @Y = 2,
        @Width = 4,
        @Height = 8,
        @Location = 3,
        @Size = 12,
        @All = 15,
        @None = 0,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ButtonBorderStyle : global::System.Int32
    {
        @None = 0,
        @Dotted = 1,
        @Dashed = 2,
        @Solid = 3,
        @Inset = 4,
        @Outset = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ButtonState : global::System.Int32
    {
        @Checked = 1024,
        @Flat = 16384,
        @Inactive = 256,
        @Normal = 0,
        @Pushed = 512,
        @All = 18176,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum CaptionButton : global::System.Int32
    {
        @Close = 0,
        @Help = 4,
        @Maximize = 2,
        @Minimize = 1,
        @Restore = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum CharacterCasing : global::System.Int32
    {
        @Normal = 0,
        @Upper = 1,
        @Lower = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ColorDepth : global::System.Int32
    {
        @Depth4Bit = 4,
        @Depth8Bit = 8,
        @Depth16Bit = 16,
        @Depth24Bit = 24,
        @Depth32Bit = 32,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ColumnHeaderAutoResizeStyle : global::System.Int32
    {
        @None = 0,
        @HeaderSize = 1,
        @ColumnContent = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ControlStyles : global::System.Int32
    {
        @ContainerControl = 1,
        @UserPaint = 2,
        @Opaque = 4,
        @ResizeRedraw = 16,
        @FixedWidth = 32,
        @FixedHeight = 64,
        @StandardClick = 256,
        @Selectable = 512,
        @UserMouse = 1024,
        @SupportsTransparentBackColor = 2048,
        @StandardDoubleClick = 4096,
        @AllPaintingInWmPaint = 8192,
        @CacheText = 16384,
        @EnableNotifyMessage = 32768,
        @DoubleBuffer = 65536,
        @OptimizedDoubleBuffer = 131072,
        @UseTextForAccessibility = 262144,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewAdvancedCellBorderStyle : global::System.Int32
    {
        @NotSet = 0,
        @None = 1,
        @Single = 2,
        @Inset = 3,
        @InsetDouble = 4,
        @Outset = 5,
        @OutsetDouble = 6,
        @OutsetPartial = 7,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewAutoSizeRowMode : global::System.Int32
    {
        @AllCells = 3,
        @AllCellsExceptHeader = 2,
        @RowHeader = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewAutoSizeRowsMode : global::System.Int32
    {
        @AllCells = 7,
        @AllCellsExceptHeaders = 6,
        @AllHeaders = 5,
        @DisplayedCells = 11,
        @DisplayedCellsExceptHeaders = 10,
        @DisplayedHeaders = 9,
        @None = 0,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewCellBorderStyle : global::System.Int32
    {
        @Custom = 0,
        @Single = 1,
        @Raised = 2,
        @Sunken = 3,
        @None = 4,
        @SingleVertical = 5,
        @RaisedVertical = 6,
        @SunkenVertical = 7,
        @SingleHorizontal = 8,
        @RaisedHorizontal = 9,
        @SunkenHorizontal = 10,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum DataGridViewCellStyleScopes : global::System.Int32
    {
        @None = 0,
        @Cell = 1,
        @Column = 2,
        @Row = 4,
        @DataGridView = 8,
        @ColumnHeaders = 16,
        @RowHeaders = 32,
        @Rows = 64,
        @AlternatingRows = 128,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewClipboardCopyMode : global::System.Int32
    {
        @Disable = 0,
        @EnableWithAutoHeaderText = 1,
        @EnableWithoutHeaderText = 2,
        @EnableAlwaysIncludeHeaderText = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewColumnSortMode : global::System.Int32
    {
        @NotSortable = 0,
        @Automatic = 1,
        @Programmatic = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewComboBoxDisplayStyle : global::System.Int32
    {
        @ComboBox = 0,
        @DropDownButton = 1,
        @Nothing = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewContentAlignment : global::System.Int32
    {
        @NotSet = 0,
        @TopLeft = 1,
        @TopCenter = 2,
        @TopRight = 4,
        @MiddleLeft = 16,
        @MiddleCenter = 32,
        @MiddleRight = 64,
        @BottomLeft = 256,
        @BottomCenter = 512,
        @BottomRight = 1024,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewEditMode : global::System.Int32
    {
        @EditOnEnter = 0,
        @EditOnKeystroke = 1,
        @EditOnKeystrokeOrF2 = 2,
        @EditOnF2 = 3,
        @EditProgrammatically = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum DataGridViewElementStates : global::System.Int32
    {
        @None = 0,
        @Displayed = 1,
        @Frozen = 2,
        @ReadOnly = 4,
        @Resizable = 8,
        @ResizableSet = 16,
        @Selected = 32,
        @Visible = 64,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewHeaderBorderStyle : global::System.Int32
    {
        @Custom = 0,
        @Single = 1,
        @Raised = 2,
        @Sunken = 3,
        @None = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewHitTestType : global::System.Int32
    {
        @None = 0,
        @Cell = 1,
        @ColumnHeader = 2,
        @RowHeader = 3,
        @TopLeftHeader = 4,
        @HorizontalScrollBar = 5,
        @VerticalScrollBar = 6,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewImageCellLayout : global::System.Int32
    {
        @NotSet = 0,
        @Normal = 1,
        @Stretch = 2,
        @Zoom = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum DataGridViewPaintParts : global::System.Int32
    {
        @None = 0,
        @All = 127,
        @Background = 1,
        @Border = 2,
        @ContentBackground = 4,
        @ContentForeground = 8,
        @ErrorIcon = 16,
        @Focus = 32,
        @SelectionBackground = 64,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewRowHeadersWidthSizeMode : global::System.Int32
    {
        @EnableResizing = 0,
        @DisableResizing = 1,
        @AutoSizeToAllHeaders = 2,
        @AutoSizeToDisplayedHeaders = 3,
        @AutoSizeToFirstHeader = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DataGridViewTriState : global::System.Int32
    {
        @NotSet = 0,
        @True = 1,
        @False = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DateTimePickerFormat : global::System.Int32
    {
        @Long = 1,
        @Short = 2,
        @Time = 4,
        @Custom = 8,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum Day : global::System.Int32
    {
        @Monday = 0,
        @Tuesday = 1,
        @Wednesday = 2,
        @Thursday = 3,
        @Friday = 4,
        @Saturday = 5,
        @Sunday = 6,
        @Default = 7,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DockingBehavior : global::System.Int32
    {
        @Never = 0,
        @Ask = 1,
        @AutoDock = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DragAction : global::System.Int32
    {
        @Continue = 0,
        @Drop = 1,
        @Cancel = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum DrawItemState : global::System.Int32
    {
        @Checked = 8,
        @ComboBoxEdit = 4096,
        @Default = 32,
        @Disabled = 4,
        @Focus = 16,
        @Grayed = 2,
        @HotLight = 64,
        @Inactive = 128,
        @NoAccelerator = 256,
        @NoFocusRect = 512,
        @Selected = 1,
        @None = 0,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum DrawMode : global::System.Int32
    {
        @Normal = 0,
        @OwnerDrawFixed = 1,
        @OwnerDrawVariable = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ErrorBlinkStyle : global::System.Int32
    {
        @BlinkIfDifferentError = 0,
        @AlwaysBlink = 1,
        @NeverBlink = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ErrorIconAlignment : global::System.Int32
    {
        @TopLeft = 0,
        @TopRight = 1,
        @MiddleLeft = 2,
        @MiddleRight = 3,
        @BottomLeft = 4,
        @BottomRight = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FlatStyle : global::System.Int32
    {
        @Flat = 0,
        @Popup = 1,
        @Standard = 2,
        @System = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FlowDirection : global::System.Int32
    {
        @LeftToRight = 0,
        @TopDown = 1,
        @RightToLeft = 2,
        @BottomUp = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FormWindowState : global::System.Int32
    {
        @Normal = 0,
        @Minimized = 1,
        @Maximized = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FrameStyle : global::System.Int32
    {
        @Dashed = 0,
        @Thick = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum GetChildAtPointSkip : global::System.Int32
    {
        @None = 0,
        @Invisible = 1,
        @Disabled = 2,
        @Transparent = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum HelpNavigator : global::System.Int32
    {
        @Topic = -2147483647,
        @TableOfContents = -2147483646,
        @Index = -2147483645,
        @Find = -2147483644,
        @AssociateIndex = -2147483643,
        @KeywordIndex = -2147483642,
        @TopicId = -2147483641,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum HtmlElementInsertionOrientation : global::System.Int32
    {
        @BeforeBegin = 0,
        @AfterBegin = 1,
        @BeforeEnd = 2,
        @AfterEnd = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ImageLayout : global::System.Int32
    {
        @None = 0,
        @Tile = 1,
        @Center = 2,
        @Stretch = 3,
        @Zoom = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ImeMode : global::System.Int32
    {
        @Inherit = -1,
        @NoControl = 0,
        @On = 1,
        @Off = 2,
        @Disable = 3,
        @Hiragana = 4,
        @Katakana = 5,
        @KatakanaHalf = 6,
        @AlphaFull = 7,
        @Alpha = 8,
        @HangulFull = 9,
        @Hangul = 10,
        @Close = 11,
        @OnHalf = 12,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ItemActivation : global::System.Int32
    {
        @Standard = 0,
        @OneClick = 1,
        @TwoClick = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ItemBoundsPortion : global::System.Int32
    {
        @Entire = 0,
        @Icon = 1,
        @Label = 2,
        @ItemOnly = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum LinkState : global::System.Int32
    {
        @Normal = 0,
        @Hover = 1,
        @Active = 2,
        @Visited = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ListViewAlignment : global::System.Int32
    {
        @Default = 0,
        @Top = 2,
        @Left = 1,
        @SnapToGrid = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ListViewGroupCollapsedState : global::System.Int32
    {
        @Default = 0,
        @Expanded = 1,
        @Collapsed = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ListViewHitTestLocations : global::System.Int32
    {
        @None = 1,
        @AboveClientArea = 256,
        @BelowClientArea = 16,
        @LeftOfClientArea = 64,
        @RightOfClientArea = 32,
        @Image = 2,
        @StateImage = 512,
        @Label = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ListViewItemStates : global::System.Int32
    {
        @Checked = 8,
        @Default = 32,
        @Focused = 16,
        @Grayed = 2,
        @Hot = 64,
        @Indeterminate = 256,
        @Marked = 128,
        @Selected = 1,
        @ShowKeyboardCues = 512,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum MenuGlyph : global::System.Int32
    {
        @Arrow = 0,
        @Checkmark = 1,
        @Bullet = 2,
        @Min = 0,
        @Max = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum MergeAction : global::System.Int32
    {
        @Append = 0,
        @Insert = 1,
        @Replace = 2,
        @Remove = 3,
        @MatchOnly = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum MessageBoxDefaultButton : global::System.Int32
    {
        @Button1 = 0,
        @Button2 = 256,
        @Button3 = 512,
        @Button4 = 768,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum MessageBoxOptions : global::System.Int32
    {
        @ServiceNotification = 2097152,
        @DefaultDesktopOnly = 131072,
        @RightAlign = 524288,
        @RtlReading = 1048576,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum PowerLineStatus : global::System.Int32
    {
        @Offline = 0,
        @Online = 1,
        @Unknown = 255,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum PowerState : global::System.Int32
    {
        @Suspend = 0,
        @Hibernate = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum PreProcessControlState : global::System.Int32
    {
        @MessageProcessed = 0,
        @MessageNeeded = 1,
        @MessageNotNeeded = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum RichTextBoxFinds : global::System.Int32
    {
        @None = 0,
        @WholeWord = 2,
        @MatchCase = 4,
        @NoHighlight = 8,
        @Reverse = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum RichTextBoxLanguageOptions : global::System.Int32
    {
        @AutoFont = 2,
        @AutoFontSizeAdjust = 16,
        @AutoKeyboard = 1,
        @DualFont = 128,
        @ImeAlwaysSendNotify = 8,
        @ImeCancelComplete = 4,
        @UIFonts = 32,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum RichTextBoxScrollBars : global::System.Int32
    {
        @None = 0,
        @Horizontal = 1,
        @Vertical = 2,
        @Both = 3,
        @ForcedHorizontal = 17,
        @ForcedVertical = 18,
        @ForcedBoth = 19,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum RichTextBoxSelectionAttribute : global::System.Int32
    {
        @Mixed = -1,
        @None = 0,
        @All = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum RichTextBoxSelectionTypes : global::System.Int32
    {
        @Empty = 0,
        @Text = 1,
        @Object = 2,
        @MultiChar = 4,
        @MultiObject = 8,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum RichTextBoxWordPunctuations : global::System.Int32
    {
        @Level1 = 128,
        @Level2 = 256,
        @Custom = 512,
        @All = 896,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum RightToLeft : global::System.Int32
    {
        @No = 0,
        @Yes = 1,
        @Inherit = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScreenOrientation : global::System.Int32
    {
        @Angle0 = 0,
        @Angle90 = 1,
        @Angle180 = 2,
        @Angle270 = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScrollBars : global::System.Int32
    {
        @None = 0,
        @Horizontal = 1,
        @Vertical = 2,
        @Both = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScrollButton : global::System.Int32
    {
        @Down = 1,
        @Left = 2,
        @Right = 3,
        @Up = 0,
        @Min = 0,
        @Max = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SearchDirectionHint : global::System.Int32
    {
        @Up = 38,
        @Down = 40,
        @Left = 37,
        @Right = 39,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SecurityIDType : global::System.Int32
    {
        @User = 1,
        @Group = 2,
        @Domain = 3,
        @Alias = 4,
        @WellKnownGroup = 5,
        @DeletedAccount = 6,
        @Invalid = 7,
        @Unknown = 8,
        @Computer = 9,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SelectionMode : global::System.Int32
    {
        @None = 0,
        @One = 1,
        @MultiSimple = 2,
        @MultiExtended = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum Shortcut : global::System.Int32
    {
        @None = 0,
        @CtrlA = 131137,
        @CtrlB = 131138,
        @CtrlC = 131139,
        @CtrlD = 131140,
        @CtrlE = 131141,
        @CtrlF = 131142,
        @CtrlG = 131143,
        @CtrlH = 131144,
        @CtrlI = 131145,
        @CtrlJ = 131146,
        @CtrlK = 131147,
        @CtrlL = 131148,
        @CtrlM = 131149,
        @CtrlN = 131150,
        @CtrlO = 131151,
        @CtrlP = 131152,
        @CtrlQ = 131153,
        @CtrlR = 131154,
        @CtrlS = 131155,
        @CtrlT = 131156,
        @CtrlU = 131157,
        @CtrlV = 131158,
        @CtrlW = 131159,
        @CtrlX = 131160,
        @CtrlY = 131161,
        @CtrlZ = 131162,
        @CtrlShiftA = 196673,
        @CtrlShiftB = 196674,
        @CtrlShiftC = 196675,
        @CtrlShiftD = 196676,
        @CtrlShiftE = 196677,
        @CtrlShiftF = 196678,
        @CtrlShiftG = 196679,
        @CtrlShiftH = 196680,
        @CtrlShiftI = 196681,
        @CtrlShiftJ = 196682,
        @CtrlShiftK = 196683,
        @CtrlShiftL = 196684,
        @CtrlShiftM = 196685,
        @CtrlShiftN = 196686,
        @CtrlShiftO = 196687,
        @CtrlShiftP = 196688,
        @CtrlShiftQ = 196689,
        @CtrlShiftR = 196690,
        @CtrlShiftS = 196691,
        @CtrlShiftT = 196692,
        @CtrlShiftU = 196693,
        @CtrlShiftV = 196694,
        @CtrlShiftW = 196695,
        @CtrlShiftX = 196696,
        @CtrlShiftY = 196697,
        @CtrlShiftZ = 196698,
        @F1 = 112,
        @F2 = 113,
        @F3 = 114,
        @F4 = 115,
        @F5 = 116,
        @F6 = 117,
        @F7 = 118,
        @F8 = 119,
        @F9 = 120,
        @F10 = 121,
        @F11 = 122,
        @F12 = 123,
        @ShiftF1 = 65648,
        @ShiftF2 = 65649,
        @ShiftF3 = 65650,
        @ShiftF4 = 65651,
        @ShiftF5 = 65652,
        @ShiftF6 = 65653,
        @ShiftF7 = 65654,
        @ShiftF8 = 65655,
        @ShiftF9 = 65656,
        @ShiftF10 = 65657,
        @ShiftF11 = 65658,
        @ShiftF12 = 65659,
        @CtrlF1 = 131184,
        @CtrlF2 = 131185,
        @CtrlF3 = 131186,
        @CtrlF4 = 131187,
        @CtrlF5 = 131188,
        @CtrlF6 = 131189,
        @CtrlF7 = 131190,
        @CtrlF8 = 131191,
        @CtrlF9 = 131192,
        @CtrlF10 = 131193,
        @CtrlF11 = 131194,
        @CtrlF12 = 131195,
        @CtrlShiftF1 = 196720,
        @CtrlShiftF2 = 196721,
        @CtrlShiftF3 = 196722,
        @CtrlShiftF4 = 196723,
        @CtrlShiftF5 = 196724,
        @CtrlShiftF6 = 196725,
        @CtrlShiftF7 = 196726,
        @CtrlShiftF8 = 196727,
        @CtrlShiftF9 = 196728,
        @CtrlShiftF10 = 196729,
        @CtrlShiftF11 = 196730,
        @CtrlShiftF12 = 196731,
        @Ins = 45,
        @CtrlIns = 131117,
        @ShiftIns = 65581,
        @Del = 46,
        @CtrlDel = 131118,
        @ShiftDel = 65582,
        @AltRightArrow = 262183,
        @AltLeftArrow = 262181,
        @AltUpArrow = 262182,
        @AltDownArrow = 262184,
        @AltBksp = 262152,
        @AltF1 = 262256,
        @AltF2 = 262257,
        @AltF3 = 262258,
        @AltF4 = 262259,
        @AltF5 = 262260,
        @AltF6 = 262261,
        @AltF7 = 262262,
        @AltF8 = 262263,
        @AltF9 = 262264,
        @AltF10 = 262265,
        @AltF11 = 262266,
        @AltF12 = 262267,
        @Alt0 = 262192,
        @Alt1 = 262193,
        @Alt2 = 262194,
        @Alt3 = 262195,
        @Alt4 = 262196,
        @Alt5 = 262197,
        @Alt6 = 262198,
        @Alt7 = 262199,
        @Alt8 = 262200,
        @Alt9 = 262201,
        @Ctrl0 = 131120,
        @Ctrl1 = 131121,
        @Ctrl2 = 131122,
        @Ctrl3 = 131123,
        @Ctrl4 = 131124,
        @Ctrl5 = 131125,
        @Ctrl6 = 131126,
        @Ctrl7 = 131127,
        @Ctrl8 = 131128,
        @Ctrl9 = 131129,
        @CtrlShift0 = 196656,
        @CtrlShift1 = 196657,
        @CtrlShift2 = 196658,
        @CtrlShift3 = 196659,
        @CtrlShift4 = 196660,
        @CtrlShift5 = 196661,
        @CtrlShift6 = 196662,
        @CtrlShift7 = 196663,
        @CtrlShift8 = 196664,
        @CtrlShift9 = 196665,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SizeGripStyle : global::System.Int32
    {
        @Auto = 0,
        @Show = 1,
        @Hide = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum StructFormat : global::System.Int32
    {
        @Ansi = 1,
        @Unicode = 2,
        @Auto = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SystemParameter : global::System.Int32
    {
        @DropShadow = 0,
        @FlatMenu = 1,
        @FontSmoothingContrastMetric = 2,
        @FontSmoothingTypeMetric = 3,
        @MenuFadeEnabled = 4,
        @SelectionFade = 5,
        @ToolTipAnimationMetric = 6,
        @UIEffects = 7,
        @CaretWidthMetric = 8,
        @VerticalFocusThicknessMetric = 9,
        @HorizontalFocusThicknessMetric = 10,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabAlignment : global::System.Int32
    {
        @Top = 0,
        @Bottom = 1,
        @Left = 2,
        @Right = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabAppearance : global::System.Int32
    {
        @Normal = 0,
        @Buttons = 1,
        @FlatButtons = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabControlAction : global::System.Int32
    {
        @Selecting = 0,
        @Selected = 1,
        @Deselecting = 2,
        @Deselected = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabDrawMode : global::System.Int32
    {
        @Normal = 0,
        @OwnerDrawFixed = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TableLayoutPanelCellBorderStyle : global::System.Int32
    {
        @None = 0,
        @Single = 1,
        @Inset = 2,
        @InsetDouble = 3,
        @Outset = 4,
        @OutsetDouble = 5,
        @OutsetPartial = 6,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TableLayoutPanelGrowStyle : global::System.Int32
    {
        @FixedSize = 0,
        @AddRows = 1,
        @AddColumns = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabSizeMode : global::System.Int32
    {
        @Normal = 0,
        @FillToRight = 1,
        @Fixed = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TaskDialogExpanderPosition : global::System.Int32
    {
        @AfterText = 0,
        @AfterFootnote = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TaskDialogProgressBarState : global::System.Int32
    {
        @Normal = 0,
        @Paused = 1,
        @Error = 2,
        @Marquee = 3,
        @MarqueePaused = 4,
        @None = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TaskDialogStartupLocation : global::System.Int32
    {
        @CenterScreen = 0,
        @CenterOwner = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum TextFormatFlags : global::System.Int32
    {
        @Bottom = 8,
        @EndEllipsis = 32768,
        @ExpandTabs = 64,
        @ExternalLeading = 512,
        @Default = 0,
        @HidePrefix = 1048576,
        @HorizontalCenter = 1,
        @Internal = 4096,
        @Left = 0,
        @ModifyString = 65536,
        @NoClipping = 256,
        @NoPrefix = 2048,
        @NoFullWidthCharacterBreak = 524288,
        @PathEllipsis = 16384,
        @PrefixOnly = 2097152,
        @Right = 2,
        @RightToLeft = 131072,
        @SingleLine = 32,
        @TextBoxControl = 8192,
        @Top = 0,
        @VerticalCenter = 4,
        @WordBreak = 16,
        @WordEllipsis = 262144,
        @PreserveGraphicsClipping = 16777216,
        @PreserveGraphicsTranslateTransform = 33554432,
        @GlyphOverhangPadding = 0,
        @NoPadding = 268435456,
        @LeftAndRightPadding = 536870912,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TextImageRelation : global::System.Int32
    {
        @Overlay = 0,
        @ImageBeforeText = 4,
        @TextBeforeImage = 8,
        @ImageAboveText = 1,
        @TextAboveImage = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripDropDownDirection : global::System.Int32
    {
        @AboveLeft = 0,
        @AboveRight = 1,
        @BelowLeft = 2,
        @BelowRight = 3,
        @Left = 4,
        @Right = 5,
        @Default = 7,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripGripDisplayStyle : global::System.Int32
    {
        @Horizontal = 0,
        @Vertical = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripGripStyle : global::System.Int32
    {
        @Hidden = 0,
        @Visible = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripItemAlignment : global::System.Int32
    {
        @Left = 0,
        @Right = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripItemImageScaling : global::System.Int32
    {
        @None = 0,
        @SizeToFit = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripItemOverflow : global::System.Int32
    {
        @Never = 0,
        @Always = 1,
        @AsNeeded = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripItemPlacement : global::System.Int32
    {
        @Main = 0,
        @Overflow = 1,
        @None = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripLayoutStyle : global::System.Int32
    {
        @StackWithOverflow = 0,
        @HorizontalStackWithOverflow = 1,
        @VerticalStackWithOverflow = 2,
        @Flow = 3,
        @Table = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripManagerRenderMode : global::System.Int32
    {
        @Custom = 0,
        @System = 1,
        @Professional = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripRenderMode : global::System.Int32
    {
        @Custom = 0,
        @System = 1,
        @Professional = 2,
        @ManagerRenderMode = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ToolStripStatusLabelBorderSides : global::System.Int32
    {
        @All = 15,
        @Bottom = 8,
        @Left = 1,
        @Right = 4,
        @Top = 2,
        @None = 0,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolStripTextDirection : global::System.Int32
    {
        @Inherit = 0,
        @Horizontal = 1,
        @Vertical90 = 2,
        @Vertical270 = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolTipIcon : global::System.Int32
    {
        @None = 0,
        @Info = 1,
        @Warning = 2,
        @Error = 3,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum TreeNodeStates : global::System.Int32
    {
        @Checked = 8,
        @Default = 32,
        @Focused = 16,
        @Grayed = 2,
        @Hot = 64,
        @Indeterminate = 256,
        @Marked = 128,
        @Selected = 1,
        @ShowKeyboardCues = 512,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TreeViewDrawMode : global::System.Int32
    {
        @Normal = 0,
        @OwnerDrawText = 1,
        @OwnerDrawAll = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum TreeViewHitTestLocations : global::System.Int32
    {
        @None = 1,
        @Image = 2,
        @Label = 4,
        @Indent = 8,
        @AboveClientArea = 256,
        @BelowClientArea = 512,
        @LeftOfClientArea = 2048,
        @RightOfClientArea = 1024,
        @RightOfLabel = 32,
        @StateImage = 64,
        @PlusMinus = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum UICues : global::System.Int32
    {
        @ShowFocus = 1,
        @ShowKeyboard = 2,
        @Shown = 3,
        @ChangeFocus = 4,
        @ChangeKeyboard = 8,
        @Changed = 12,
        @None = 0,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum UnhandledExceptionMode : global::System.Int32
    {
        @Automatic = 0,
        @ThrowException = 1,
        @CatchException = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ValidationConstraints : global::System.Int32
    {
        @None = 0,
        @Selectable = 1,
        @Enabled = 2,
        @Visible = 4,
        @TabStop = 8,
        @ImmediateChildren = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum WebBrowserEncryptionLevel : global::System.Int32
    {
        @Insecure = 0,
        @Mixed = 1,
        @Unknown = 2,
        @Bit40 = 3,
        @Bit56 = 4,
        @Fortezza = 5,
        @Bit128 = 6,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum WebBrowserRefreshOption : global::System.Int32
    {
        @Normal = 0,
        @IfExpired = 1,
        @Continue = 2,
        @Completely = 3,
    }
}
namespace System.Windows.Forms.Automation
{
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutomationLiveSetting : global::System.Int32
    {
        @Off = 0,
        @Polite = 1,
        @Assertive = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutomationNotificationKind : global::System.Int32
    {
        @ItemAdded = 0,
        @ItemRemoved = 1,
        @ActionCompleted = 2,
        @ActionAborted = 3,
        @Other = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum AutomationNotificationProcessing : global::System.Int32
    {
        @ImportantAll = 0,
        @ImportantMostRecent = 1,
        @All = 2,
        @MostRecent = 3,
        @CurrentThenMostRecent = 4,
    }
}
namespace System.Windows.Forms.Design
{
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum ToolStripItemDesignerAvailability : global::System.Int32
    {
        @None = 0,
        @ToolStrip = 1,
        @MenuStrip = 2,
        @ContextMenuStrip = 4,
        @StatusStrip = 8,
        @All = 15,
    }
}
namespace System.Windows.Forms.VisualStyles
{
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum BackgroundType : global::System.Int32
    {
        @ImageFile = 0,
        @BorderFill = 1,
        @None = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum BooleanProperty : global::System.Int32
    {
        @Transparent = 2201,
        @AutoSize = 2202,
        @BorderOnly = 2203,
        @Composited = 2204,
        @BackgroundFill = 2205,
        @GlyphTransparent = 2206,
        @GlyphOnly = 2207,
        @AlwaysShowSizingBar = 2208,
        @MirrorImage = 2209,
        @UniformSizing = 2210,
        @IntegralSizing = 2211,
        @SourceGrow = 2212,
        @SourceShrink = 2213,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum BorderType : global::System.Int32
    {
        @Rectangle = 0,
        @RoundedRectangle = 1,
        @Ellipse = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum CheckBoxState : global::System.Int32
    {
        @UncheckedNormal = 1,
        @UncheckedHot = 2,
        @UncheckedPressed = 3,
        @UncheckedDisabled = 4,
        @CheckedNormal = 5,
        @CheckedHot = 6,
        @CheckedPressed = 7,
        @CheckedDisabled = 8,
        @MixedNormal = 9,
        @MixedHot = 10,
        @MixedPressed = 11,
        @MixedDisabled = 12,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ColorProperty : global::System.Int32
    {
        @BorderColor = 3801,
        @FillColor = 3802,
        @TextColor = 3803,
        @EdgeLightColor = 3804,
        @EdgeHighlightColor = 3805,
        @EdgeShadowColor = 3806,
        @EdgeDarkShadowColor = 3807,
        @EdgeFillColor = 3808,
        @TransparentColor = 3809,
        @GradientColor1 = 3810,
        @GradientColor2 = 3811,
        @GradientColor3 = 3812,
        @GradientColor4 = 3813,
        @GradientColor5 = 3814,
        @ShadowColor = 3815,
        @GlowColor = 3816,
        @TextBorderColor = 3817,
        @TextShadowColor = 3818,
        @GlyphTextColor = 3819,
        @GlyphTransparentColor = 3820,
        @FillColorHint = 3821,
        @BorderColorHint = 3822,
        @AccentColorHint = 3823,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ComboBoxState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Pressed = 3,
        @Disabled = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ContentAlignment : global::System.Int32
    {
        @Left = 0,
        @Center = 1,
        @Right = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum EdgeEffects : global::System.Int32
    {
        @None = 0,
        @FillInterior = 2048,
        @Flat = 16384,
        @Soft = 4096,
        @Mono = 32768,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum Edges : global::System.Int32
    {
        @Left = 1,
        @Top = 2,
        @Right = 4,
        @Bottom = 8,
        @Diagonal = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum EdgeStyle : global::System.Int32
    {
        @Raised = 5,
        @Sunken = 10,
        @Etched = 6,
        @Bump = 9,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum EnumProperty : global::System.Int32
    {
        @BackgroundType = 4001,
        @BorderType = 4002,
        @FillType = 4003,
        @SizingType = 4004,
        @HorizontalAlignment = 4005,
        @ContentAlignment = 4006,
        @VerticalAlignment = 4007,
        @OffsetType = 4008,
        @IconEffect = 4009,
        @TextShadowType = 4010,
        @ImageLayout = 4011,
        @GlyphType = 4012,
        @ImageSelectType = 4013,
        @GlyphFontSizingType = 4014,
        @TrueSizeScalingType = 4015,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FilenameProperty : global::System.Int32
    {
        @ImageFile = 3001,
        @ImageFile1 = 3002,
        @ImageFile2 = 3003,
        @ImageFile3 = 3004,
        @ImageFile4 = 3005,
        @ImageFile5 = 3006,
        @StockImageFile = 3007,
        @GlyphImageFile = 3008,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FillType : global::System.Int32
    {
        @Solid = 0,
        @VerticalGradient = 1,
        @HorizontalGradient = 2,
        @RadialGradient = 3,
        @TileImage = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum FontProperty : global::System.Int32
    {
        @TextFont = 210,
        @GlyphFont = 2601,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum GlyphFontSizingType : global::System.Int32
    {
        @None = 0,
        @Size = 1,
        @Dpi = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum GlyphType : global::System.Int32
    {
        @None = 0,
        @ImageGlyph = 1,
        @FontGlyph = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum GroupBoxState : global::System.Int32
    {
        @Normal = 1,
        @Disabled = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum HitTestCode : global::System.Int32
    {
        @Nowhere = 0,
        @Client = 1,
        @Left = 10,
        @Right = 11,
        @Top = 12,
        @Bottom = 15,
        @TopLeft = 13,
        @TopRight = 14,
        @BottomLeft = 16,
        @BottomRight = 17,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum HitTestOptions : global::System.Int32
    {
        @BackgroundSegment = 0,
        @FixedBorder = 2,
        @Caption = 4,
        @ResizingBorderLeft = 16,
        @ResizingBorderTop = 32,
        @ResizingBorderRight = 64,
        @ResizingBorderBottom = 128,
        @ResizingBorder = 240,
        @SizingTemplate = 256,
        @SystemSizingMargins = 512,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum HorizontalAlign : global::System.Int32
    {
        @Left = 0,
        @Center = 1,
        @Right = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum IconEffect : global::System.Int32
    {
        @None = 0,
        @Glow = 1,
        @Shadow = 2,
        @Pulse = 3,
        @Alpha = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ImageOrientation : global::System.Int32
    {
        @Vertical = 0,
        @Horizontal = 1,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ImageSelectType : global::System.Int32
    {
        @None = 0,
        @Size = 1,
        @Dpi = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum IntegerProperty : global::System.Int32
    {
        @ImageCount = 2401,
        @AlphaLevel = 2402,
        @BorderSize = 2403,
        @RoundCornerWidth = 2404,
        @RoundCornerHeight = 2405,
        @GradientRatio1 = 2406,
        @GradientRatio2 = 2407,
        @GradientRatio3 = 2408,
        @GradientRatio4 = 2409,
        @GradientRatio5 = 2410,
        @ProgressChunkSize = 2411,
        @ProgressSpaceSize = 2412,
        @Saturation = 2413,
        @TextBorderSize = 2414,
        @AlphaThreshold = 2415,
        @Width = 2416,
        @Height = 2417,
        @GlyphIndex = 2418,
        @TrueSizeStretchMark = 2419,
        @MinDpi1 = 2420,
        @MinDpi2 = 2421,
        @MinDpi3 = 2422,
        @MinDpi4 = 2423,
        @MinDpi5 = 2424,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum MarginProperty : global::System.Int32
    {
        @SizingMargins = 3601,
        @ContentMargins = 3602,
        @CaptionMargins = 3603,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum OffsetType : global::System.Int32
    {
        @TopLeft = 0,
        @TopRight = 1,
        @TopMiddle = 2,
        @BottomLeft = 3,
        @BottomRight = 4,
        @BottomMiddle = 5,
        @MiddleLeft = 6,
        @MiddleRight = 7,
        @LeftOfCaption = 8,
        @RightOfCaption = 9,
        @LeftOfLastButton = 10,
        @RightOfLastButton = 11,
        @AboveLastButton = 12,
        @BelowLastButton = 13,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum PointProperty : global::System.Int32
    {
        @Offset = 3401,
        @TextShadowOffset = 3402,
        @MinSize = 3403,
        @MinSize1 = 3404,
        @MinSize2 = 3405,
        @MinSize3 = 3406,
        @MinSize4 = 3407,
        @MinSize5 = 3408,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum PushButtonState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Pressed = 3,
        @Disabled = 4,
        @Default = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum RadioButtonState : global::System.Int32
    {
        @UncheckedNormal = 1,
        @UncheckedHot = 2,
        @UncheckedPressed = 3,
        @UncheckedDisabled = 4,
        @CheckedNormal = 5,
        @CheckedHot = 6,
        @CheckedPressed = 7,
        @CheckedDisabled = 8,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScrollBarArrowButtonState : global::System.Int32
    {
        @UpNormal = 1,
        @UpHot = 2,
        @UpPressed = 3,
        @UpDisabled = 4,
        @DownNormal = 5,
        @DownHot = 6,
        @DownPressed = 7,
        @DownDisabled = 8,
        @LeftNormal = 9,
        @LeftHot = 10,
        @LeftPressed = 11,
        @LeftDisabled = 12,
        @RightNormal = 13,
        @RightHot = 14,
        @RightPressed = 15,
        @RightDisabled = 16,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScrollBarSizeBoxState : global::System.Int32
    {
        @RightAlign = 1,
        @LeftAlign = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ScrollBarState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Pressed = 3,
        @Disabled = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum SizingType : global::System.Int32
    {
        @FixedSize = 0,
        @Stretch = 1,
        @Tile = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum StringProperty : global::System.Int32
    {
        @Text = 3201,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TabItemState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Selected = 3,
        @Disabled = 4,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TextBoxState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Selected = 3,
        @Disabled = 4,
        @Readonly = 6,
        @Assist = 7,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TextMetricsCharacterSet : global::System.Int32
    {
        @Ansi = 0,
        @Baltic = 186,
        @ChineseBig5 = 136,
        @Default = 1,
        @EastEurope = 238,
        @Gb2312 = 134,
        @Greek = 161,
        @Hangul = 129,
        @Mac = 77,
        @Oem = 255,
        @Russian = 204,
        @ShiftJis = 128,
        @Symbol = 2,
        @Turkish = 162,
        @Vietnamese = 163,
        @Johab = 130,
        @Arabic = 178,
        @Hebrew = 177,
        @Thai = 222,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum TextMetricsPitchAndFamilyValues : global::System.Int32
    {
        @FixedPitch = 1,
        @Vector = 2,
        @TrueType = 4,
        @Device = 8,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TextShadowType : global::System.Int32
    {
        @None = 0,
        @Single = 1,
        @Continuous = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ThemeSizeType : global::System.Int32
    {
        @Minimum = 0,
        @True = 1,
        @Draw = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum ToolBarState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Pressed = 3,
        @Disabled = 4,
        @Checked = 5,
        @HotChecked = 6,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TrackBarThumbState : global::System.Int32
    {
        @Normal = 1,
        @Hot = 2,
        @Pressed = 3,
        @Disabled = 5,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum TrueSizeScalingType : global::System.Int32
    {
        @None = 0,
        @Size = 1,
        @Dpi = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    public enum VerticalAlignment : global::System.Int32
    {
        @Top = 0,
        @Center = 1,
        @Bottom = 2,
    }
    [global::System.CodeDom.Compiler.GeneratedCode("MacWinForms.ApiInventory","1")]
    [global::System.Flags]
    public enum VisualStyleState : global::System.Int32
    {
        @NoneEnabled = 0,
        @ClientAreaEnabled = 2,
        @NonClientAreaEnabled = 1,
        @ClientAndNonClientAreasEnabled = 3,
    }
}
