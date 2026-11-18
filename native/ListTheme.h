#import <AppKit/AppKit.h>

void MFStyleScrollBorder(NSScrollView *scroll, NSColor *edge);
void MFStyleListBorder(NSScrollView *scroll, NSInteger borderStyle);
BOOL MFListHasFocus(NSTableView *table);
NSColor *MFListSelectionColor(BOOL focused);
NSOutlineView *MFCreateTree(void);
NSTableRowView *MFCreateTreeRow(NSOutlineView *tree);
NSTextField *MFTreeLabel(NSOutlineView *tree, NSString *caption);
void MFSetTreeTextStyle(NSOutlineView *tree,NSFont *font,NSColor *color);
void MFSetTreeAppearance(NSOutlineView *tree,int indent,int height,BOOL lines,BOOL rootLines,
                         BOOL plusMinus,BOOL fullRow,BOOL hideSelection,int lineArgb);
