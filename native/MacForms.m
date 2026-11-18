#import "Drawing.h"
#import "Buttons.h"
#import <AppKit/AppKit.h>
#import <UniformTypeIdentifiers/UniformTypeIdentifiers.h>
#import "MacForms.h"
#import "Keyboard.h"
#import "WindowCaption.h"
#import "Browser.h"
#import "Mdi.h"
#import "WindowsControls.h"
#import "GridAppearance.h"
#import "Transfer.h"
#import "ScrollBar.h"
#import "ListWidgets.h"
#import "ListTheme.h"
#import "TextEditing.h"

@interface MFCanvas : NSView
@property void *peer;
@property mf_callback callback;
@property NSPoint lastMouse;
@end
@implementation MFCanvas
- (BOOL)isFlipped { return YES; }
- (void)drawRect:(NSRect)dirty {
    [super drawRect:dirty];
    if (self.callback) self.callback(self.peer, 8);
}
- (void)mouseDown:(NSEvent *)event {
    self.lastMouse = [self convertPoint:event.locationInWindow fromView:nil];
    if (self.callback) self.callback(self.peer, 9);
}
- (void)mouseDragged:(NSEvent *)event {
    self.lastMouse = [self convertPoint:event.locationInWindow fromView:nil];
    if (self.callback) self.callback(self.peer, 10);
}
- (void)mouseUp:(NSEvent *)event {
    self.lastMouse = [self convertPoint:event.locationInWindow fromView:nil];
    if (self.callback) {
        self.callback(self.peer, 11);
        if (NSPointInRect(self.lastMouse, self.bounds) && self.callback) self.callback(self.peer, MF_CLICK);
    }
}
@end

@interface MFSplitterCanvas : MFCanvas
@property NSRect divider;
@property BOOL vertical;
@end
@implementation MFSplitterCanvas
- (BOOL)acceptsFirstResponder { return YES; }
- (void)resetCursorRects {
    [super resetCursorRects];
    if (!NSIsEmptyRect(self.divider))
        [self addCursorRect:self.divider cursor:self.vertical ? NSCursor.resizeLeftRightCursor : NSCursor.resizeUpDownCursor];
}
- (void)mouseDown:(NSEvent *)event {
    [self.window makeFirstResponder:self];
    [super mouseDown:event];
}
@end

@interface MFLinkText : NSTextView
@property BOOL underlineOnHover;
@property NSRange hoveredRange;
@property NSTrackingArea *hoverArea;
@end
@implementation MFLinkText
- (void)updateTrackingAreas {
    [super updateTrackingAreas];
    if (self.hoverArea) [self removeTrackingArea:self.hoverArea];
    self.hoverArea=[[NSTrackingArea alloc] initWithRect:NSZeroRect options:NSTrackingInVisibleRect|NSTrackingMouseMoved|NSTrackingMouseEnteredAndExited|NSTrackingActiveInKeyWindow owner:self userInfo:nil];
    [self addTrackingArea:self.hoverArea];
}
- (void)clearHover {
    if (self.hoveredRange.length && NSMaxRange(self.hoveredRange)<=self.string.length)
        [self.layoutManager removeTemporaryAttribute:NSUnderlineStyleAttributeName forCharacterRange:self.hoveredRange];
    self.hoveredRange=NSMakeRange(0,0);
}
- (void)mouseMoved:(NSEvent *)event {
    [super mouseMoved:event]; [self clearHover];
    if (!self.underlineOnHover || !self.string.length) return;
    NSUInteger index=[self characterIndexForInsertionAtPoint:[self convertPoint:event.locationInWindow fromView:nil]];
    if (index>=self.string.length) return;
    NSRange range;
    id link=[self.textStorage attribute:NSLinkAttributeName atIndex:index effectiveRange:&range];
    if (link) {
        self.hoveredRange=range;
        [self.layoutManager addTemporaryAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) forCharacterRange:range];
    }
}
- (void)mouseExited:(NSEvent *)event { [self clearHover]; [super mouseExited:event]; }
@end

static NSString *MFString(const char *value) {
    return value ? [NSString stringWithUTF8String:value] ?: @"" : @"";
}

/* AppKit has no Windows '&' keyboard cues. Keep escaped literal ampersands. */
static NSString *MFTitle(NSString *value) {
    NSMutableString *result = [NSMutableString string];
    for (NSUInteger i = 0; i < value.length; i++) {
        if ([value characterAtIndex:i] == '&') {
            if (i + 1 >= value.length || [value characterAtIndex:i + 1] != '&') continue;
            i++;
        }
        [result appendString:[value substringWithRange:NSMakeRange(i, 1)]];
    }
    return result;
}

static NSColor *MFRGB(unsigned value) {
    return [NSColor colorWithSRGBRed:((value >> 16) & 255) / 255.0
                              green:((value >> 8) & 255) / 255.0
                               blue:(value & 255) / 255.0 alpha:1.0];
}

static void MFInsetBorder(NSRect rect, NSColor *color, CGFloat width) {
    NSBezierPath *border = [NSBezierPath bezierPathWithRect:NSInsetRect(rect, width * 0.5, width * 0.5)];
    border.lineWidth = width;
    [color setStroke];
    [border stroke];
}

static void MFDrawCaption(NSString *text, NSRect frame, NSFont *font, NSColor *color, NSTextAlignment align) {
    NSMutableParagraphStyle *style = [[NSMutableParagraphStyle alloc] init];
    style.alignment = align;
    style.lineBreakMode = NSLineBreakByTruncatingTail;
    NSDictionary *attributes = @{NSFontAttributeName:font ?: [NSFont systemFontOfSize:12],
                                 NSForegroundColorAttributeName:color, NSParagraphStyleAttributeName:style};
    CGFloat height = ceil([@"Ag" sizeWithAttributes:attributes].height);
    NSRect title = NSMakeRect(frame.origin.x, floor(NSMidY(frame) - height / 2), frame.size.width, height);
    [text drawInRect:title withAttributes:attributes];
}

static void MFDrawFocusRect(NSRect rect) {
    NSInteger left = ceil(NSMinX(rect)), top = ceil(NSMinY(rect));
    NSInteger right = floor(NSMaxX(rect))-1, bottom = floor(NSMaxY(rect))-1;
    if (right <= left || bottom <= top) return;
    [MFRGB(0x090909) setFill];
    for (NSInteger x = left; x <= right; x++) {
        if ((x+top)%2) NSRectFill(NSMakeRect(x,top,1,1));
        if ((x+bottom)%2) NSRectFill(NSMakeRect(x,bottom,1,1));
    }
    for (NSInteger y = top+1; y < bottom; y++) {
        if ((left+y)%2) NSRectFill(NSMakeRect(left,y,1,1));
        if ((right+y)%2) NSRectFill(NSMakeRect(right,y,1,1));
    }
}

/* Use stock controls for event tracking, keyboard focus, accessibility and
   menus. Only their cells' drawing is customized to match designer geometry. */
@interface MFButtonCell : NSButtonCell
@property BOOL tabAppearance;
@property BOOL hideCaption;
@end
@implementation MFButtonCell
- (void)drawWithFrame:(NSRect)frame inView:(NSView *)view {
    BOOL isDefault = view.window.defaultButtonCell == self;
    BOOL selected = self.tabAppearance && self.state == NSControlStateValueOn;
    NSColor *background = MFRGB(self.highlighted ? 0xe8f1fa : selected ? 0xf9f9f9 : 0xfdfdfd);
    NSRect bezel = NSInsetRect(frame, self.tabAppearance ? 0.5 : 1.5, self.tabAppearance ? 0.5 : 1.5);
    NSBezierPath *path = [NSBezierPath bezierPathWithRoundedRect:bezel
                                                         xRadius:self.tabAppearance ? 0 : 4
                                                         yRadius:self.tabAppearance ? 0 : 4];
    [background setFill];
    [path fill];
    [MFRGB(self.tabAppearance ? 0xe5e5e5 : isDefault ? 0x0078d4 : 0xd0d0d0) setStroke];
    path.lineWidth = 1;
    if (!selected) [path stroke];
    NSRect caption = NSInsetRect(frame,3,0);
    if (self.image && self.imagePosition != NSNoImage) {
        CGFloat size = MIN(16,NSHeight(frame)-2);
        BOOL imageOnly = self.imagePosition == NSImageOnly || !self.title.length;
        CGFloat left = imageOnly ? floor(NSMidX(frame)-size/2) : NSMinX(caption);
        NSRect dest = NSMakeRect(left,floor(NSMidY(frame)-size/2),size,size);
        [self.image drawInRect:dest fromRect:NSZeroRect operation:NSCompositingOperationSourceOver fraction:self.enabled ? 1 : 0.45 respectFlipped:YES hints:nil];
        if (!imageOnly) { caption.origin.x += size+2; caption.size.width -= size+2; }
    }
    caption.origin.y -= 1;
    if (!self.hideCaption) MFDrawCaption(self.title, caption, self.font,
                  self.enabled ? NSColor.blackColor : NSColor.disabledControlTextColor, NSTextAlignmentCenter);
    if (view.window.firstResponder == view && (self.tabAppearance || view.window.isKeyWindow)) {
        CGFloat inset = self.tabAppearance ? 1 : 3;
        MFDrawFocusRect(NSInsetRect(frame,inset,inset));
    }
}
@end

static NSButton *MFButton(id target, SEL action, BOOL tab) {
    NSButton *button = [(tab ? [NSButton class] : [MFStyledButton class]) alloc];
    button = [button initWithFrame:NSZeroRect];
    MFButtonCell *cell = [[MFButtonCell alloc] initTextCell:@""];
    cell.tabAppearance = tab;
    cell.font = [NSFont systemFontOfSize:12];
    button.cell = cell;
    [button setButtonType:tab ? NSButtonTypePushOnPushOff : NSButtonTypeMomentaryPushIn];
    button.focusRingType = NSFocusRingTypeNone;
    button.target = target;
    button.action = action;
    return button;
}

@interface MFComboCell : NSPopUpButtonCell
@end
@implementation MFComboCell
- (void)drawWithFrame:(NSRect)frame inView:(NSView *)view {
    NSBezierPath *box = [NSBezierPath bezierPathWithRoundedRect:NSInsetRect(frame,.5,.5) xRadius:2 yRadius:2];
    [MFRGB(self.enabled ? 0xfdfdfd : 0xf0f0f0) setFill]; [box fill];
    [MFRGB(0xd2d2d2) setStroke]; [box stroke];
    [MFRGB(0xbdbdbd) setFill]; NSRectFill(NSMakeRect(NSMinX(frame)+2,NSMaxY(frame)-1,MAX(0,NSWidth(frame)-4),1));
    NSRect caption = NSInsetRect(frame, 4, 0);
    caption.size.width = MAX(0, caption.size.width - 18);
    MFDrawCaption(self.titleOfSelectedItem ?: @"", caption, self.font, NSColor.textColor, NSTextAlignmentLeft);
    CGFloat x = NSMaxX(frame) - 10, y = NSMidY(frame);
    NSBezierPath *arrow = [NSBezierPath bezierPath];
    [arrow moveToPoint:NSMakePoint(x - 4, y - 2)];
    [arrow lineToPoint:NSMakePoint(x, y + 2)];
    [arrow lineToPoint:NSMakePoint(x + 4, y - 2)];
    arrow.lineWidth = 1;
    [NSColor.darkGrayColor setStroke]; [arrow stroke];
}
@end

