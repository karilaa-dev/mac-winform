#import "Drawing.h"
#import "Buttons.h"
#import "ListWidgets.h"
#import "ListTheme.h"

// A shared AppKit presentation for ListView and CheckedListBox. The model and
// cancellable WinForms notifications live in their respective managed controls.
@class MFListHost;
@interface MFListRow : NSTableRowView
@property (weak) MFListHost *host;
@end

@interface MFListHeaderCell : NSTableHeaderCell
@end
@implementation MFListHeaderCell
- (void)drawWithFrame:(NSRect)frame inView:(NSView *)view {
    [(self.highlighted ? [NSColor colorWithWhite:.91 alpha:1] : NSColor.whiteColor) setFill]; NSRectFill(frame);
    [[NSColor colorWithWhite:.898 alpha:1] setFill]; NSRectFill(NSMakeRect(NSMaxX(frame)-1,NSMinY(frame)+1,1,MAX(0,NSHeight(frame)-1)));
    NSMutableParagraphStyle *style=[NSMutableParagraphStyle new];
    style.alignment=self.alignment; style.lineBreakMode=NSLineBreakByTruncatingTail;
    NSDictionary *attrs=@{NSFontAttributeName:self.font, NSForegroundColorAttributeName:NSColor.blackColor, NSParagraphStyleAttributeName:style};
    CGFloat height=ceil([@"Ag" sizeWithAttributes:attrs].height);
    [self.stringValue drawInRect:NSMakeRect(NSMinX(frame)+6,floor(NSMidY(frame)-height/2),MAX(0,NSWidth(frame)-14),height) withAttributes:attrs];
}
@end
@interface MFListHeader : NSTableHeaderView
@end
@implementation MFListHeader
- (void)drawRect:(NSRect)dirty {
    [NSColor.whiteColor setFill]; NSRectFill(dirty);
    NSInteger i=0;
    for (NSTableColumn *column in self.tableView.tableColumns) {
        NSRect r=[self headerRectOfColumn:i++]; r.origin.x+=1;
        if (NSIntersectsRect(dirty,r)) [column.headerCell drawWithFrame:r inView:self];
    }
}
@end

