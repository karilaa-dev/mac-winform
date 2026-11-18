#import <AppKit/AppKit.h>

@interface MFGridColumn : NSTableColumn
@property CGFloat fixedWidth;
@property double fillWeight;
@property BOOL checkBox;
@property BOOL allowsEdit;
@property NSArray<NSString *> *choices;
@property NSColor *cellBack, *cellFore, *selectionBack, *selectionFore;
@property NSFont *cellFont;
- (void)configurePalette:(NSDictionary *)style;
@end

@interface MFGridTable : NSTableView
@property NSInteger activeColumn;
@property NSInteger selectionMode;
@property NSFont *captionFont;
- (void)refreshSelection;
- (BOOL)highlightsRow:(NSInteger)row column:(NSInteger)column;
@end

NSTableRowView *MFMakeGridRow(MFGridTable *table, NSInteger row);
NSTextField *MFMakeGridField(void);
NSColor *MFGridColor(int argb);