@interface MFGroupBox : NSBox
@end
@implementation MFGroupBox
- (BOOL)isFlipped { return YES; }
- (void)drawRect:(NSRect)dirty {
    NSRect r = NSInsetRect(self.bounds, 0.5, 0.5);
    NSDictionary *attrs = @{NSFontAttributeName:self.titleFont ?: [NSFont systemFontOfSize:12],
                            NSForegroundColorAttributeName:NSColor.textColor};
    NSSize text = [self.title sizeWithAttributes:attrs];
    CGFloat y = floor(text.height / 2) + 0.5;
    NSBezierPath *edge = [NSBezierPath bezierPath];
    [edge moveToPoint:NSMakePoint(7, y)];
    [edge lineToPoint:NSMakePoint(NSMinX(r), y)];
    [edge lineToPoint:NSMakePoint(NSMinX(r), NSMaxY(r))];
    [edge lineToPoint:NSMakePoint(NSMaxX(r), NSMaxY(r))];
    [edge lineToPoint:NSMakePoint(NSMaxX(r), y)];
    [edge lineToPoint:NSMakePoint(14 + text.width, y)];
    [MFRGB(0xdcdcdc) setStroke]; [edge stroke];
    [self.title drawAtPoint:NSMakePoint(9, 0) withAttributes:attrs];
}
@end

@interface MFNotebook : NSView
@property NSTabView *tabs;
@property NSArray<NSButton *> *buttons;
@property (copy) void (^selectRequested)(NSInteger);
@property NSInteger requested;
@property BOOL noSelection;
@property NSFont *captionFont;
- (void)refresh;
@end

@implementation MFNotebook
- (BOOL)isFlipped { return YES; }
- (instancetype)initWithFrame:(NSRect)frame {
    if ((self = [super initWithFrame:frame])) {
        _tabs = [[NSTabView alloc] initWithFrame:NSZeroRect];
        _tabs.tabViewType = NSNoTabsNoBorder;
        _tabs.drawsBackground = NO;
        [self addSubview:_tabs];
    }
    return self;
}
- (void)selectPage:(NSButton *)sender {
    if (self.selectRequested) self.selectRequested(sender.tag);
    else [self.tabs selectTabViewItemAtIndex:sender.tag];
    [self refresh];
}
- (void)refresh {
    if ((NSInteger)self.buttons.count != self.tabs.numberOfTabViewItems) {
        for (NSButton *button in self.buttons) [button removeFromSuperview];
        NSMutableArray *buttons = [NSMutableArray array];
        for (NSInteger i = 0; i < self.tabs.numberOfTabViewItems; i++) {
            NSButton *button = MFButton(self,@selector(selectPage:),YES);
            if (self.captionFont) button.font = self.captionFont;
            button.tag = i;
            [button setAccessibilityRole:NSAccessibilityRadioButtonRole];
            [self addSubview:button];
            [buttons addObject:button];
        }
        self.buttons = buttons;
    }
    CGFloat x = 2;
    for (NSInteger i = 0; i < self.tabs.numberOfTabViewItems; i++) {
        NSTabViewItem *item = [self.tabs tabViewItemAtIndex:i];
        NSButton *button = self.buttons[i];
        button.title = item.label ?: @"";
        button.state = !self.noSelection && item == self.tabs.selectedTabViewItem ? NSControlStateValueOn : NSControlStateValueOff;
        CGFloat preferred = ceil([button.title sizeWithAttributes:@{NSFontAttributeName:button.font}].width) + 14 + (button.image ? 16 : 0);
        CGFloat width = MIN(preferred, MAX(0, self.bounds.size.width - x) / MAX(1, self.tabs.numberOfTabViewItems - i));
        button.frame = NSMakeRect(x, 2, width, 20);
        x += width;
    }
    self.tabs.frame = NSMakeRect(4, 24, MAX(0,self.bounds.size.width-8), MAX(0,self.bounds.size.height-28));
    self.needsDisplay = YES;
}
- (void)setFrameSize:(NSSize)size { [super setFrameSize:size]; [self refresh]; }
- (void)drawRect:(NSRect)dirty {
    NSRect client = NSMakeRect(0,22,MAX(0,NSWidth(self.bounds)-1),MAX(0,NSHeight(self.bounds)-23));
    [MFRGB(0xf9f9f9) setFill]; NSRectFill(client);
    MFInsetBorder(client,MFRGB(0xe5e5e5),1);
    [MFRGB(0xf0f0f0) setFill]; NSRectFill(self.tabs.frame);
    for (NSButton *button in self.buttons) if (button.state == NSControlStateValueOn) {
        NSRect tab = NSInsetRect(button.frame,-2,-2);
        [MFRGB(0xe5e5e5) setFill]; NSRectFill(tab);
        [MFRGB(0xf9f9f9) setFill]; NSRectFill(NSMakeRect(NSMinX(tab)+1,NSMinY(tab)+1,MAX(0,NSWidth(tab)-2),MAX(0,NSHeight(tab)-1)));
        break;
    }
}
@end

typedef int (*MFGridEdit)(int phase, int row, int column, const char *text);

@interface MFControl : NSObject <NSWindowDelegate, NSTextFieldDelegate, NSComboBoxDelegate,
    NSTextViewDelegate, NSTabViewDelegate, NSTableViewDelegate, NSTableViewDataSource,
    NSOutlineViewDelegate, NSOutlineViewDataSource, NSMenuDelegate>
@property int kind;
@property mf_callback callback;
@property NSWindow *window;
@property MFWindowCaption *caption;
@property NSBox *group;
@property NSMenuItem *menuItem;
@property NSProgressIndicator *progress;
@property NSOutlineView *tree;
@property NSDictionary<NSNumber *, NSDictionary *> *nodeInfo;
@property NSDictionary<NSNumber *, NSArray<NSNumber *> *> *branches;
@property NSView *view;
@property NSView *container;
@property NSScrollView *autoScroll;
@property NSControl *control;
@property NSTextView *editor;
@property NSTableView *table;
@property NSTabView *tabs;
@property NSTabViewItem *page;
@property NSStepper *stepper;
@property BOOL stepperLeft;
@property int linkIndex;
@property NSArray<NSArray<NSString *> *> *rows;
@property CGFloat standardHeaderHeight;
@property MFGridEdit gridEdit;
@property (weak) NSTextField *editingField;
@property (weak) NSPopUpButton *editingPopup;
@property MFControl *editingPeer;
@property NSControl *standaloneControl;
@property BOOL finishingEdit;
@property NSMutableArray<NSString *> *items;
@property NSString *rawText;
@property (copy) NSString *returnedText;
@property BOOL suppressEvents;
- (int)gridEditPhase:(int)phase view:(NSView *)view text:(NSString *)text;
@end

@interface MFGridPopup : NSPopUpButton
@property (weak) MFControl *grid;
@end
@implementation MFGridPopup
- (void)mouseDown:(NSEvent *)event {
    if (![self.grid gridEditPhase:0 view:self text:nil]) return;
    self.grid.editingPopup = self;
    [self.window makeFirstResponder:self];
    [super mouseDown:event];
}
- (BOOL)resignFirstResponder {
    MFControl *g=self.grid;
    if (!g.finishingEdit && g.editingPopup==self) {
        if (![g gridEditPhase:1 view:self text:self.titleOfSelectedItem]) return NO;
        g.editingPopup=nil;
        [g gridEditPhase:2 view:self text:nil];
    }
    return [super resignFirstResponder];
}
@end

@interface MFEditingTable : MFGridTable
@property (weak) MFControl *host;
@end
@implementation MFEditingTable
- (void)mouseDown:(NSEvent *)event {
    NSPoint point = [self convertPoint:event.locationInWindow fromView:nil];
    NSInteger row = [self rowAtPoint:point], col = [self columnAtPoint:point];
    if (row >= 0 && col >= 0 && self.host.gridEdit) self.host.gridEdit(5,(int)row,(int)col,"");
    [super mouseDown:event];
}
- (void)keyDown:(NSEvent *)event {
    NSString *chars = event.charactersIgnoringModifiers;
    if (chars.length && self.selectedRow >= 0 && self.host.gridEdit) {
        unichar key = [chars characterAtIndex:0];
        if (key == NSLeftArrowFunctionKey || key == NSRightArrowFunctionKey) {
            NSInteger col = MAX(0,MIN(self.numberOfColumns-1,self.activeColumn + (key == NSRightArrowFunctionKey ? 1 : -1)));
            if (self.numberOfColumns && self.host.gridEdit(5,(int)self.selectedRow,(int)col,"")) {
                [self scrollColumnToVisible:col];
                return;
            }
        }
        if ((key == NSDeleteCharacter || key == NSDeleteFunctionKey) && self.host.gridEdit(4,(int)self.selectedRow,-1,"")) return;
    }
    [super keyDown:event];
}
@end