@interface MFCheckGlyph : NSButton
@end
@implementation MFCheckGlyph
- (void)drawRect:(NSRect)rect {
    CGFloat top = floor((NSHeight(self.bounds)-13)/2) + 1;
    CGFloat y = self.isFlipped ? top : NSHeight(self.bounds)-top-13;
    MFDrawChoiceGlyph(self.isFlipped,NSMakeRect(0,y,13,13),NO,self.state,self.enabled,self.highlighted,NO);
}
- (NSView *)hitTest:(NSPoint)p { return nil; }
@end
@interface MFListTable : NSTableView
@property (weak) MFListHost *host;
@end
@interface MFListTile : NSCollectionViewItem
@property NSButton *check;
@end
@interface MFListHost : NSScrollView <NSTableViewDataSource,NSTableViewDelegate,NSCollectionViewDataSource,NSCollectionViewDelegate>
@property (copy) void (^action)(int);
@property NSArray<NSDictionary *> *rows;
@property NSArray<NSDictionary *> *columns;
@property NSArray *images;
@property MFListTable *table;
@property NSCollectionView *tiles;
@property NSTableHeaderView *header;
@property BOOL checking, checkedList, checkOnClick, suppressed, active, fullRowSelect;
@property BOOL hideSelection;
@property NSFont *font;
@property NSColor *textColor;
@property int mode, changedRow, clickedColumn;
- (void)toggle:(NSInteger)row;
- (void)synchronizeSelection:(NSIndexSet *)selection;
- (NSIndexSet *)selected;
@end
@implementation MFListRow
- (void)drawSelectionInRect:(NSRect)dirty {
    MFListHost *host=self.host;
    if (host.checkedList) { [super drawSelectionInRect:dirty]; return; }
    BOOL focus=MFListHasFocus(host.table);
    if (!focus && host.hideSelection) return;
    NSInteger row=[host.table rowForView:self];
    if (row<0) return;
    CGFloat right=0;
    if (host.fullRowSelect) {
        for (NSInteger i=0; i<(NSInteger)host.table.tableColumns.count; i++) right=NSMaxX([host.table rectOfColumn:i]);
    } else {
        NSView *cell=[host.table viewAtColumn:0 row:row makeIfNecessary:NO];
        NSTextField *label=(NSTextField *)cell.subviews.lastObject;
        if (!label) return;
        right=MIN(host.table.tableColumns.firstObject.width,label.frame.origin.x+ceil([label.stringValue sizeWithAttributes:@{NSFontAttributeName:label.font}].width)+4);
    }
    CGFloat start=5;
    [MFListSelectionColor(focus) setFill];
    NSRectFill(NSIntersectionRect(dirty,NSMakeRect(start,0,MAX(0,right-start+1),NSHeight(self.bounds))));
}
@end
@interface MFListCaption : NSTextField
@property (weak) MFListHost *host;
@property NSInteger column;
@end
@implementation MFListCaption
- (void)drawRect:(NSRect)dirty {
    MFListHost *host=self.host;
    NSInteger row=[host.table rowForView:self];
    BOOL selected=row>=0 && (host.fullRowSelect || self.column==0) && [host.table.selectedRowIndexes containsIndex:row];
    NSColor *foreground=!host.active ? NSColor.disabledControlTextColor :
        (selected && MFListHasFocus(host.table) ? NSColor.whiteColor : host.textColor);
    NSMutableParagraphStyle *style=[NSMutableParagraphStyle new];
    style.alignment=self.alignment; style.lineBreakMode=NSLineBreakByTruncatingTail;
    NSDictionary *attrs=@{NSFontAttributeName:self.font,NSForegroundColorAttributeName:foreground,NSParagraphStyleAttributeName:style};
    CGFloat height=ceil([@"Ag" sizeWithAttributes:attrs].height);
    [self.stringValue drawInRect:NSMakeRect(0,floor((NSHeight(self.bounds)-height)/2),NSWidth(self.bounds),height) withAttributes:attrs];
}
@end
@implementation MFListTable
- (void)mouseDown:(NSEvent *)event {
    NSInteger row = [self rowAtPoint:[self convertPoint:event.locationInWindow fromView:nil]];
    BOOL oldSelection = row >= 0 && [self.selectedRowIndexes containsIndex:row];
    [super mouseDown:event];
    if (self.enabled && self.host.checkedList && row >= 0 && (oldSelection || self.host.checkOnClick))
        [self.host toggle:row];
}
- (void)keyDown:(NSEvent *)event {
    if (self.enabled && self.host.checking && [event.charactersIgnoringModifiers isEqualToString:@" "] && self.selectedRow >= 0) {
        [self.host toggle:self.selectedRow];
    } else [super keyDown:event];
}
@end
@implementation MFListTile
- (void)loadView {
    self.view = [[NSView alloc] initWithFrame:NSMakeRect(0,0,64,64)];
    self.view.wantsLayer = YES;
    NSImageView *image = [[NSImageView alloc] initWithFrame:NSZeroRect];
    NSTextField *label = [NSTextField wrappingLabelWithString:@""];
    [self.view addSubview:image];
    [self.view addSubview:label];
    self.imageView = image;
    self.textField = label;
    self.check = [NSButton checkboxWithTitle:@"" target:nil action:nil];
    [self.view addSubview:self.check];
}
- (void)setSelected:(BOOL)selected {
    [super setSelected:selected];
    self.view.layer.backgroundColor = (selected ? NSColor.selectedContentBackgroundColor : NSColor.clearColor).CGColor;
    self.textField.textColor = selected ? NSColor.selectedMenuItemTextColor : NSColor.labelColor;
}
@end
@implementation MFListHost
- (void)toggle:(NSInteger)row {
    if (!self.active || row < 0 || row >= (NSInteger)self.rows.count) return;
    self.changedRow = (int)row;
    self.action(25);
}
- (void)checkPressed:(NSButton *)sender { [self toggle:sender.tag]; }
- (void)activated:(id)sender { if (self.active && self.selected.count) self.action(26); }
- (void)tableView:(NSTableView *)table didClickTableColumn:(NSTableColumn *)column {
    self.clickedColumn = [column.identifier intValue];
    self.action(27);
}
- (NSTableRowView *)tableView:(NSTableView *)table rowViewForRow:(NSInteger)row {
    MFListRow *view = [[MFListRow alloc] initWithFrame:NSZeroRect];
    view.host = self;
    return view;
}
- (NSInteger)numberOfRowsInTableView:(NSTableView *)table { return self.rows.count; }
- (NSView *)tableView:(NSTableView *)table viewForTableColumn:(NSTableColumn *)column row:(NSInteger)row {
    if (row < 0 || row >= (NSInteger)self.rows.count) return nil;
    NSInteger index = [column.identifier integerValue];
    NSDictionary *item = self.rows[row];
    NSArray<NSString *> *values = item[@"values"];
    NSString *caption = index < (NSInteger)values.count ? values[index] : @"";
    NSView *view = [[NSView alloc] initWithFrame:NSMakeRect(0,0,column.width,table.rowHeight)];
    CGFloat left = self.checkedList ? 1 : 7;
    if (index == 0 && self.checking) {
        NSButton *button = self.checkedList
            ? [MFCheckGlyph checkboxWithTitle:@"" target:self action:@selector(checkPressed:)]
            : [NSButton checkboxWithTitle:@"" target:self action:@selector(checkPressed:)];
        button.tag = row;
        int state = [item[@"state"] intValue];
        button.allowsMixedState = state == 2;
        button.state = state == 2 ? NSControlStateValueMixed : state;
        button.enabled = self.active;
        button.frame = NSMakeRect(left,0,19,table.rowHeight);
        // CheckedListBox handles the entire row in mouseDown, including its
        // checkbox. It still exposes the checkbox's action to accessibility.
        [view addSubview:button];
        left += self.checkedList ? 17 : 20;
    }
    if (index == 0 && self.images[row] != NSNull.null) {
        NSImageView *image = [[NSImageView alloc] initWithFrame:NSMakeRect(left,0,16,table.rowHeight)];
        image.image = self.images[row];
        [view addSubview:image]; left += 18;
    }
    NSTextField *label;
    if (self.checkedList) {
        label=[NSTextField labelWithString:caption];
        label.enabled=self.active;
        label.textColor=self.active ? self.textColor : NSColor.disabledControlTextColor;
    }
    else {
        MFListCaption *text=[MFListCaption labelWithString:caption];
        text.host=self; text.column=index; label=text;
    }
    label.font = self.font;
    NSInteger align = index < (NSInteger)self.columns.count ? [self.columns[index][@"alignment"] intValue] : 0;
    label.alignment = align == 2 ? NSTextAlignmentCenter : align == 1 ? NSTextAlignmentRight : NSTextAlignmentLeft;
    label.frame = NSMakeRect(left,0,MAX(1,column.width-left-2),table.rowHeight);
    label.autoresizingMask = NSViewWidthSizable;
    [view addSubview:label];
    return view;
}
- (void)tableViewSelectionDidChange:(NSNotification *)note { if (!self.suppressed) self.action(24); }
- (NSInteger)collectionView:(NSCollectionView *)cv numberOfItemsInSection:(NSInteger)section { return self.rows.count; }
- (NSCollectionViewItem *)collectionView:(NSCollectionView *)cv itemForRepresentedObjectAtIndexPath:(NSIndexPath *)path {
    MFListTile *tile = (MFListTile *)[cv makeItemWithIdentifier:@"Item" forIndexPath:path];
    NSInteger row = path.item;
    NSDictionary *item = self.rows[row];
    NSArray *values = item[@"values"];
    CGSize size = ((NSCollectionViewFlowLayout *)cv.collectionViewLayout).itemSize;
    BOOL large = self.mode == 0, showDetails = self.mode == 4;
    CGFloat imageSize = large || showDetails ? 32 : 16;
    BOOL hasImage = self.images[row] != NSNull.null;
    tile.imageView.image = hasImage ? self.images[row] : nil;
    tile.imageView.frame = NSMakeRect(large ? (size.width-imageSize)/2 : 3,large ? size.height-imageSize-2 : (size.height-imageSize)/2,imageSize,imageSize);
    CGFloat inset = large ? 1 : hasImage ? imageSize+5 : 3;
    if (self.checking) inset += large ? 0 : 19;
    tile.textField.frame = NSMakeRect(inset,1,MAX(1,size.width-inset-2), large ? size.height-imageSize-3 : size.height-2);
    tile.textField.font = [NSFont systemFontOfSize:12];
    tile.textField.alignment = large ? NSTextAlignmentCenter : NSTextAlignmentLeft;
    tile.textField.stringValue = showDetails ? [values componentsJoinedByString:@"\n"] : (values.firstObject ?: @"");
    tile.check.hidden = !self.checking;
    tile.check.frame = NSMakeRect(0,large ? size.height-18 : 0,19,19);
    tile.check.tag = row; tile.check.target = self; tile.check.action = @selector(checkPressed:);
    tile.check.enabled = self.active; tile.check.state = [item[@"state"] intValue];
    [tile setSelected:[cv.selectionIndexPaths containsObject:path]];
    return tile;
}
- (void)collectionView:(NSCollectionView *)cv didSelectItemsAtIndexPaths:(NSSet<NSIndexPath *> *)paths {
    if (!self.suppressed) self.action(24);
}
- (void)collectionView:(NSCollectionView *)cv didDeselectItemsAtIndexPaths:(NSSet<NSIndexPath *> *)paths {
    if (!self.suppressed) self.action(24);
}
- (NSIndexSet *)selected {
    return self.documentView == self.table ? self.table.selectedRowIndexes : self.tiles.selectionIndexes;
}
- (void)synchronizeSelection:(NSIndexSet *)selection {
    BOOL previous = self.suppressed;
    self.suppressed = YES;
    if (self.documentView == self.table) [self.table selectRowIndexes:selection byExtendingSelection:NO];
    else self.tiles.selectionIndexes = selection;
    self.suppressed = previous;
}
@end

