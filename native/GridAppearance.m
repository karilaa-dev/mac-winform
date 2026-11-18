#import "GridAppearance.h"

NSColor *MFGridColor(int argb) {
    uint32_t color = (uint32_t)argb;
    return [NSColor colorWithSRGBRed:((color >> 16) & 255) / 255.0 green:((color >> 8) & 255) / 255.0
                                blue:(color & 255) / 255.0 alpha:(color >> 24) / 255.0];
}

@interface MFGridHeaderCell : NSTableHeaderCell
@end
@implementation MFGridHeaderCell
- (void)drawWithFrame:(NSRect)rect inView:(NSView *)view {
    MFGridTable *grid = (MFGridTable *)((NSTableHeaderView *)view).tableView;
    [NSColor.whiteColor setFill];
    NSRectFill(rect);
    [MFGridColor(0xfff1f1f1) setFill];
    NSRectFill(NSMakeRect(NSMinX(rect)+1,NSMinY(rect)+1,NSWidth(rect)-2,1));
    NSRectFill(NSMakeRect(NSMinX(rect)+1,NSMinY(rect)+1,1,NSHeight(rect)-2));
    [grid.gridColor setFill];
    NSRectFill(NSMakeRect(NSMinX(rect),NSMinY(rect),NSWidth(rect),1));
    NSRectFill(NSMakeRect(NSMinX(rect),NSMaxY(rect)-1,NSWidth(rect),1));
    NSRectFill(NSMakeRect(NSMaxX(rect)-1,NSMinY(rect),1,NSHeight(rect)));
    if (NSMinX(rect) < 1) NSRectFill(NSMakeRect(NSMinX(rect),NSMinY(rect),1,NSHeight(rect)));
    NSFont *font = grid.captionFont ?: [NSFont systemFontOfSize:12];
    NSMutableParagraphStyle *paragraph = [NSMutableParagraphStyle new];
    paragraph.lineBreakMode = NSLineBreakByTruncatingTail;
    NSDictionary *style = @{NSFontAttributeName:font, NSForegroundColorAttributeName:NSColor.blackColor, NSParagraphStyleAttributeName:paragraph};
    CGFloat height = ceil([@"Ag" sizeWithAttributes:style].height);
    CGFloat inset = NSMinX(rect) < 1 ? 10 : 7;
    [self.stringValue drawInRect:NSMakeRect(rect.origin.x+inset,floor(NSMidY(rect)-height/2),MAX(0,rect.size.width-inset-3),height)
                  withAttributes:style];
}
@end

@interface MFGridTextFieldCell : NSTextFieldCell
@end

@implementation MFGridTextFieldCell
- (NSRect)drawingRectForBounds:(NSRect)bounds {
    NSRect text = [super drawingRectForBounds:bounds];
    NSFont *font = self.font ?: [NSFont systemFontOfSize:12];
    CGFloat height = MIN(NSHeight(text),ceil(font.ascender-font.descender+font.leading));
    text.origin.y += floor((NSHeight(text)-height)/2);
    text.size.height = height;
    text.origin.x += 2;
    text.size.width = MAX(0,NSWidth(text)-3);
    return text;
}
@end

@interface MFGridTextField : NSTextField
@end
@implementation MFGridTextField
+ (Class)cellClass { return MFGridTextFieldCell.class; }
@end

NSTextField *MFMakeGridField(void) { return [MFGridTextField labelWithString:@""]; }

@implementation MFGridColumn
- (instancetype)initWithIdentifier:(NSUserInterfaceItemIdentifier)identifier {
    if ((self = [super initWithIdentifier:identifier])) self.headerCell = [[MFGridHeaderCell alloc] initTextCell:@""];
    return self;
}
- (void)configurePalette:(NSDictionary *)style {
    self.cellBack = MFGridColor([style[@"back"] intValue]);
    self.cellFore = MFGridColor([style[@"fore"] intValue]);
    self.selectionBack = MFGridColor([style[@"selectedBack"] intValue]);
    self.selectionFore = MFGridColor([style[@"selectedFore"] intValue]);
    NSNumber *pointSize = style[@"size"];
    CGFloat size = [pointSize isKindOfClass:NSNumber.class] ? pointSize.doubleValue * 96.0/72.0 : 0;
    NSString *name = style[@"font"];
    if ([name isKindOfClass:NSString.class] && size > 0) {
        NSFont *font = [NSFont fontWithName:name size:size] ?: [NSFont fontWithName:@"Tahoma" size:size] ?: [NSFont systemFontOfSize:size];
        NSFontTraitMask traits = ([style[@"bold"] boolValue] ? NSBoldFontMask : 0) | ([style[@"italic"] boolValue] ? NSItalicFontMask : 0);
        self.cellFont = traits ? [NSFontManager.sharedFontManager convertFont:font toHaveTrait:traits] : font;
    }
}
@end

