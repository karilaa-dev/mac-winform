#import "ListTheme.h"

static NSColor *RGB(unsigned rgb) {
    return [NSColor colorWithSRGBRed:((rgb>>16)&255)/255.0 green:((rgb>>8)&255)/255.0 blue:(rgb&255)/255.0 alpha:1];
}
// Reserve one unit for the frame so the document, headers and scrollbars
// use the same client origin as the WinForms control.
@interface MFListFrame : NSView
@property NSColor *edge;
@end
@implementation MFListFrame
- (void)viewWillMoveToSuperview:(NSView *)newSuperview {
    [NSNotificationCenter.defaultCenter removeObserver:self name:NSViewFrameDidChangeNotification object:self.superview];
    [super viewWillMoveToSuperview:newSuperview];
    if (newSuperview) {
        newSuperview.postsFrameChangedNotifications=YES;
        [NSNotificationCenter.defaultCenter addObserver:self selector:@selector(resizeFrame:)
            name:NSViewFrameDidChangeNotification object:newSuperview];
    }
}
- (void)resizeFrame:(NSNotification *)notification { self.frame=self.superview.bounds; }
- (NSView *)hitTest:(NSPoint)point { return nil; }
- (void)drawRect:(NSRect)dirty {
    [self.edge setFill];
    NSFrameRectWithWidth(self.bounds,1);
}
@end
void MFStyleScrollBorder(NSScrollView *scroll, NSColor *edge) {
    scroll.borderType=NSNoBorder;
    scroll.automaticallyAdjustsContentInsets=NO;
    CGFloat inset=edge ? 1 : 0;
    scroll.contentInsets=NSEdgeInsetsMake(inset,inset,inset,inset);
    scroll.wantsLayer=YES;
    MFListFrame *frame = nil;
    for (NSView *view in scroll.subviews)
        if ([view isKindOfClass:MFListFrame.class]) { frame = (MFListFrame *)view; break; }
    if (!frame && edge) {
        frame = [[MFListFrame alloc] initWithFrame:scroll.bounds];
        frame.wantsLayer=YES;
        frame.layer.zPosition=1;
        [scroll addSubview:frame positioned:NSWindowAbove relativeTo:nil];
    }
    frame.frame=scroll.bounds;
    frame.hidden = !edge;
    frame.edge = edge;
    frame.needsDisplay = YES;
}
void MFStyleListBorder(NSScrollView *scroll, NSInteger style) {
    MFStyleScrollBorder(scroll,style ? RGB(style == 1 ? 0x646464 : 0xabadb3) : nil);
}
BOOL MFListHasFocus(NSTableView *table) {
    NSResponder *r = table.window.firstResponder;
    return r == table || ([r isKindOfClass:NSView.class] && [(NSView *)r isDescendantOf:table]);
}
NSColor *MFListSelectionColor(BOOL focused) { return RGB(focused ? 0x0078d7 : 0xf0f0f0); }

@interface MFWinTree : NSOutlineView
@property BOOL lines, rootLines, plusMinus, fullRow, hideSelection;
@property NSColor *lineColor, *textColor;
@property NSFont *captionFont;
@property NSInteger explicitRowHeight;
- (CGFloat)junctionAtRow:(NSInteger)row;
- (void)toggleDisclosure:(NSButton *)button;
@end

@interface MFTreeDisclosure : NSButton
@end
@implementation MFTreeDisclosure
- (void)drawRect:(NSRect)dirty {
    CGFloat x = floor(NSMidX(self.bounds)) - 4, y = floor(NSMidY(self.bounds)) - 4;
    NSRect box = NSMakeRect(x,y,9,9);
    [RGB(0x919191) setFill]; NSRectFill(box);
    NSGradient *gradient = [[NSGradient alloc] initWithStartingColor:RGB(0xffffff) endingColor:RGB(0xe3e4e4)];
    [gradient drawInRect:NSInsetRect(box,1,1) angle:self.isFlipped ? 90 : -90];
    [RGB(0x4b63a7) setFill]; NSRectFill(NSMakeRect(x+2,y+4,5,1));
    if (self.state == NSControlStateValueOff) NSRectFill(NSMakeRect(x+4,y+2,1,5));
}
@end