NSScrollView *MFCreateList(void (^action)(int)) {
    MFListHost *host = [[MFListHost alloc] initWithFrame:NSMakeRect(0,0,120,96)];
    host.action = action;
    host.active = YES;
    host.font=[NSFont systemFontOfSize:12]; host.textColor=NSColor.blackColor;
    host.hasVerticalScroller = host.hasHorizontalScroller = host.autohidesScrollers = YES;
    host.borderType = NSBezelBorder;
    MFListTable *table = [[MFListTable alloc] initWithFrame:host.bounds];
    if (@available(macOS 11.0,*)) table.style = NSTableViewStylePlain;
    table.host = host; table.dataSource = host; table.delegate = host;
    table.allowsEmptySelection = YES;
    table.target = host; table.doubleAction = @selector(activated:);
    table.rowHeight = 18; table.intercellSpacing = NSMakeSize(2,1);
    host.table = table;
    host.header = [[MFListHeader alloc] initWithFrame:NSMakeRect(0,0,120,25)];
    NSCollectionView *tiles = [[NSCollectionView alloc] initWithFrame:host.bounds];
    tiles.dataSource = host; tiles.delegate = host; tiles.selectable = YES;
    tiles.collectionViewLayout = [[NSCollectionViewFlowLayout alloc] init];
    [tiles registerClass:MFListTile.class forItemWithIdentifier:@"Item"];
    tiles.autoresizingMask = NSViewWidthSizable;
    NSClickGestureRecognizer *activate = [[NSClickGestureRecognizer alloc] initWithTarget:host action:@selector(activated:)];
    activate.numberOfClicksRequired = 2; [tiles addGestureRecognizer:activate];
    host.tiles = tiles;
    return host;
}
static void MFSetDetailsMetrics(MFListHost *host) {
    host.table.rowHeight=MAX(1,ceil(host.font.ascender-host.font.descender)+4);
    host.header.frame=NSMakeRect(0,0,NSWidth(host.bounds),host.table.rowHeight+6);
}