@implementation MFControl
- (void)menuWillOpen:(NSMenu *)menu { [self sendEvent:31]; }
- (void)menuDidClose:(NSMenu *)menu { [self sendEvent:32]; }
- (void)fitColumns {
    if (self.kind != MF_GRID) return;
    NSArray<MFGridColumn *> *columns = (NSArray *)self.table.tableColumns;
    NSScrollView *scroll = (NSScrollView *)self.view;
    CGFloat border = 2; // One pixel each side of the grid.
    CGFloat width = MAX(0,NSWidth(scroll.bounds)-border);
    CGFloat height = MAX(0,NSHeight(scroll.bounds)-border-NSHeight(self.table.headerView.frame));
    CGFloat minWidth = 0;
    for (MFGridColumn *column in columns) minWidth += column.fixedWidth > 0 ? column.fixedWidth : column.minWidth;
    CGFloat dataHeight = self.rows.count * self.table.rowHeight;
    CGFloat scroller = [NSScroller scrollerWidthForControlSize:NSControlSizeRegular scrollerStyle:scroll.scrollerStyle];
    BOOL vertical = dataHeight > height;
    BOOL horizontal = minWidth > width - (vertical ? scroller : 0);
    vertical = dataHeight > height - (horizontal ? scroller : 0);
    horizontal = minWidth > width - (vertical ? scroller : 0);
    if (scroll.hasVerticalScroller != vertical) scroll.hasVerticalScroller = vertical;
    if (scroll.hasHorizontalScroller != horizontal) scroll.hasHorizontalScroller = horizontal;
    CGFloat remaining = width - (vertical ? scroller : 0);
    double total = 0;
    NSMutableArray<MFGridColumn *> *fill = [NSMutableArray array];
    for (MFGridColumn *column in columns) {
        if (column.fixedWidth > 0) {
            column.width = column.fixedWidth;
            remaining -= column.width;
        } else {
            [fill addObject:column];
            total += column.fillWeight;
        }
    }
    // Reserve columns that hit their minimum before sharing the remaining width.
    NSMutableSet *atMinimum = [NSMutableSet set];
    for (MFGridColumn *column in [fill sortedArrayUsingComparator:^NSComparisonResult(MFGridColumn *a, MFGridColumn *b) {
        return [@(b.minWidth / b.fillWeight) compare:@(a.minWidth / a.fillWeight)];
    }]) {
        if (remaining * column.fillWeight / total >= column.minWidth) break;
        column.width = column.minWidth;
        remaining -= column.width;
        total -= column.fillWeight;
        [atMinimum addObject:column];
    }
    for (MFGridColumn *column in fill) {
        if ([atMinimum containsObject:column]) continue;
        column.width = MAX(column.minWidth, round(remaining * column.fillWeight / total));
        remaining -= column.width;
        total -= column.fillWeight;
    }
    if (!NSIsEmptyRect(scroll.bounds)) MFStyleScrollBorder(scroll,NSColor.blackColor);
}
- (void)sendEvent:(int)event {
    if (self.callback && !self.suppressEvents) self.callback((__bridge void *)self, event);
}
- (void)activate:(id)sender {
    if (self.kind == MF_MENU_ITEM && self.menuItem.submenu.numberOfItems > 0) {
        [self.menuItem.submenu popUpMenuPositioningItem:nil atLocation:NSMakePoint(0, 0) inView:self.view];
        return;
    }
    if (self.kind == MF_NUMBER || self.kind == MF_DOMAIN) {
        int event = 36;
        if (sender == self.stepper) {
            event = self.stepper.integerValue > 0 ? 34 : 35;
            self.stepper.integerValue = 0;
        }
        [self sendEvent:event];
        return;
    }
    if (self.kind == MF_TRACK) self.control.doubleValue = round(self.control.doubleValue);
    if (self.kind == 32 || self.kind == MF_DATE || self.kind == MF_TRACK) {
        [self sendEvent:MF_VALUE_CHANGED];
        return;
    }
    if (self.kind == MF_COMBO_BOX) {
        [self sendEvent:MF_SELECTION_CHANGED];
        return;
    }
    if (self.kind == MF_CHECK_BOX || self.kind == MF_RADIO_BUTTON)
        [self sendEvent:MF_CHECKED_CHANGED];
    [self sendEvent:MF_CLICK];
}
- (BOOL)windowShouldClose:(NSWindow *)sender {
    /* The managed FormClosing handler can veto or hide/dispose this window. */
    if (!self.callback) return YES;
    [self sendEvent:MF_CLOSED];
    return NO;
}
- (void)windowDidResize:(NSNotification *)note { [self.caption refresh]; [self sendEvent:MF_RESIZED]; }
- (void)windowDidBecomeKey:(NSNotification *)note { [self.caption refresh]; }
- (void)windowDidResignKey:(NSNotification *)note { [self.caption refresh]; }
- (void)comboBoxSelectionDidChange:(NSNotification *)note { [self sendEvent:MF_SELECTION_CHANGED]; }
// phase 0 begins an edit, 1 validates and writes, 2 ends, and 3 cancels.
- (int)gridEditPhase:(int)phase view:(NSView *)view text:(NSString *)text {
    if (!self.gridEdit || self.suppressEvents) return 0;
    NSInteger row = [self.table rowForView:view], col = [self.table columnForView:view];
    if (row < 0 || col < 0) return 0;
    int index = [self.table.tableColumns[col].identifier intValue];
    return self.gridEdit(phase,(int)row,index,text.UTF8String ?: "");
}
- (void)toggleGridValue:(NSButton *)button {
    if (![self gridEditPhase:0 view:button text:nil] ||
        ![self gridEditPhase:1 view:button text:button.state == NSControlStateValueOn ? @"True" : @"False"]) {
        button.state = button.state == NSControlStateValueOn ? NSControlStateValueOff : NSControlStateValueOn;
        [self gridEditPhase:3 view:button text:nil];
        return;
    }
    [self gridEditPhase:2 view:button text:nil];
}
- (void)selectGridChoice:(NSPopUpButton *)button { [self.editingPeer sendEvent:MF_SELECTION_CHANGED]; }
- (BOOL)control:(NSControl *)field textShouldBeginEditing:(NSText *)editor {
    if (self.kind != MF_GRID) return YES;
    if (![self gridEditPhase:0 view:field text:nil]) return NO;
    self.editingField = (NSTextField *)field;
    return YES;
}
- (BOOL)control:(NSControl *)field textShouldEndEditing:(NSText *)editor {
    if (self.kind != MF_GRID || self.finishingEdit || self.suppressEvents) return YES;
    return [self gridEditPhase:1 view:field text:editor.string] != 0;
}
- (void)controlTextDidBeginEditing:(NSNotification *)note {
    if (self.kind == MF_TEXT_BOX || self.kind == MF_PASSWORD) MFFieldBegan(note.object,note.userInfo);
}
- (BOOL)textView:(NSTextView *)editor shouldChangeTextInRange:(NSRange)range replacementString:(NSString *)replacement {
    return MFShouldReplaceText((__bridge void *)self,range,replacement);
}
- (BOOL)textView:(NSTextView *)editor clickedOnLink:(id)link atIndex:(NSUInteger)index {
    if (self.kind != MF_LINK_LABEL) return NO;
    self.linkIndex = [[link isKindOfClass:NSURL.class] ? [link lastPathComponent] : [link description] intValue];
    [self sendEvent:37];
    return YES;
}
- (void)textViewDidChangeSelection:(NSNotification *)note {
    if (self.kind == MF_RICH_TEXT) [self sendEvent:28];
}
- (void)controlTextDidEndEditing:(NSNotification *)note {
    if (self.kind == MF_TEXT_BOX || self.kind == MF_PASSWORD) MFFieldFinished(note.object,note.userInfo);
    if (self.kind != MF_GRID || self.finishingEdit || self.suppressEvents) return;
    self.editingField = nil;
    [self gridEditPhase:2 view:note.object text:nil];
}
- (BOOL)control:(NSControl *)control textView:(NSTextView *)textView doCommandBySelector:(SEL)command {
    if (self.kind != MF_GRID || command != @selector(cancelOperation:)) return NO;
    self.finishingEdit = YES;
    [control abortEditing];
    self.editingField = nil;
    self.finishingEdit = NO;
    [self gridEditPhase:3 view:control text:nil];
    return YES;
}
- (void)controlTextDidChange:(NSNotification *)note {
    if (self.kind != MF_GRID) [self sendEvent:MF_TEXT_CHANGED];
    else [self.editingPeer sendEvent:MF_TEXT_CHANGED];
}
- (void)textDidChange:(NSNotification *)note { [self sendEvent:MF_TEXT_CHANGED]; }
- (void)tableViewSelectionDidChange:(NSNotification *)note { [self sendEvent:MF_SELECTION_CHANGED]; }
- (void)tabView:(NSTabView *)tabs didSelectTabViewItem:(NSTabViewItem *)item {
    [(MFNotebook *)self.view refresh];
    [self sendEvent:MF_SELECTION_CHANGED];
}
- (void)tabViewDidChangeNumberOfTabViewItems:(NSTabView *)tabs { [(MFNotebook *)self.view refresh]; }
- (NSInteger)outlineView:(NSOutlineView *)outline numberOfChildrenOfItem:(id)item {
    return self.branches[item ?: @0].count;
}
- (id)outlineView:(NSOutlineView *)outline child:(NSInteger)index ofItem:(id)item {
    return self.branches[item ?: @0][index];
}
- (BOOL)outlineView:(NSOutlineView *)outline isItemExpandable:(id)item {
    return self.branches[item].count != 0;
}
- (NSView *)outlineView:(NSOutlineView *)outline viewForTableColumn:(NSTableColumn *)column item:(id)item {
    return MFTreeLabel(outline,self.nodeInfo[item][@"Text"] ?: @"");
}
- (NSTableRowView *)outlineView:(NSOutlineView *)outline rowViewForItem:(id)item {
    return MFCreateTreeRow(outline);
}
- (void)outlineViewSelectionDidChange:(NSNotification *)note { [self sendEvent:MF_SELECTION_CHANGED]; }
- (NSTableRowView *)tableView:(NSTableView *)table rowViewForRow:(NSInteger)row {
    return self.kind == MF_GRID ? MFMakeGridRow((MFGridTable *)table,row) : nil;
}
- (NSInteger)numberOfRowsInTableView:(NSTableView *)table {
    return self.kind == MF_GRID ? self.rows.count : self.items.count;
}
- (NSView *)tableView:(NSTableView *)table viewForTableColumn:(NSTableColumn *)column
                  row:(NSInteger)row {
    id value = nil;
    if (self.kind == MF_GRID) {
        NSInteger index = [column.identifier integerValue];
        NSArray *values = self.rows[row];
        if (index < (NSInteger)values.count) value = values[index];
        MFGridColumn *gridColumn = (MFGridColumn *)column;
        if (gridColumn.choices) {
            MFGridPopup *box = [table makeViewWithIdentifier:@"combo" owner:nil];
            if (!box) {
                box = [[MFGridPopup alloc] initWithFrame:NSZeroRect pullsDown:NO];
                box.identifier = @"combo"; box.grid = self;
                box.target = self; box.action = @selector(selectGridChoice:);
                box.bezelStyle = NSBezelStyleSmallSquare;
            }
            [box removeAllItems];
            for (NSString *name in gridColumn.choices) [box.menu addItem:[[NSMenuItem alloc] initWithTitle:name action:nil keyEquivalent:@""]];
            [box selectItemWithTitle:value == NSNull.null ? @"" : [value description]];
            box.enabled = self.table.enabled && gridColumn.allowsEdit;
            return box;
        }
        if (gridColumn.checkBox) {
            NSButton *box = [table makeViewWithIdentifier:@"check" owner:nil];
            if (!box) {
                box = [NSButton checkboxWithTitle:@"" target:self action:@selector(toggleGridValue:)];
                box.identifier = @"check";
            }
            box.enabled = self.table.enabled && ((MFGridColumn *)column).allowsEdit;
            box.state = [value respondsToSelector:@selector(boolValue)] && [value boolValue]
                            ? NSControlStateValueOn : NSControlStateValueOff;
            return box;
        }
    } else value = self.items[row];
    NSTextField *cell = [table makeViewWithIdentifier:@"cell" owner:nil];
    if (!cell) {
        cell = self.kind == MF_GRID ? MFMakeGridField() : [NSTextField labelWithString:@""];
        cell.identifier = @"cell";
    }
    if (self.kind == MF_GRID) {
        MFGridTable *grid = (MFGridTable *)table;
        MFGridColumn *style = (MFGridColumn *)column;
        cell.font = style.cellFont ?: grid.captionFont;
        cell.textColor = [grid highlightsRow:row column:column.identifier.integerValue] ? style.selectionFore : style.cellFore;
        cell.editable = ((MFGridColumn *)column).allowsEdit && self.table.enabled;
        cell.selectable = cell.editable;
        cell.delegate = self;
        cell.focusRingType = NSFocusRingTypeNone;
    }
    cell.stringValue = !value || value == NSNull.null ? @"" : [value description];
    return cell;
}
@end

static MFControl *MFHandle(void *handle) { return (__bridge MFControl *)handle; }

static NSScrollView *MFScroller(void) {
    NSScrollView *scroll = [[NSScrollView alloc] initWithFrame:NSZeroRect];
    scroll.hasVerticalScroller = YES;
    scroll.autohidesScrollers = YES;
    scroll.borderType = NSBezelBorder;
    return scroll;
}

void mf_init(void) {
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        [NSApplication sharedApplication];
        [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
        NSMenu *main = [[NSMenu alloc] init];
        NSMenuItem *appItem = [[NSMenuItem alloc] init];
        [main addItem:appItem];
        appItem.submenu = [[NSMenu alloc] initWithTitle:@"Application"];
        [appItem.submenu addItemWithTitle:@"Quit" action:@selector(terminate:) keyEquivalent:@"q"];
        NSMenuItem *editItem = [[NSMenuItem alloc] init];
        [main addItem:editItem];
        editItem.submenu = [[NSMenu alloc] initWithTitle:@"Edit"];
        [editItem.submenu addItemWithTitle:@"Cut" action:@selector(cut:) keyEquivalent:@"x"];
        [editItem.submenu addItemWithTitle:@"Copy" action:@selector(copy:) keyEquivalent:@"c"];
        [editItem.submenu addItemWithTitle:@"Paste" action:@selector(paste:) keyEquivalent:@"v"];
        [editItem.submenu addItemWithTitle:@"Select All" action:@selector(selectAll:) keyEquivalent:@"a"];
        NSApp.mainMenu = main;
        [NSApp finishLaunching];
    });
}

void mf_run(void) {
    @autoreleasepool { [NSApp run]; }
}

void mf_stop(void) {
    [NSApp stop:nil];
    NSEvent *wake = [NSEvent otherEventWithType:NSEventTypeApplicationDefined location:NSZeroPoint
                                 modifierFlags:0 timestamp:0 windowNumber:0 context:nil
                                       subtype:0 data1:0 data2:0];
    [NSApp postEvent:wake atStart:NO];
}

void mf_pump(void) {
    @autoreleasepool {
        NSEvent *event;
        NSDate *deadline = [NSDate dateWithTimeIntervalSinceNow:0.02];
        while ((event = [NSApp nextEventMatchingMask:NSEventMaskAny untilDate:deadline
                                             inMode:NSDefaultRunLoopMode dequeue:YES])) {
            [NSApp sendEvent:event];
            deadline = [NSDate distantPast];
        }
        [NSApp updateWindows];
    }
}