@interface MFGridRowView : NSTableRowView
@property (weak) MFGridTable *grid;
@property NSInteger dataRow;
@end
@implementation MFGridRowView
- (NSBackgroundStyle)interiorBackgroundStyle { return NSBackgroundStyleNormal; }
- (void)drawRect:(NSRect)dirty {
    MFGridTable *grid = self.grid;
    [grid.backgroundColor setFill]; NSRectFill(dirty);
    NSIndexSet *visible = [grid columnIndexesInRect:[self convertRect:dirty toView:grid]];
    [visible enumerateIndexesUsingBlock:^(NSUInteger index, BOOL *stop) {
        MFGridColumn *column = (MFGridColumn *)grid.tableColumns[index];
        NSRect rect = [grid rectOfColumn:index];
        rect.origin.y = 0;
        rect.size.height = NSHeight(self.bounds);
        BOOL selected = [grid highlightsRow:self.dataRow column:index];
        [(selected ? column.selectionBack : column.cellBack) setFill];
        NSRectFill(rect);
        [grid.gridColor setFill];
        NSRectFill(NSMakeRect(NSMaxX(rect)-1,NSMinY(rect),1,NSHeight(rect)));
        if (index == 0) NSRectFill(NSMakeRect(NSMinX(rect),NSMinY(rect),1,NSHeight(rect)));
        NSRectFill(NSMakeRect(NSMinX(rect),self.flipped ? NSMaxY(rect)-1 : NSMinY(rect),NSWidth(rect),1));
    }];
}
@end

NSTableRowView *MFMakeGridRow(MFGridTable *table, NSInteger row) {
    MFGridRowView *view = [table makeViewWithIdentifier:@"winforms-grid-row" owner:nil];
    if (!view) {
        view = [MFGridRowView new];
        view.identifier = @"winforms-grid-row";
        view.selectionHighlightStyle = NSTableViewSelectionHighlightStyleNone;
    }
    view.grid = table;
    view.dataRow = row;
    return view;
}

@implementation MFGridTable
- (NSArray *)accessibilitySelectedRows {
    return self.selectionMode == 1 ? super.accessibilitySelectedRows : @[];
}
- (NSArray *)accessibilitySelectedCells {
    if (self.selectionMode != 0) return super.accessibilitySelectedCells;
    if (self.selectedRow < 0 || self.selectedRow >= self.numberOfRows || self.activeColumn < 0 ||
        self.activeColumn >= self.numberOfColumns) return @[];
    id cell = [self accessibilityCellForColumn:self.activeColumn row:self.selectedRow] ?:
        [self viewAtColumn:self.activeColumn row:self.selectedRow makeIfNecessary:YES];
    return cell ? @[cell] : @[];
}
- (NSArray *)accessibilitySelectedColumns {
    if (self.selectionMode != 2 || self.selectedRow < 0 || self.activeColumn < 0) return @[];
    NSArray *columns = self.accessibilityColumns;
    return self.activeColumn < (NSInteger)columns.count ? @[columns[self.activeColumn]] : @[];
}
- (BOOL)highlightsRow:(NSInteger)row column:(NSInteger)column {
    if (self.selectedRow < 0 || self.activeColumn < 0) return NO;
    if (self.selectionMode == 2) return column == self.activeColumn;
    return self.selectedRow == row && (self.selectionMode == 1 || column == self.activeColumn);
}
- (void)refreshSelection {
    [self enumerateAvailableRowViewsUsingBlock:^(NSTableRowView *rowView, NSInteger row) {
        rowView.needsDisplay = YES;
        for (NSView *cell in rowView.subviews) {
            if (![cell isKindOfClass:NSTextField.class]) continue;
            NSTextField *field = (NSTextField *)cell;
            if (field.currentEditor) continue;
            NSInteger column = [self columnForView:cell];
            if (column < 0) continue;
            MFGridColumn *descriptor = (MFGridColumn *)self.tableColumns[column];
            field.textColor = [self highlightsRow:row column:column] ? descriptor.selectionFore : descriptor.cellFore;
        }
    }];
}
@end