@implementation MFWinTree
- (CGFloat)junctionAtRow:(NSInteger)row {
    NSInteger level = [self levelForRow:row];
    return 13 + (level - (self.rootLines ? 0 : 1)) * self.indentationPerLevel;
}
- (NSRect)frameOfCellAtColumn:(NSInteger)column row:(NSInteger)row {
    NSRect r = [super frameOfCellAtColumn:column row:row];
    if (column == 0 && row >= 0) {
        CGFloat right = NSMaxX(r);
        r.origin.x = MAX(0,[self junctionAtRow:row] + 10);
        r.size.width = MAX(0,right-r.origin.x);
    }
    return r;
}
- (NSRect)frameOfOutlineCellAtRow:(NSInteger)row {
    NSRect r=[super frameOfOutlineCellAtRow:row];
    if (row < 0 || NSIsEmptyRect(r)) return r;
    NSRect line=[self rectOfRow:row];
    CGFloat x=[self junctionAtRow:row];
    if (!self.plusMinus || x < 0) return NSZeroRect;
    return NSMakeRect(x-6,floor(NSMidY(line))-4,13,13);
}
- (void)toggleDisclosure:(NSButton *)button {
    NSInteger row=[self rowForView:button];
    if (row<0 || !self.enabled) return;
    id node=[self itemAtRow:row];
    if ([self isItemExpanded:node]) [self collapseItem:node];
    else [self expandItem:node];
}
- (NSView *)makeViewWithIdentifier:(NSUserInterfaceItemIdentifier)identifier owner:(id)owner {
    if ([identifier isEqual:NSOutlineViewDisclosureButtonKey]) {
        MFTreeDisclosure *button=[MFTreeDisclosure new];
        button.identifier=identifier;
        button.target=self; button.action=@selector(toggleDisclosure:);
        button.bordered=NO;
        [button setButtonType:NSButtonTypePushOnPushOff];
        return button;
    }
    return [super makeViewWithIdentifier:identifier owner:owner];
}
@end

@interface MFTreeCaption : NSTextField
@property (weak) MFWinTree *tree;
@end
@implementation MFTreeCaption
- (void)drawRect:(NSRect)dirty {
    MFWinTree *tree=self.tree;
    NSInteger row=[tree rowForView:self];
    BOOL highlight=row >= 0 && [tree.selectedRowIndexes containsIndex:row] && MFListHasFocus(tree);
    NSColor *color = !tree.enabled ? NSColor.disabledControlTextColor : highlight ? NSColor.whiteColor : tree.textColor;
    NSMutableParagraphStyle *style=[NSMutableParagraphStyle new];
    style.lineBreakMode = NSLineBreakByTruncatingTail;
    NSDictionary *attrs=@{NSFontAttributeName:tree.captionFont ?: [NSFont systemFontOfSize:12],NSForegroundColorAttributeName:color ?: NSColor.blackColor,NSParagraphStyleAttributeName:style};
    CGFloat h=ceil([@"Ag" sizeWithAttributes:attrs].height);
    [self.stringValue drawInRect:NSMakeRect(2,floor((NSHeight(self.bounds)-h)/2),MAX(0,NSWidth(self.bounds)-2),h) withAttributes:attrs];
}
@end