void *mf_create(int kind, mf_callback callback, const char *text) {
    mf_init();
    MFControl *h = [[MFControl alloc] init];
    h.kind = kind;
    h.items = [NSMutableArray array];
    switch (kind) {
        case MF_FORM: {
            NSWindowStyleMask style = NSWindowStyleMaskTitled | NSWindowStyleMaskClosable |
                                      NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable;
            h.window = [[MFWindow alloc] initWithContentRect:NSMakeRect(0, 0, 300, 300)
                                                   styleMask:style backing:NSBackingStoreBuffered defer:NO];
            h.window.releasedWhenClosed = NO;
            mf_register_drop_window(h.window);
            h.window.appearance = [NSAppearance appearanceNamed:NSAppearanceNameAqua];
            h.window.backgroundColor = MFRGB(0xf0f0f0);
            h.container = [[MFCanvas alloc] initWithFrame:h.window.contentView.bounds];
            h.window.contentView = h.container;
            h.caption = [[MFWindowCaption alloc] initWithWindow:h.window];
            [h.caption updateBorder:4 controlBox:YES minimize:YES maximize:YES];
            h.window.delegate = h;
            [h.window center];
            break;
        }
        case 38: {
            h.container = [[MFCanvas alloc] initWithFrame:NSZeroRect];
            MFMdiFrame *frame = [[MFMdiFrame alloc] initWithContent:h.container];
            __weak MFControl *owner = h;
            frame.notify = ^(int ev) { [owner sendEvent:ev]; };
            h.view = frame;
            break;
        }
        case 31:
            h.view = MFMakeBrowser((__bridge void *)h,callback);
            if (!h.view) return NULL;
            break;
        case 32: {
            NSDatePicker *cal = [NSDatePicker new];
            cal.datePickerStyle = NSDatePickerStyleClockAndCalendar;
            cal.datePickerElements = NSDatePickerElementFlagYearMonthDay;
            cal.target = h;
            cal.action = @selector(activate:);
            h.control = cal;
            break;
        }
        case MF_SPLITTER:
            h.view = h.container = [[MFSplitterCanvas alloc] initWithFrame:NSZeroRect];
            break;
        case MF_PANEL:
            h.view = h.container = [[MFCanvas alloc] initWithFrame:NSZeroRect];
            break;
        case MF_GROUP_BOX: {
            NSBox *box = [[MFGroupBox alloc] initWithFrame:NSZeroRect];
            /* WinForms positions children against the entire group, including its caption. */
            h.view = h.container = [[MFCanvas alloc] initWithFrame:NSZeroRect];
            box.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
            [h.container addSubview:box];
            h.group = box;
            break;
        }
        case MF_LABEL:
            h.control = [NSTextField wrappingLabelWithString:@""];
            break;
        case MF_MENU_ITEM: {
            NSButton *button = [NSButton buttonWithTitle:@"" target:h action:@selector(activate:)];
            button.bordered = NO;
            h.control = button;
            h.menuItem = [[NSMenuItem alloc] initWithTitle:@"" action:@selector(activate:) keyEquivalent:@""];
            h.menuItem.target = h;
            break;
        }
        case 30: {
            h.menuItem = [[NSMenuItem alloc] init];
            h.menuItem.submenu = [[NSMenu alloc] initWithTitle:@""];
            h.menuItem.submenu.autoenablesItems = NO;
            h.menuItem.submenu.delegate = h;
            break;
        }
        case MF_MENU_SEPARATOR:
            h.menuItem = NSMenuItem.separatorItem;
            h.view = [[NSBox alloc] initWithFrame:NSMakeRect(0,0,1,20)];
            ((NSBox *)h.view).boxType = NSBoxSeparator;
            break;
        case MF_BUTTON:
            h.control = MFButton(h,@selector(activate:),NO);
            break;
        case MF_PASSWORD:
        case MF_TEXT_BOX: {
            Class fieldClass = kind == MF_PASSWORD ? NSSecureTextField.class : NSTextField.class;
            NSTextField *field = [[fieldClass alloc] initWithFrame:NSZeroRect];
            field.bezeled = NO;
            field.bordered = YES;
            field.drawsBackground = YES;
            field.backgroundColor = NSColor.whiteColor;
            field.wantsLayer = YES;
            field.layer.backgroundColor = NSColor.whiteColor.CGColor;
            field.layer.borderColor = MFRGB(0xabadb3).CGColor;
            field.layer.borderWidth = 1;
            field.delegate = h;
            h.control = field;
            break;
        }
        case MF_CHECK_BOX:
            h.control = [MFChoiceButton checkboxWithTitle:@"" target:h action:@selector(activate:)];
            break;
        case MF_RADIO_BUTTON:
            h.control = [MFChoiceButton radioButtonWithTitle:@"" target:h action:@selector(activate:)];
            ((MFChoiceButton *)h.control).radio = YES;
            break;
        case MF_EDITABLE_COMBO: {
            NSComboBox *combo = [[NSComboBox alloc] initWithFrame:NSZeroRect];
            combo.delegate = h;
            h.control = combo;
            break;
        }
        case MF_COMBO_BOX: {
            NSPopUpButton *button = [[NSPopUpButton alloc] initWithFrame:NSZeroRect pullsDown:NO];
            button.cell = [[MFComboCell alloc] initTextCell:@"" pullsDown:NO];
            button.target = h;
            button.action = @selector(activate:);
            h.control = button;
            break;
        }
        case MF_TABS: {
            MFNotebook *notebook = [[MFNotebook alloc] initWithFrame:NSZeroRect];
            h.tabs = notebook.tabs;
            h.tabs.delegate = h;
            __weak MFControl *peer=h;
            __weak MFNotebook *weakNotebook=notebook;
            notebook.selectRequested=^(NSInteger row){ weakNotebook.requested=row; [peer sendEvent:38]; };
            h.view = notebook;
            break;
        }
        case MF_TAB_PAGE:
            h.page = [[NSTabViewItem alloc] initWithIdentifier:nil];
            h.view = h.container = [[MFCanvas alloc] initWithFrame:NSZeroRect];
            h.page.view = h.view;
            break;
        case MF_DATE: {
            NSDatePicker *date = [[NSDatePicker alloc] initWithFrame:NSZeroRect];
            date.datePickerStyle = NSDatePickerStyleTextFieldAndStepper;
            date.datePickerElements = NSDatePickerElementFlagYearMonthDay;
            date.dateValue = [NSDate date];
            date.target = h;
            date.action = @selector(activate:);
            h.control = date;
            break;
        }
        case MF_DOMAIN:
        case MF_NUMBER: {
            h.view = [[MFCanvas alloc] initWithFrame:NSZeroRect];
            NSTextField *field = [[NSTextField alloc] initWithFrame:NSZeroRect];
            field.delegate = h;
            field.target = h;
            field.action = @selector(activate:);
            ((NSActionCell *)field.cell).sendsActionOnEndEditing = YES;
            h.control = field;
            h.stepper = [[NSStepper alloc] initWithFrame:NSZeroRect];
            h.stepper.minValue = -1; h.stepper.maxValue = 1; h.stepper.integerValue = 0;
            h.stepper.valueWraps = NO;
            h.stepper.target = h;
            h.stepper.action = @selector(activate:);
            [h.view addSubview:field];
            [h.view addSubview:h.stepper];
            break;
        }
        case MF_PROGRESS:
            h.progress = [[MFProgressIndicator alloc] initWithFrame:NSZeroRect];
            h.progress.style = NSProgressIndicatorStyleBar;
            h.progress.indeterminate = NO;
            h.view = h.progress;
            break;
        case 36:
        case 37: {
            __weak MFControl *weak = h;
            h.control = MFCreateScrollBar(kind == 36, ^(int event) { [weak sendEvent:event]; });
            break;
        }
        case MF_TRACK: {
            MFTrackBar *slider = [[MFTrackBar alloc] initWithFrame:NSZeroRect];
            slider.focusRingType = NSFocusRingTypeNone;
            slider.target = h;
            slider.action = @selector(activate:);
            slider.continuous = YES;
            h.control = slider;
            break;
        }
        case MF_LIST_VIEW:
        case MF_CHECKED_LIST: {
            __weak MFControl *weak = h;
            h.view = MFCreateList(^(int event) { [weak sendEvent:event]; });
            break;
        }
        case MF_TREE: {
            NSScrollView *scroll = MFScroller();
            NSOutlineView *tree = MFCreateTree();
            NSTableColumn *column = [[NSTableColumn alloc] initWithIdentifier:@"node"];
            [tree addTableColumn:column];
            tree.outlineTableColumn = column;
            tree.headerView = nil;
            tree.allowsEmptySelection = YES;
            tree.columnAutoresizingStyle = NSTableViewLastColumnOnlyAutoresizingStyle;
            tree.dataSource = h;
            tree.delegate = h;
            scroll.documentView = tree;
            h.tree = tree;
            h.view = scroll;
            break;
        }
        case MF_GRID:
        case MF_LIST_BOX: {
            NSScrollView *scroll = MFScroller();
            NSTableView *table;
            if (kind == MF_GRID) {
                MFEditingTable *editing = [[MFEditingTable alloc] initWithFrame:NSZeroRect];
                editing.host = h;
                editing.style = NSTableViewStylePlain;
                editing.usesAutomaticRowHeights = NO;
                editing.rowSizeStyle = NSTableViewRowSizeStyleCustom;
                editing.rowHeight = 25;
                scroll.scrollerStyle = NSScrollerStyleLegacy;
                scroll.autohidesScrollers = NO;
                scroll.borderType = NSLineBorder;
                table = editing;
            } else table = [[NSTableView alloc] initWithFrame:NSZeroRect];
            NSTableColumn *column = [[NSTableColumn alloc] initWithIdentifier:@"values"];
            column.resizingMask = NSTableColumnAutoresizingMask;
            if (kind == MF_LIST_BOX) {
                [table addTableColumn:column];
                table.headerView = nil;
            } else {
                table.gridStyleMask = NSTableViewGridNone;
                table.usesAlternatingRowBackgroundColors = NO;
                table.gridColor = [NSColor colorWithWhite:.50 alpha:1];
                table.backgroundColor = NSColor.whiteColor;
                table.intercellSpacing = NSZeroSize;
                scroll.hasHorizontalScroller = YES;
            }
            table.allowsEmptySelection = YES;
            table.dataSource = h;
            table.delegate = h;
            table.columnAutoresizingStyle = kind == MF_GRID ? NSTableViewNoColumnAutoresizing : NSTableViewLastColumnOnlyAutoresizingStyle;
            h.standardHeaderHeight = NSHeight(table.headerView.frame);
            scroll.documentView = table;
            h.table = table;
            h.view = scroll;
            break;
        }
        case MF_LINK_LABEL: {
            MFLinkText *label = [[MFLinkText alloc] initWithFrame:NSZeroRect];
            label.editable = NO; label.selectable = YES; label.richText = YES;
            label.drawsBackground = NO; label.textContainerInset = NSZeroSize;
            label.textContainer.lineFragmentPadding = 1;
            label.linkTextAttributes = @{};
            label.delegate = h;
            h.editor = label; h.view = label;
            break;
        }
        case MF_RICH_TEXT:
        case MF_MASKED_TEXT:
        case MF_MULTILINE: {
            NSScrollView *scroll = MFScroller();
            NSTextView *editor = [[NSTextView alloc] initWithFrame:NSMakeRect(0, 0, 1, 1)];
            editor.richText = kind == MF_RICH_TEXT;
            editor.allowsUndo = YES;
            if (kind == MF_MASKED_TEXT) {
                editor.textContainer.maximumNumberOfLines = 1;
                scroll.hasVerticalScroller = NO;
            }
            editor.font = [NSFont systemFontOfSize:NSFont.systemFontSize];
            editor.verticallyResizable = YES;
            editor.horizontallyResizable = NO;
            editor.autoresizingMask = NSViewWidthSizable;
            editor.minSize = NSMakeSize(0, 0);
            editor.maxSize = NSMakeSize(CGFLOAT_MAX, CGFLOAT_MAX);
            editor.textContainer.widthTracksTextView = YES;
            editor.delegate = h;
            scroll.documentView = editor;
            h.editor = editor;
            h.view = scroll;
            break;
        }
        default: return NULL;
    }
    if (!h.view) h.view = h.control;
    void *handle = (__bridge_retained void *)h;
    mf_text(handle, text);
    h.callback = callback;
    mf_key_view(h.view ?: h.container, handle);
    if ([h.container isKindOfClass:MFCanvas.class]) {
        MFCanvas *canvas = (MFCanvas *)h.container;
        canvas.peer = handle;
        canvas.callback = callback;
    }
    return handle;
}