void MFSetListTextStyle(NSScrollView *scroll,NSFont *font,NSColor *color) {
    MFListHost *host=(MFListHost *)scroll;
    if (font) {
        host.font=font;
        if (!host.checkedList && host.documentView==host.table) MFSetDetailsMetrics(host);
        for (NSTableColumn *column in host.table.tableColumns) column.headerCell.font=font;
        host.header.needsDisplay=YES;
    }
    if (color) host.textColor=color;
    [host.table enumerateAvailableRowViewsUsingBlock:^(NSTableRowView *row,NSInteger i) {
        for (NSView *cell in row.subviews) for (NSView *child in cell.subviews)
            if ([child isKindOfClass:NSTextField.class]) {
                ((NSTextField *)child).font=host.font;
                if (host.checkedList) ((NSTextField *)child).textColor=host.active ? host.textColor : NSColor.disabledControlTextColor;
                child.needsDisplay=YES;
            }
    }];
}
void MFSetListEnabled(NSScrollView *scroll, BOOL enabled) {
    MFListHost *host = (MFListHost *)scroll;
    host.active = enabled;
    host.table.enabled = enabled;
    host.tiles.selectable = enabled;
    if (host.checkedList) {
        [host.table enumerateAvailableRowViewsUsingBlock:^(NSTableRowView *row,NSInteger index) {
            for (NSView *cell in row.subviews) for (NSView *child in cell.subviews) {
                if ([child isKindOfClass:NSControl.class]) ((NSControl *)child).enabled=enabled;
                if ([child isKindOfClass:NSTextField.class])
                    ((NSTextField *)child).textColor=enabled ? host.textColor : NSColor.disabledControlTextColor;
            }
        }];
    }
}
static MFListHost *MFList(void *handle) { return (MFListHost *)MFWidgetView(handle); }
void mf_list_render(void *handle, const char *json, void **imageHandles, int count) {
    MFListHost *host = MFList(handle);
    NSDictionary *model = [NSJSONSerialization JSONObjectWithData:[@(json) dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
    host.suppressed = YES;
    host.rows = model[@"rows"];
    host.columns = model[@"columns"];
    host.mode = [model[@"view"] intValue];
    host.fullRowSelect = [model[@"fullRowSelect"] boolValue];
    host.checking = [model[@"checks"] boolValue];
    host.checkedList = [model[@"checkedList"] boolValue];
    host.hideSelection = [model[@"hideSelection"] boolValue];
    MFStyleListBorder(host,host.checkedList ? 2 : [model[@"borderStyle"] integerValue]);
    // The native WinForms details header uses 25 units at the default font and each row uses 19.
    host.table.intercellSpacing = host.checkedList ? NSMakeSize(2,1) : NSZeroSize;
    if (host.checkedList) host.table.rowHeight=MAX(1,[model[@"rowHeight"] integerValue]-1);
    else MFSetDetailsMetrics(host);
    host.checkOnClick = [model[@"checkOnClick"] boolValue];
    NSMutableArray *images = [NSMutableArray arrayWithCapacity:count];
    for (int i=0;i<count;i++) [images addObject:imageHandles[i] ? mf_native_image(imageHandles[i]) : NSNull.null];
    host.images = images;
    if (host.mode == 1 || host.checkedList) {
        host.documentView = host.table;
        for (NSTableColumn *column in host.table.tableColumns.copy) [host.table removeTableColumn:column];
        host.table.headerView = [model[@"headers"] boolValue] ? host.header : nil;
        host.table.gridStyleMask = [model[@"grid"] boolValue] ? NSTableViewSolidHorizontalGridLineMask|NSTableViewSolidVerticalGridLineMask : 0;
        host.table.allowsMultipleSelection = [model[@"multi"] boolValue];
        host.table.columnAutoresizingStyle = host.columns.count ? NSTableViewNoColumnAutoresizing : NSTableViewLastColumnOnlyAutoresizingStyle;
        int cols = MAX(1,(int)host.columns.count);
        for (int i = 0; i < cols; i++) {
            NSTableColumn *col = [[NSTableColumn alloc] initWithIdentifier:@(i).stringValue];
            NSDictionary *descriptor = i < host.columns.count ? host.columns[i] : nil;
            if (!host.checkedList) {
                col.headerCell=[[MFListHeaderCell alloc] initTextCell:@""];
                col.headerCell.font=host.font;
                NSInteger align=[descriptor[@"alignment"] integerValue];
                col.headerCell.alignment=align==2 ? NSTextAlignmentCenter : align==1 ? NSTextAlignmentRight : NSTextAlignmentLeft;
            }
            col.title = descriptor[@"title"] ?: @"";
            CGFloat width = descriptor ? [descriptor[@"width"] doubleValue] : NSWidth(host.contentView.bounds);
            if (width < 0) {
                CGFloat measured = width == -2 ? [col.title sizeWithAttributes:nil].width : 0;
                for (NSDictionary *row in host.rows) {
                    NSArray *values = row[@"values"];
                    if (i < values.count) measured = MAX(measured,[values[i] sizeWithAttributes:nil].width);
                }
                width = ceil(measured) + 12 + ((i == 0 && host.checking) ? 20 : 0);
            }
            col.minWidth = 0; col.width = MAX(0,width);
            [host.table addTableColumn:col];
        }
        [host.table reloadData];
    } else {
        host.documentView = host.tiles;
        host.tiles.allowsMultipleSelection = [model[@"multi"] boolValue];
        NSCollectionViewFlowLayout *layout = (NSCollectionViewFlowLayout *)host.tiles.collectionViewLayout;
        layout.scrollDirection = host.mode == 3 ? NSCollectionViewScrollDirectionHorizontal : NSCollectionViewScrollDirectionVertical;
        NSArray *tileSize = model[@"tileSize"];
        layout.minimumInteritemSpacing = 1; layout.minimumLineSpacing = 1;
        layout.itemSize = host.mode == 0 ? NSMakeSize(82,66) : host.mode == 4 ? NSMakeSize([tileSize[0] doubleValue],[tileSize[1] doubleValue]) : NSMakeSize(130,22);
        [host.tiles reloadData];
    }
    host.suppressed = NO;
}
void mf_list_select(void *handle, const int *indices, int count) {
    NSMutableIndexSet *selection = [NSMutableIndexSet indexSet];
    for (int i=0;i<count;i++) [selection addIndex:indices[i]];
    [MFList(handle) synchronizeSelection:selection];
}
int mf_list_selection(void *handle, int *buffer, int capacity) {
    NSIndexSet *rows = MFList(handle).selected;
    int count = 0;
    for (NSUInteger idx=rows.firstIndex; idx!=NSNotFound && count < capacity; idx=[rows indexGreaterThanIndex:idx])
        buffer[count++] = (int)idx;
    return count;
}
int mf_list_action_index(void *handle,int event) { return event == 27 ? MFList(handle).clickedColumn : MFList(handle).changedRow; }
void mf_list_ensure_visible(void *handle,int row) {
    MFListHost *host = MFList(handle);
    if (host.documentView == host.table) [host.table scrollRowToVisible:row];
    else [host.tiles scrollToItemsAtIndexPaths:[NSSet setWithObject:[NSIndexPath indexPathForItem:row inSection:0]] scrollPosition:NSCollectionViewScrollPositionNearestHorizontalEdge|NSCollectionViewScrollPositionNearestVerticalEdge];
}
