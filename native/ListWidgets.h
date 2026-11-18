#import <AppKit/AppKit.h>

@class MFListHost;
NSScrollView *MFCreateList(void (^action)(int event));
NSView *MFWidgetView(void *handle);
void MFSetListEnabled(NSScrollView *host, BOOL enabled);

void MFSetListTextStyle(NSScrollView *scroll,NSFont *font,NSColor *color);