void mf_destroy(void *handle) {
    MFControl *h = (__bridge_transfer MFControl *)handle;
    if (h.standaloneControl) {
        mf_key_view(h.control,NULL);
        h.view = h.control = h.standaloneControl;
        h.standaloneControl = nil;
    }
    if ([h.container isKindOfClass:MFCanvas.class]) ((MFCanvas *)h.container).callback = NULL;
    if (h.kind == 31) { ((WKWebView *)h.view).navigationDelegate = nil; [(WKWebView *)h.view stopLoading]; }
    mf_key_view(h.view ?: h.container, NULL);
    h.callback = NULL;
    h.window.delegate = nil;
    [h.menuItem.menu removeItem:h.menuItem];
    [h.window close];
    if (h.page.tabView) [h.page.tabView removeTabViewItem:h.page];
    else [h.view removeFromSuperview];
}

void mf_parent(void *handle, void *parent) {
    MFControl *h = MFHandle(handle);
    MFControl *owner = MFHandle(parent);
    if (h.menuItem) {
        [h.menuItem.menu removeItem:h.menuItem];
        if (owner.kind == MF_MENU_ITEM || owner.kind == 30) {
            if (!owner.menuItem.submenu) {
                NSMenu *menu = [[NSMenu alloc] initWithTitle:owner.menuItem.title];
                menu.autoenablesItems = NO;
                owner.menuItem.submenu = menu;
            }
            [h.view removeFromSuperview];
            [owner.menuItem.submenu addItem:h.menuItem];
            return;
        }
    }
    if (h.page) {
        if (h.page.tabView == owner.tabs) return;
        if (h.page.tabView) [h.page.tabView removeTabViewItem:h.page];
        [owner.tabs addTabViewItem:h.page];
        return;
    }
    NSView *container = owner.container;
    if (container && h.view && h.view.superview != container) [container addSubview:h.view];
    else if (!container) [h.view removeFromSuperview];
    if (h.view) [NSNotificationCenter.defaultCenter postNotificationName:@"MFViewReparented" object:h.view];
}

void mf_frame(void *handle, int x, int y, int width, int height) {
    MFControl *h = MFHandle(handle);
    if (h.standaloneControl) return; // NSTableView lays out this borrowed editing field.
    NSRect bounds = NSMakeRect(x, y, MAX(0, width), MAX(0, height));
    if (h.window) {
        NSRect frame = [h.window frameRectForContentRect:bounds];
        NSRect desktop = NSScreen.screens.firstObject.frame;
        frame.origin.y = NSMaxY(desktop) - y - frame.size.height;
        if (!NSEqualRects(frame, h.window.frame)) [h.window setFrame:frame display:YES];
    } else if (!h.page) {
        h.view.frame = bounds;
        [h fitColumns];
        if (h.stepper) {
            CGFloat spinner = MIN(19, bounds.size.width);
            h.control.frame = NSMakeRect(h.stepperLeft ? spinner : 0, 0, bounds.size.width - spinner, bounds.size.height);
            h.stepper.frame = NSMakeRect(h.stepperLeft ? 0 : bounds.size.width - spinner, 0, spinner, bounds.size.height);
        }
    }
}

int mf_width(void *handle) {
    MFControl *h = MFHandle(handle);
    return (int)round((h.window ? h.window.contentView : h.view).bounds.size.width);
}

int mf_height(void *handle) {
    MFControl *h = MFHandle(handle);
    return (int)round((h.window ? h.window.contentView : h.view).bounds.size.height);
}

void mf_text(void *handle, const char *text) {
    MFControl *h = MFHandle(handle);
    NSString *value = MFString(text);
    h.rawText = value;
    if (h.kind == MF_MENU_ITEM) h.menuItem.title = MFTitle(value);
    if (h.kind == 38) ((MFMdiFrame *)h.view).title = value;
    else if (h.window) { h.window.title = value; [h.caption refresh]; }
    else if (h.page) {
        h.page.label = value;
        [(MFNotebook *)h.page.tabView.superview refresh];
    }
    else if (h.editor) h.editor.string = value;
    else if (h.kind == MF_GROUP_BOX) h.group.title = MFTitle(value);
    else if (h.kind == MF_EDITABLE_COMBO) h.control.stringValue = value;
    else if (h.kind == MF_COMBO_BOX) [(NSPopUpButton *)h.control selectItemWithTitle:value];
    else if ([h.control isKindOfClass:NSButton.class]) ((NSButton *)h.control).title = MFTitle(value);
    else if (h.kind == MF_LABEL) h.control.stringValue = MFTitle(value);
    else if (h.kind == MF_TEXT_BOX || h.kind == MF_PASSWORD || h.stepper) h.control.stringValue = value;
}

const char *mf_get_text(void *handle) {
    MFControl *h = MFHandle(handle);
    if (h.editor) h.returnedText = h.editor.string;
    else if (h.kind == MF_TEXT_BOX || h.kind == MF_PASSWORD || h.stepper) h.returnedText = h.control.stringValue;
    else if (h.kind == MF_EDITABLE_COMBO) h.returnedText = h.control.stringValue;
    else if (h.kind == MF_COMBO_BOX) h.returnedText = ((NSPopUpButton *)h.control).titleOfSelectedItem;
    else h.returnedText = h.rawText;
    return h.returnedText.UTF8String ?: "";
}

void mf_visible(void *handle, int visible) {
    MFControl *h = MFHandle(handle);
    if (h.window) {
        if (visible) {
            [h.window makeKeyAndOrderFront:nil];
            if (@available(macOS 14.0, *)) [NSApp activate];
            else [NSApp activateIgnoringOtherApps:YES];
        } else [h.window orderOut:nil];
    } else {
        h.view.hidden = !visible;
        h.menuItem.hidden = !visible;
    }
}

void mf_readonly(void *handle, int readOnly) {
    MFControl *h = MFHandle(handle);
    if ([h.control isKindOfClass:NSTextField.class]) ((NSTextField *)h.control).editable = !readOnly;
    h.editor.editable = !readOnly;
}

void mf_enabled(void *handle, int enabled) {
    MFControl *h = MFHandle(handle);
    if (h.window) h.window.ignoresMouseEvents = !enabled;
    h.menuItem.enabled = enabled;
    h.control.enabled = enabled;
    h.stepper.enabled = enabled;
    h.editor.selectable = enabled;
    h.table.enabled = enabled;
    h.tree.enabled = enabled;
    if (h.kind == MF_LIST_VIEW || h.kind == MF_CHECKED_LIST) MFSetListEnabled((NSScrollView *)h.view,enabled);
}

void mf_checked(void *handle, int value) {
    MFControl *h = MFHandle(handle);
    if (h.kind == MF_MENU_ITEM) h.menuItem.state = value ? NSControlStateValueOn : NSControlStateValueOff;
    if (h.kind == MF_CHECK_BOX || h.kind == MF_RADIO_BUTTON) {
        NSButton *button = (NSButton *)h.control;
        if (value == 2) button.allowsMixedState = YES;
        button.state = value == 2 ? NSControlStateValueMixed : value ? NSControlStateValueOn : NSControlStateValueOff;
    }
}

int mf_get_checked(void *handle) {
    MFControl *h = MFHandle(handle);
    if (h.kind != MF_CHECK_BOX && h.kind != MF_RADIO_BUTTON) return 0;
    NSControlStateValue state = ((NSButton *)h.control).state;
    return state == NSControlStateValueMixed ? 2 : state == NSControlStateValueOn;
}

void mf_add_item(void *handle, const char *text) {
    MFControl *h = MFHandle(handle);
    NSString *value = MFString(text);
    h.suppressEvents = YES;
    if (h.kind == MF_COMBO_BOX) {
        NSPopUpButton *button = (NSPopUpButton *)h.control;
        NSInteger selected = button.indexOfSelectedItem;
        [button.menu addItem:[[NSMenuItem alloc] initWithTitle:value action:nil keyEquivalent:@""]];
        [button selectItemAtIndex:selected];
    } else if (h.kind == MF_EDITABLE_COMBO) {
        [(NSComboBox *)h.control addItemWithObjectValue:value];
    } else if (h.table) {
        [h.items addObject:value];
        [h.table reloadData];
    }
    h.suppressEvents = NO;
}

void mf_clear_items(void *handle) {
    MFControl *h = MFHandle(handle);
    h.suppressEvents = YES;
    if (h.kind == MF_COMBO_BOX) [(NSPopUpButton *)h.control removeAllItems];
    if (h.kind == MF_EDITABLE_COMBO) [(NSComboBox *)h.control removeAllItems];
    [h.items removeAllObjects];
    [h.table reloadData];
    h.suppressEvents = NO;
}

void mf_select(void *handle, int index) {
    MFControl *h = MFHandle(handle);
    h.suppressEvents = YES;
    if (h.kind == MF_COMBO_BOX) {
        NSPopUpButton *button = (NSPopUpButton *)h.control;
        if (index >= -1 && index < button.numberOfItems) [button selectItemAtIndex:index];
    } else if (h.kind == MF_EDITABLE_COMBO) {
        NSComboBox *combo = (NSComboBox *)h.control;
        if (index >= 0 && index < combo.numberOfItems) {
            [combo selectItemAtIndex:index];
            combo.stringValue = [combo itemObjectValueAtIndex:index];
        } else if (combo.indexOfSelectedItem >= 0) [combo deselectItemAtIndex:combo.indexOfSelectedItem];
    } else if (h.tabs) {
        if (index >= 0 && index < h.tabs.numberOfTabViewItems) [h.tabs selectTabViewItemAtIndex:index];
    } else if (h.table) {
        if (index < 0) [h.table deselectAll:nil];
        else if (index < h.table.numberOfRows)
            [h.table selectRowIndexes:[NSIndexSet indexSetWithIndex:index] byExtendingSelection:NO];
    }
    h.suppressEvents = NO;
}

int mf_get_selected(void *handle) {
    MFControl *h = MFHandle(handle);
    if (h.kind == MF_COMBO_BOX) return (int)((NSPopUpButton *)h.control).indexOfSelectedItem;
    if (h.kind == MF_EDITABLE_COMBO) return (int)((NSComboBox *)h.control).indexOfSelectedItem;
    if (h.tabs) return h.tabs.selectedTabViewItem ? (int)[h.tabs indexOfTabViewItem:h.tabs.selectedTabViewItem] : -1;
    return h.table ? (int)h.table.selectedRow : -1;
}