@interface MFTreeRow : NSTableRowView
@property (weak) MFWinTree *tree;
@end
@implementation MFTreeRow
- (void)drawSelectionInRect:(NSRect)dirty {
    MFWinTree *tree=self.tree;
    NSInteger row=[tree rowForView:self];
    BOOL focus=MFListHasFocus(tree);
    if (row<0 || (tree.hideSelection && !focus)) return;
    NSRect selection=self.bounds;
    if (!tree.fullRow || tree.lines) {
        NSTextField *label=(NSTextField *)[tree viewAtColumn:0 row:row makeIfNecessary:NO];
        if (!label) return;
        selection.origin.x=MAX(0,[tree junctionAtRow:row]+10);
        selection.size.width=ceil([label.stringValue sizeWithAttributes:@{NSFontAttributeName:tree.captionFont}].width)+4;
    }
    [MFListSelectionColor(focus) setFill];
    NSRectFill(NSIntersectionRect(selection,dirty));
}
- (void)drawBackgroundInRect:(NSRect)dirty {
    [super drawBackgroundInRect:dirty];
    MFWinTree *tree=self.tree;
    NSInteger row=[tree rowForView:self];
    if (!tree.lines || row < 0) return;
    id node=[tree itemAtRow:row];
    id<NSOutlineViewDataSource> data=tree.dataSource;
    NSInteger level=[tree levelForRow:row];
    CGFloat height=NSHeight(self.bounds);
    // Draw dotted connectors a device-independent pixel at a time for crisp 96-DPI lines.
    NSInteger mid=(NSInteger)floor(height/2)+2;
    [tree.lineColor setFill];
    for (NSInteger currentLevel=level; currentLevel>=0 && node; currentLevel--) {
        id parent=[tree parentForItem:node];
        NSInteger count=[data outlineView:tree numberOfChildrenOfItem:parent];
        BOOL hasNext=count && ![[data outlineView:tree child:count-1 ofItem:parent] isEqual:node];
        CGFloat x=[tree junctionAtRow:row]-(level-currentLevel)*tree.indentationPerLevel;
        if (x>=0 && (currentLevel==level || hasNext)) {
            BOOL previousOrParent=parent || (count && ![[data outlineView:tree child:0 ofItem:parent] isEqual:node]);
            NSInteger start=(currentLevel==level && !previousOrParent) ? mid : 0;
            NSInteger end=hasNext ? (NSInteger)height-1 : mid;
            for (NSInteger y=start+(start%2==0); y<=end; y+=2) NSRectFill(NSMakeRect(x,y,1,1));
            if (currentLevel==level) {
                for (NSInteger dx=0;dx<=8;dx+=2) NSRectFill(NSMakeRect(x+dx,mid,1,1));
            }
        }
        node=parent;
    }
}
@end

NSOutlineView *MFCreateTree(void) {
    MFWinTree *tree=[MFWinTree new];
    if (@available(macOS 11.0,*)) tree.style=NSTableViewStylePlain;
    tree.intercellSpacing=NSZeroSize;
    tree.lineColor=RGB(0x6d6d6d); tree.textColor=NSColor.blackColor;
    tree.captionFont=[NSFont systemFontOfSize:12];
    return tree;
}
NSTableRowView *MFCreateTreeRow(NSOutlineView *tree) {
    MFTreeRow *row=[MFTreeRow new]; row.tree=(MFWinTree *)tree; return row;
}
NSTextField *MFTreeLabel(NSOutlineView *tree,NSString *caption) {
    MFTreeCaption *cell=[tree makeViewWithIdentifier:@"node" owner:tree.delegate];
    if (!cell) { cell=[MFTreeCaption labelWithString:@""]; cell.identifier=@"node"; }
    cell.tree=(MFWinTree *)tree; cell.font=cell.tree.captionFont; cell.stringValue=caption;
    return cell;
}
void MFSetTreeTextStyle(NSOutlineView *view,NSFont *font,NSColor *color) {
    MFWinTree *tree=(MFWinTree *)view;
    if (font) {
        tree.captionFont=font;
        if (!tree.explicitRowHeight) tree.rowHeight=MAX(1,round(font.pointSize*1.5));
    }
    if (color) tree.textColor=color;
    tree.needsDisplay=YES;
}
void MFSetTreeAppearance(NSOutlineView *view,int indent,int height,BOOL lines,BOOL rootLines,BOOL plusMinus,BOOL fullRow,BOOL hideSelection,int lineArgb) {
    MFWinTree *tree=(MFWinTree *)view;
    tree.indentationPerLevel=indent; tree.explicitRowHeight=height;
    tree.rowHeight=height ? height : MAX(1,round(tree.captionFont.pointSize*1.5));
    tree.lines=lines; tree.rootLines=rootLines; tree.plusMinus=plusMinus;
    tree.fullRow=fullRow; tree.hideSelection=hideSelection;
    tree.lineColor=lineArgb ? RGB((unsigned)lineArgb) : RGB(0x6d6d6d);
    tree.needsLayout=YES; tree.needsDisplay=YES;
}