int mf_focus(void *handle) {
    MFControl *h = MFHandle(handle);
    NSView *view = h.editor ?: h.tree ?: h.table ?: h.view ?: h.container;
    if (h.kind == MF_LIST_VIEW || h.kind == MF_CHECKED_LIST) view = ((NSScrollView *)h.view).documentView;
    else if (h.stepper) view = h.control;
    else if (h.tabs && h.tabs.selectedTabViewItem) {
        MFNotebook *book = (MFNotebook *)h.view;
        NSInteger index = [h.tabs indexOfTabViewItem:h.tabs.selectedTabViewItem];
        if (index >= 0 && index < (NSInteger)book.buttons.count) view = book.buttons[index];
    }
    if (!view.window || !view.acceptsFirstResponder) return 0;
    if (!view.window.visible) view.window.initialFirstResponder = view;
    return [view.window makeFirstResponder:view];
}

void mf_center(void *handle) { [MFHandle(handle).window center]; }



void mf_default_button(void *form, void *button) {
    NSWindow *window = MFHandle(form).window;
    NSButton *target = (NSButton *)MFHandle(button).control;
    window.defaultButtonCell = (NSButtonCell *)target.cell;
}

void mf_tooltip(void *handle, const char *text) {
    MFHandle(handle).view.toolTip = MFString(text);
}

void mf_post(void (*callback)(void)) {
    // A WinForms event can synchronously open a modal form. Its nested pump
    // must be able to dispatch a second continuation before the first returns.
    CFRunLoopRef main = CFRunLoopGetMain();
    CFRunLoopTimerRef timer = CFRunLoopTimerCreateWithHandler(NULL, CFAbsoluteTimeGetCurrent(), 0, 0, 0,
        ^(CFRunLoopTimerRef _) { callback(); });
    CFRunLoopAddTimer(main, timer, kCFRunLoopCommonModes);
    CFRelease(timer);
    CFRunLoopWakeUp(main);
}

void mf_date_value(void *handle, long long seconds) {
    NSDatePicker *date = (NSDatePicker *)MFHandle(handle).control;
    date.dateValue = [NSDate dateWithTimeIntervalSince1970:(NSTimeInterval)seconds];
}
long long mf_get_date_value(void *handle) {
    NSDatePicker *date = (NSDatePicker *)MFHandle(handle).control;
    return (long long)date.dateValue.timeIntervalSince1970;
}

void mf_number(void *handle, double value, double minimum, double maximum) {
    MFControl *h = MFHandle(handle);
    h.stepper.minValue = minimum;
    h.stepper.maxValue = maximum;
    h.control.doubleValue = h.stepper.doubleValue = MIN(maximum, MAX(minimum, value));
}

double mf_get_number(void *handle) {
    MFControl *h = MFHandle(handle);
    return MIN(h.stepper.maxValue, MAX(h.stepper.minValue, h.control.doubleValue));
}

static NSArray *MFJsonArray(const char *json) {
    if (!json) return @[];
    NSData *data = [NSData dataWithBytes:json length:strlen(json)];
    id parsed = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [parsed isKindOfClass:NSArray.class] ? parsed : @[];
}

void mf_grid_set_editor(void *handle, MFGridEdit callback) {
    MFHandle(handle).gridEdit = callback;
}
void mf_grid_edit_control(void *handle,void *peer,int row,int column) {
    MFControl *g = MFHandle(handle);
    MFControl *old = g.editingPeer;
    if (old) {
        mf_key_view(old.view,NULL);
        old.view = old.control = old.standaloneControl;
        old.standaloneControl = nil;
        g.editingPeer = nil;
    }
    if (peer) {
        MFControl *h = MFHandle(peer);
        NSControl *cell = (NSControl *)[g.table viewAtColumn:column row:row makeIfNecessary:YES];
        if (!cell) return;
        h.standaloneControl = h.control;
        g.editingPeer = h;
        h.view = h.control = cell;
        mf_key_view(cell,peer);
    }
}
int mf_grid_begin_edit(void *handle, int row, int column, int selectAll) {
    MFControl *h = MFHandle(handle);
    if (row < 0 || row >= h.table.numberOfRows || column < 0 || column >= h.table.numberOfColumns) return 0;
    [h.table.window.contentView layoutSubtreeIfNeeded];
    [h.table scrollRowToVisible:row];
    [h.table scrollColumnToVisible:column];
    NSView *view = [h.table viewAtColumn:column row:row makeIfNecessary:YES];
    if ([view isKindOfClass:MFGridPopup.class]) {
        MFGridPopup *popup=(MFGridPopup *)view;
        if (!popup.enabled || ![h gridEditPhase:0 view:popup text:nil]) return 0;
        h.editingPopup=popup;
        if ([popup.window makeFirstResponder:popup]) return 1;
        h.editingPopup=nil; [h gridEditPhase:3 view:popup text:nil]; return 0;
    }
    if (![view isKindOfClass:NSTextField.class]) return 0;
    NSTextField *field = (NSTextField *)view;
    if (!field.editable || ![h gridEditPhase:0 view:field text:nil]) return 0;
    if (![h.table.window makeFirstResponder:field]) {
        [h gridEditPhase:3 view:field text:nil];
        return 0;
    }
    h.editingField = field;
    NSText *editor = field.currentEditor;
    if (editor && selectAll) editor.selectedRange = NSMakeRange(0,editor.string.length);
    return 1;
}
int mf_grid_end_edit(void *handle, int cancel) {
    MFControl *h = MFHandle(handle);
    if (h.editingPopup) {
        NSPopUpButton *popup=h.editingPopup;
        if (!cancel && ![h gridEditPhase:1 view:popup text:popup.titleOfSelectedItem]) return 0;
        h.editingPopup=nil;
        h.finishingEdit=YES;
        [popup.window makeFirstResponder:h.table];
        h.finishingEdit=NO;
        [h gridEditPhase:(cancel ? 3 : 2) view:popup text:nil];
        return 1;
    }
    NSTextField *field = h.editingField;
    if (!field) return 1;
    if (!cancel && ![h gridEditPhase:1 view:field text:field.currentEditor.string ?: field.stringValue]) return 0;
    h.finishingEdit = YES;
    h.editingField = nil;
    [field abortEditing];
    [h.table.window makeFirstResponder:h.table];
    h.finishingEdit = NO;
    [h gridEditPhase:(cancel ? 3 : 2) view:field text:nil];
    return 1;
}

void mf_grid_palette(void *handle,int background,int border) {
    MFControl *h = MFHandle(handle);
    h.table.backgroundColor = MFGridColor(background);
    ((NSScrollView *)h.view).backgroundColor = h.table.backgroundColor;
    h.table.gridColor = MFGridColor(border);
    h.table.headerView.needsDisplay = YES;
    [h.table enumerateAvailableRowViewsUsingBlock:^(NSTableRowView *view, NSInteger row) { view.needsDisplay = YES; }];
}
void mf_grid_selection(void *handle,int row,int column,int mode) {
    MFControl *h = MFHandle(handle);
    MFGridTable *table = (MFGridTable *)h.table;
    BOOL changed = table.selectedRow != row || table.activeColumn != column || table.selectionMode != mode;
    table.activeColumn = column;
    table.selectionMode = mode;
    mf_select(handle,row);
    [table refreshSelection];
    if (changed) NSAccessibilityPostNotification(table,NSAccessibilitySelectedCellsChangedNotification);
}

void mf_grid_columns(void *handle, const char *json) {
    MFControl *h = MFHandle(handle);
    NSArray *columns = MFJsonArray(json);
    for (NSTableColumn *column in h.table.tableColumns.copy) [h.table removeTableColumn:column];
    NSInteger i = 0;
    for (id entry in columns) {
        NSDictionary *descriptor = [entry isKindOfClass:NSDictionary.class] ? entry : nil;
        NSString *title = descriptor ? descriptor[@"title"] : entry;
        MFGridColumn *column = [[MFGridColumn alloc] initWithIdentifier:[@(i++) stringValue]];
        column.title = title ?: @"";
        [column configurePalette:descriptor];
        column.checkBox = [descriptor[@"check"] boolValue];
        if ([descriptor[@"choices"] isKindOfClass:NSArray.class]) column.choices = descriptor[@"choices"];
        column.allowsEdit = [descriptor[@"editable"] boolValue];
        column.fixedWidth = [descriptor[@"width"] doubleValue];
        double weight = [descriptor[@"weight"] doubleValue];
        column.fillWeight = weight > 0 ? weight : 1.0;
        column.minWidth = column.fixedWidth > 0 ? MIN(column.fixedWidth, 24) : 24;
        column.resizingMask = NSTableColumnUserResizingMask;
        [h.table addTableColumn:column];
    }
    [h fitColumns];
    [h.table reloadData];
}

void mf_grid_header_height(void *handle, int height) {
    MFControl *h = MFHandle(handle);
    NSTableHeaderView *header = h.table.headerView;
    NSRect frame = header.frame;
    frame.size.height = height > 0 ? height : h.standardHeaderHeight;
    header.frame = frame;
    [(NSScrollView *)h.view tile];
    [h fitColumns];
}

void mf_grid_rows(void *handle, const char *json) {
    MFControl *h = MFHandle(handle);
    h.suppressEvents = YES;
    h.rows = MFJsonArray(json);
    [h.table reloadData];
    [h fitColumns];
    h.suppressEvents = NO;
}

void mf_style(void *handle, const char *name, float points, int bold, int argb, int align) {
    MFControl *h = MFHandle(handle);
    if (points > 0) {
        /* Designer rectangles use 96 units per inch. AppKit measures fonts in 72 points per inch. */
        points *= 96.0f / 72.0f;
        NSFont *font = [NSFont fontWithName:MFString(name) size:points];
        if (!font && [MFString(name) caseInsensitiveCompare:@"Segoe UI"] == NSOrderedSame)
            font = [NSFont fontWithName:@"Tahoma" size:points] ?: [NSFont fontWithName:@"Arial" size:points];
        if (!font) font = [NSFont systemFontOfSize:points];
        if (bold) font = [NSFontManager.sharedFontManager convertFont:font toHaveTrait:NSBoldFontMask];
        if (h.kind == MF_LIST_VIEW || h.kind == MF_CHECKED_LIST) MFSetListTextStyle((NSScrollView *)h.view,font,nil);
        if (h.tree) MFSetTreeTextStyle(h.tree,font,nil);
        h.control.font = font;
        h.editor.font = font;
        if (h.tabs) {
            MFNotebook *book = (MFNotebook *)h.view;
            book.captionFont = font;
            for (NSButton *button in book.buttons) button.font = font;
            [book refresh];
        }
        if (h.kind == MF_GROUP_BOX) h.group.titleFont = font;
        if (h.kind == MF_GRID) ((MFGridTable *)h.table).captionFont = font;
    }
    NSColor *color = NSColor.blackColor;
    if (argb) {
        unsigned rgba = (unsigned)argb;
        color = [NSColor colorWithSRGBRed:((rgba >> 16) & 255) / 255.0 green:((rgba >> 8) & 255) / 255.0
                                     blue:(rgba & 255) / 255.0 alpha:((rgba >> 24) & 255) / 255.0];
    }
    if (h.kind == MF_LIST_VIEW || h.kind == MF_CHECKED_LIST) MFSetListTextStyle((NSScrollView *)h.view,nil,color);
    if (h.tree) MFSetTreeTextStyle(h.tree,nil,color);
    if ([h.control isKindOfClass:NSTextField.class]) {
        NSTextField *field = (NSTextField *)h.control;
        field.textColor = color;
        field.alignment = (align & 0x222) ? NSTextAlignmentCenter : (align & 0x444) ? NSTextAlignmentRight : NSTextAlignmentLeft;
    }
    h.editor.textColor = color;
}

int mf_alert(void *ownerView,const char *text,const char *title,int buttons,int icon,int defaultButton,int options) {
    mf_init();
    NSAlert *alert = [[NSAlert alloc] init];
    alert.messageText = MFString(title);
    alert.informativeText = MFString(text);
    alert.alertStyle = icon == 16 ? NSAlertStyleCritical : icon == 48 ? NSAlertStyleWarning : NSAlertStyleInformational;
    NSArray<NSString *> *names = @[@"", @"OK", @"Cancel", @"Abort", @"Retry", @"Ignore", @"Yes", @"No", @"", @"", @"Try Again", @"Continue"];
    NSArray<NSArray<NSNumber *> *> *sets = @[@[@1], @[@1,@2], @[@3,@4,@5], @[@6,@7,@2], @[@6,@7], @[@4,@2], @[@2,@10,@11]];
    NSArray<NSNumber *> *choices = buttons >= 0 && buttons < (int)sets.count ? sets[buttons] : sets[0];
    for (NSNumber *result in choices) [alert addButtonWithTitle:names[result.intValue]];
    if (defaultButton >= 0 && defaultButton < (int)alert.buttons.count) {
        for (NSButton *button in alert.buttons) button.keyEquivalent = @"";
        alert.buttons[defaultButton].keyEquivalent = @"\r";
    }
    if (options & (0x80000 | 0x100000)) {
        for (NSView *view in alert.window.contentView.subviews) if ([view isKindOfClass:NSTextField.class]) {
            NSTextField *field = (NSTextField *)view;
            if (options & 0x80000) field.alignment = NSTextAlignmentRight;
            if (options & 0x100000) field.baseWritingDirection = NSWritingDirectionRightToLeft;
        }
    }
    __block NSModalResponse result = NSModalResponseContinue;
    NSWindow *owner = ((__bridge NSView *)ownerView).window;
    if (owner && owner.visible && !owner.attachedSheet) {
        [alert beginSheetModalForWindow:owner completionHandler:^(NSModalResponse response) { result = response; }];
        while (result == NSModalResponseContinue) mf_pump();
    } else result = [alert runModal];
    NSInteger index = result - NSAlertFirstButtonReturn;
    return index >= 0 && index < (NSInteger)choices.count ? choices[index].intValue : 2;
}

@interface MFFileFilter : NSObject
@property (weak) NSSavePanel *panel;
@property NSArray<NSString *> *patterns;
@property NSPopUpButton *picker;
- (void)apply:(id)sender;
@end

@implementation MFFileFilter
- (void)apply:(id)sender {
    NSInteger i = self.picker.indexOfSelectedItem;
    if (i < 0 || i >= (NSInteger)self.patterns.count) return;
    NSMutableArray<UTType *> *types = [NSMutableArray array];
    for (NSString *glob in [self.patterns[i] componentsSeparatedByString:@";"]) {
        NSString *pattern = [glob stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceCharacterSet];
        if ([pattern isEqualToString:@"*"] || [pattern isEqualToString:@"*.*"]) {
            [types removeAllObjects];
            break;
        }
        if ([pattern hasPrefix:@"*."] && pattern.length > 2) {
            UTType *type = [UTType typeWithFilenameExtension:[pattern substringFromIndex:2]];
            if (type) [types addObject:type];
        }
    }
    self.panel.allowedContentTypes = types;
}
@end

char *mf_pick_file(void *ownerView, int kind, const char *json) {
    @autoreleasepool {
        mf_init();
        NSDictionary *opts = [NSJSONSerialization JSONObjectWithData:[MFString(json) dataUsingEncoding:NSUTF8StringEncoding]
                                                             options:0 error:nil];
        BOOL save = kind == 1, folder = kind == 2;
        NSSavePanel *panel = save ? NSSavePanel.savePanel : NSOpenPanel.openPanel;
        if ([opts[@"Title"] length]) panel.title = opts[@"Title"];
        if ([opts[@"Message"] length]) panel.message = opts[@"Message"];
        NSString *directory = opts[@"Directory"], *filename = opts[@"FileName"];
        if (filename.isAbsolutePath) directory = filename.stringByDeletingLastPathComponent;
        if (directory.length) panel.directoryURL = [NSURL fileURLWithPath:directory isDirectory:YES];
        if (filename.length) panel.nameFieldStringValue = filename.lastPathComponent;
        panel.canCreateDirectories = [opts[@"CreateFolders"] boolValue];
        if (!save) {
            NSOpenPanel *open = (NSOpenPanel *)panel;
            open.canChooseFiles = !folder;
            open.canChooseDirectories = folder;
            open.allowsMultipleSelection = [opts[@"Multiselect"] boolValue];
        }
        MFFileFilter *filter = [[MFFileFilter alloc] init];
        filter.panel = panel;
        NSArray *parts = [opts[@"Filter"] componentsSeparatedByString:@"|"];
        NSMutableArray *patterns = [NSMutableArray array];
        if (!folder && parts.count >= 2) {
            NSPopUpButton *picker = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(0, 0, 300, 28) pullsDown:NO];
            for (NSUInteger i = 0; i + 1 < parts.count; i += 2) {
                [picker.menu addItemWithTitle:parts[i] action:nil keyEquivalent:@""];
                [patterns addObject:parts[i + 1]];
            }
            filter.patterns = patterns;
            filter.picker = picker;
            picker.target = filter;
            picker.action = @selector(apply:);
            [picker selectItemAtIndex:MAX(0, MIN((int)patterns.count - 1, [opts[@"FilterIndex"] intValue] - 1))];
            panel.accessoryView = picker;
            if (!save) ((NSOpenPanel *)panel).accessoryViewDisclosed = YES;
            [filter apply:nil];
        } else if (save && [opts[@"DefaultExt"] length]) {
            UTType *type = [UTType typeWithFilenameExtension:opts[@"DefaultExt"]];
            if (type) { panel.allowedContentTypes = @[type]; panel.allowsOtherFileTypes = YES; }
        }
        NSWindow *parent = ((__bridge NSView *)ownerView).window;
        if (parent) {
            __block NSModalResponse response = NSModalResponseContinue;
            [panel beginSheetModalForWindow:parent completionHandler:^(NSModalResponse answer) { response = answer; }];
            while (response == NSModalResponseContinue) mf_pump();
            if (response != NSModalResponseOK) return NULL;
        } else if ([panel runModal] != NSModalResponseOK) return NULL;
        NSArray<NSURL *> *urls = save ? @[panel.URL] : ((NSOpenPanel *)panel).URLs;
        NSDictionary *result = @{@"Paths": [urls valueForKey:@"path"],
                                 @"FilterIndex": @(filter.picker.indexOfSelectedItem + 1)};
        NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:nil];
        NSString *value = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        return strdup(value.UTF8String);
    }
}

void mf_free(void *data) { free(data); }

void mf_mouse_position(void *handle, int *x, int *y) {
    MFCanvas *canvas = (MFCanvas *)MFHandle(handle).container;
    *x = (int)canvas.lastMouse.x;
    *y = (int)canvas.lastMouse.y;
}

void mf_invalidate(void *handle) {
    MFControl *h = MFHandle(handle);
    (h.container ?: h.view).needsDisplay = YES;
}

void mf_progress(void *handle, int min, int max, int value, int marquee) {
    NSProgressIndicator *bar = MFHandle(handle).progress;
    bar.minValue = min;
    bar.maxValue = max;
    bar.indeterminate = marquee;
    bar.needsDisplay = YES;
    if (marquee) [bar startAnimation:nil];
    else { [bar stopAnimation:nil]; bar.doubleValue = value; }
}

void mf_track(void *handle, int min, int max, int value, int ticks, int style, int vertical) {
    MFTrackBar *slider = (MFTrackBar *)MFHandle(handle).control;
    slider.minValue = min;
    slider.maxValue = max;
    slider.integerValue = value;
    slider.vertical = vertical;
    slider.tickStyle = style;
    slider.tickFrequency = ticks;
    slider.needsDisplay = YES;
    slider.numberOfTickMarks = style == 0 || ticks <= 0 ? 0 : MIN(256, 1 + ((long long)max - min) / ticks);
    slider.allowsTickMarkValuesOnly = NO;
}

int mf_track_value(void *handle) { return (int)MFHandle(handle).control.integerValue; }

void mf_scrollbar_set(void *handle,double position,double proportion) {
    NSScroller *bar = (NSScroller *)MFHandle(handle).control;
    bar.knobProportion = proportion;
    bar.doubleValue = position;
}
double mf_scrollbar_position(void *handle) { return MFHandle(handle).control.doubleValue; }

void mf_tree_appearance(void *handle,int indent,int height,int lines,int rootLines,int plusMinus,int fullRow,int hideSelection,int lineArgb,int borderStyle) {
    MFControl *h=MFHandle(handle);
    MFStyleListBorder((NSScrollView *)h.view,borderStyle);
    MFSetTreeAppearance(h.tree,indent,height,lines,rootLines,plusMinus,fullRow,hideSelection,lineArgb);
}

void mf_tree_nodes(void *handle, const char *json) {
    MFControl *h = MFHandle(handle);
    NSArray *entries = MFJsonArray(json);
    NSMutableDictionary *nodes = [NSMutableDictionary dictionary];
    NSMutableDictionary<NSNumber *,NSMutableArray *> *branches = [NSMutableDictionary dictionary];
    NSMutableArray *expanded = [NSMutableArray array];
    for (NSDictionary *entry in entries) {
        NSNumber *key = entry[@"Id"], *parent = entry[@"Parent"];
        nodes[key] = entry;
        if (!branches[parent]) branches[parent] = [NSMutableArray array];
        [branches[parent] addObject:key];
        BOOL open = h.nodeInfo[key] ? [h.tree isItemExpanded:key] : [entry[@"Expanded"] boolValue];
        if (open) [expanded addObject:key];
    }
    h.suppressEvents = YES;
    h.nodeInfo = nodes;
    h.branches = branches;
    [h.tree reloadData];
    for (NSNumber *key in expanded) [h.tree expandItem:key];
    h.suppressEvents = NO;
}

void mf_tree_select(void *handle, int id) {
    MFControl *h = MFHandle(handle);
    h.suppressEvents = YES;
    NSNumber *node = @(id);
    if (h.nodeInfo[node]) {
        NSMutableArray *lineage = [NSMutableArray array];
        NSNumber *parent = h.nodeInfo[node][@"Parent"];
        while (h.nodeInfo[parent]) {
            [lineage addObject:parent];
            parent = h.nodeInfo[parent][@"Parent"];
        }
        for (NSNumber *key in lineage.reverseObjectEnumerator) [h.tree expandItem:key];
        NSInteger row = [h.tree rowForItem:node];
        if (row >= 0) {
            [h.tree selectRowIndexes:[NSIndexSet indexSetWithIndex:row] byExtendingSelection:NO];
            [h.tree scrollRowToVisible:row];
        }
    } else [h.tree deselectAll:nil];
    h.suppressEvents = NO;
}

int mf_tree_selected(void *handle) {
    MFControl *h = MFHandle(handle);
    NSInteger row = h.tree.selectedRow;
    return row < 0 ? 0 : [[h.tree itemAtRow:row] intValue];
}

void mf_tree_expand(void *handle, int id, int expand, int recursive) {
    NSOutlineView *tree = MFHandle(handle).tree;
    NSNumber *node = id ? @(id) : nil;
    if (expand) [tree expandItem:node expandChildren:recursive];
    else [tree collapseItem:node collapseChildren:recursive];
}

int mf_tree_expanded(void *handle, int id) { return [MFHandle(handle).tree isItemExpanded:@(id)]; }

NSView *MFWidgetView(void *handle) { return MFHandle(handle).view; }

void mf_allow_drop(void *handle,int enabled) {
    MFControl *h = MFHandle(handle);
    mf_drop_enable(h.view ?: h.container,handle,enabled);
}
int mf_drag_data(void *handle,const char *json,int effects) {
    MFControl *h = MFHandle(handle);
    return mf_begin_drag(h.view ?: h.container,json,effects);
}

void mf_scroll_config(void *handle,int enabled,int w,int height) {
    MFControl *h = MFHandle(handle);
    NSScrollView *scroll = h.autoScroll;
    if (!enabled && !scroll) return;
    if (!h.container) return;
    if (enabled && !scroll) {
        NSView *host = h.container;
        NSArray *children = host.subviews.copy;
        scroll = [[NSScrollView alloc] initWithFrame:host.bounds];
        scroll.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;
        scroll.hasHorizontalScroller = YES;
        scroll.hasVerticalScroller = YES;
        scroll.autohidesScrollers = YES;
        scroll.drawsBackground = NO;
        MFCanvas *doc = [[MFCanvas alloc] initWithFrame:host.bounds];
        scroll.documentView = doc;
        [host addSubview:scroll];
        for (NSView *child in children) [doc addSubview:child];
        h.container = doc;
        h.autoScroll = scroll;
    } else if (!enabled) {
        NSView *host = scroll.superview;
        for (NSView *child in h.container.subviews.copy) [host addSubview:child];
        [scroll removeFromSuperview];
        h.container = host;
        h.autoScroll = nil;
        return;
    }
    h.container.frame = NSMakeRect(0,0,MAX(w,scroll.contentSize.width),MAX(height,scroll.contentSize.height));
}
void mf_scroll_position(void *handle,int set,int *x,int *y) {
    NSScrollView *scroll = MFHandle(handle).autoScroll;
    if (set && scroll) {
        NSRect proposed = scroll.contentView.bounds;
        proposed.origin = NSMakePoint(*x,*y);
        [scroll.contentView scrollToPoint:[scroll.contentView constrainBoundsRect:proposed].origin];
        [scroll reflectScrolledClipView:scroll.contentView];
    }
    *x = (int)scroll.contentView.bounds.origin.x;
    *y = (int)scroll.contentView.bounds.origin.y;
}
void mf_coordinate(void *handle,int toScreen,int *x,int *y) {
    MFControl *h = MFHandle(handle);
    NSView *v = h.view ?: h.window.contentView;
    CGFloat top = NSMaxY(NSScreen.screens.firstObject.frame);
    NSPoint p;
    if (toScreen) {
        p = NSMakePoint(*x,v.isFlipped ? *y : NSHeight(v.bounds)-*y);
        p = [v.window convertPointToScreen:[v convertPoint:p toView:nil]];
        p.y = top-p.y;
    } else {
        p = [v convertPoint:[v.window convertPointFromScreen:NSMakePoint(*x,top-*y)] fromView:nil];
        if (!v.isFlipped) p.y = NSHeight(v.bounds)-p.y;
    }
    *x=(int)round(p.x); *y=(int)round(p.y);
}

void mf_context_menu(void *handle,void *menu) {
    MFControl *h = MFHandle(handle);
    (h.view ?: h.container).menu = MFHandle(menu).menuItem.submenu;
}
void mf_popup(void *menu,void *control,int x,int y) {
    MFControl *owner = MFHandle(control);
    NSView *view = owner.view ?: owner.container;
    NSPoint p = view ? NSMakePoint(x,view.isFlipped ? y : NSHeight(view.bounds)-y) : NSMakePoint(x,NSMaxY(NSScreen.screens.firstObject.frame)-y);
    [MFHandle(menu).menuItem.submenu popUpMenuPositioningItem:nil atLocation:p inView:view];
}
void mf_cancel_menu(void *handle) { [MFHandle(handle).menuItem.submenu cancelTracking]; }
NSMenu *mf_native_menu(void *handle) { return MFHandle(handle).menuItem.submenu; }
void mf_button_state(void *handle,int value) {
    NSButton *b = (NSButton *)MFHandle(handle).control;
    b.state = value ? NSControlStateValueOn : NSControlStateValueOff;
    b.needsDisplay = YES;
}
void mf_item_image(void *handle,void *image,int display) {
    MFControl *h = MFHandle(handle);
    NSImage *icon = display >= 2 ? mf_native_image(image) : nil;
    h.menuItem.image = icon;
    if ([h.control isKindOfClass:NSButton.class]) {
        NSButton *button = (NSButton *)h.control;
        button.image = icon;
        button.imagePosition = icon ? (display == 2 ? NSImageOnly : NSImageLeft) : NSNoImage;
        if ([button.cell isKindOfClass:MFButtonCell.class]) ((MFButtonCell *)button.cell).hideCaption = display == 0 || display == 2;
        button.needsDisplay = YES;
    }
}

void mf_form_icon(void *handle,void *image) {
    [MFHandle(handle).caption setFormImage:mf_native_image(image)];
}

void *mf_widget_view(void *handle) {
    MFControl *h = MFHandle(handle);
    return (__bridge void *)(h.view ?: h.container);
}
void mf_calendar(void *handle,double min,double max,double start,double end,int multiple) {
    NSDatePicker *cal = (NSDatePicker *)MFHandle(handle).control;
    cal.minDate = [NSDate dateWithTimeIntervalSince1970:min]; cal.maxDate = [NSDate dateWithTimeIntervalSince1970:max];
    cal.datePickerMode = multiple ? NSDatePickerModeRange : NSDatePickerModeSingle;
    cal.dateValue = [NSDate dateWithTimeIntervalSince1970:start];
    if (multiple) cal.timeInterval = MAX(0,end-start);
}
double mf_calendar_end(void *handle) {
    NSDatePicker *cal = (NSDatePicker *)MFHandle(handle).control;
    return cal.dateValue.timeIntervalSince1970 + cal.timeInterval;
}

void mf_update_view(void *handle) {
    MFControl *h = MFHandle(handle); [(h.view ?: h.container) displayIfNeeded];
}

void mf_updown_options(void *handle,int readOnly,int side,int align) {
    MFControl *h=MFHandle(handle);
    h.stepperLeft=side==0;
    ((NSTextField *)h.control).editable=!readOnly;
    h.control.alignment=align==1 ? NSTextAlignmentRight : align==2 ? NSTextAlignmentCenter : NSTextAlignmentLeft;
    NSRect area=h.view.frame;
    mf_frame(handle,(int)area.origin.x,(int)area.origin.y,(int)area.size.width,(int)area.size.height);
}
int mf_link_index(void *handle) { return MFHandle(handle).linkIndex; }
void mf_link_style(void *handle,const char *json) {
    MFControl *h=MFHandle(handle);
    NSTextView *editor=h.editor;
    NSRange whole=NSMakeRange(0,editor.string.length);
    [editor.textStorage removeAttribute:NSLinkAttributeName range:whole];
    [editor.textStorage removeAttribute:NSUnderlineStyleAttributeName range:whole];
    [editor.textStorage addAttribute:NSForegroundColorAttributeName value:NSColor.labelColor range:whole];
    NSArray *spans=MFJsonArray(json);
    [(MFLinkText *)editor clearHover];
    ((MFLinkText *)editor).underlineOnHover=[spans.firstObject[@"hoverUnderline"] boolValue];
    for (NSDictionary *spec in spans) {
        NSUInteger start=[spec[@"start"] unsignedIntegerValue];
        if (start>=whole.length) continue;
        NSInteger count=[spec[@"length"] integerValue];
        NSRange range=NSMakeRange(start,count<0 ? whole.length-start : MIN((NSUInteger)count,whole.length-start));
        if (!range.length) continue;
        if ([spec[@"enabled"] boolValue]) [editor.textStorage addAttribute:NSLinkAttributeName value:[NSURL URLWithString:[NSString stringWithFormat:@"macwinforms://link/%@",spec[@"index"]]] range:range];
        uint32_t argb=[spec[@"color"] unsignedIntValue];
        NSColor *ink=[NSColor colorWithSRGBRed:((argb>>16)&255)/255.0 green:((argb>>8)&255)/255.0 blue:(argb&255)/255.0 alpha:(argb>>24)/255.0];
        [editor.textStorage addAttribute:NSForegroundColorAttributeName value:ink range:range];
        if ([spec[@"underline"] boolValue]) [editor.textStorage addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:range];
    }
}

void mf_mdi_activate(void *handle) { [(MFMdiFrame *)MFHandle(handle).view activateChild]; }
void mf_mdi_location(void *handle,int *x,int *y) { NSPoint p=MFHandle(handle).view.frame.origin; *x=(int)p.x; *y=(int)p.y; }

void mf_split_divider(void *handle,int x,int y,int w,int h,int vertical) {
    MFSplitterCanvas *canvas=(MFSplitterCanvas *)MFWidgetView(handle);
    canvas.divider=NSMakeRect(x,y,w,h); canvas.vertical=vertical!=0;
    [canvas.window invalidateCursorRectsForView:canvas];
}

int mf_form_state(void *handle,int state) {
    MFControl *h = MFHandle(handle);
    NSWindow *w = h.window;
    if (!w) return 0;
    if (state == 1 && !w.miniaturized) [w miniaturize:nil];
    if (state == 0 || state == 2) {
        if (w.miniaturized) [w deminiaturize:nil];
        [h.caption setMaximized:state == 2];
    }
    return w.miniaturized ? 1 : h.caption.isMaximized ? 2 : 0;
}
void mf_form_appearance(void *handle,int top,double opacity,int border,int controlBox,int minimize,int maximize,int minWidth,int minHeight) {
    MFControl *h = MFHandle(handle);
    if (!h.window) { h.view.alphaValue = opacity; return; }
    NSWindow *w = h.window;
    BOOL suppress = h.suppressEvents;
    h.suppressEvents = YES;
    NSSize client = w.contentView.bounds.size;
    NSPoint topLeft = NSMakePoint(NSMinX(w.frame),NSMaxY(w.frame));
    NSWindowStyleMask style = w.styleMask & ~(NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable | NSWindowStyleMaskResizable);
    if (border) {
        style |= NSWindowStyleMaskTitled;
        if (border == 4 || border == 6) style |= NSWindowStyleMaskResizable;
        if (controlBox) style |= NSWindowStyleMaskClosable;
        if (minimize) style |= NSWindowStyleMaskMiniaturizable;
    }
    if (!border && (w.styleMask & NSWindowStyleMaskTitled))
        [h.caption removeFromParentViewController];
    if (w.styleMask != style) w.styleMask = style;
    [h.caption updateBorder:border controlBox:controlBox minimize:minimize maximize:maximize];
    w.contentMinSize = NSMakeSize(MAX(0,minWidth),MAX(0,minHeight));
    if (!NSEqualSizes(client,w.contentView.bounds.size)) {
        [w setContentSize:client]; [w setFrameTopLeftPoint:topLeft];
    }
    w.level = top ? NSFloatingWindowLevel : NSNormalWindowLevel;
    w.alphaValue = opacity;
    h.suppressEvents = suppress;
}

int mf_tab_requested(void *handle) { return (int)((MFNotebook *)MFHandle(handle).view).requested; }
void mf_tab_choose(void *handle,int index) {
    MFNotebook *notebook=(MFNotebook *)MFHandle(handle).view;
    if (index>=notebook.tabs.numberOfTabViewItems) return;
    notebook.noSelection=index<0; notebook.tabs.hidden=index<0;
    if(index>=0) [notebook.tabs selectTabViewItemAtIndex:index];
    [notebook refresh];
}
void mf_tab_position(void *handle,void *page,int index,void *image,const char *tooltip) {
    MFNotebook *book=(MFNotebook *)MFHandle(handle).view;
    NSTabViewItem *item=MFHandle(page).page;
    if (item.tabView!=book.tabs || index>=book.tabs.numberOfTabViewItems) return;
    if ([book.tabs indexOfTabViewItem:item]!=index) {
        [book.tabs removeTabViewItem:item]; [book.tabs insertTabViewItem:item atIndex:index];
    }
    NSButton *button=book.buttons[index];
    button.image=mf_native_image(image); button.imagePosition=image ? NSImageLeft:NSNoImage;
    button.toolTip=MFString(tooltip);
}
void mf_tab_rect(void *handle,int index,int *x,int *y,int *w,int *h) {
    MFNotebook *book=(MFNotebook *)MFHandle(handle).view; NSRect r=book.buttons[index].frame;
    *x=r.origin.x; *y=r.origin.y; *w=r.size.width; *h=r.size.height;
}
