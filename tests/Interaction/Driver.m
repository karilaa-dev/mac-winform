#import <AppKit/AppKit.h>
#import <objc/runtime.h>

// Redirect the public Clipboard calls made by this test process to a fresh,
// private system pasteboard. No contents of the user's pasteboard are read.
static NSPasteboard *testBoard;
static IMP previousBoardGetter;
static NSPasteboard *PrivateBoard(id cls, SEL selector) { return testBoard; }
void start_board(void) {
    testBoard = [NSPasteboard pasteboardWithUniqueName];
    Method method = class_getClassMethod(NSPasteboard.class,@selector(generalPasteboard));
    previousBoardGetter = method_setImplementation(method,(IMP)PrivateBoard);
}
void finish_board(void) {
    method_setImplementation(class_getClassMethod(NSPasteboard.class,@selector(generalPasteboard)),previousBoardGetter);
    [testBoard releaseGlobally];
    testBoard = nil;
}
int has_external_formats(int count, const char *text) {
    if (![[testBoard stringForType:NSPasteboardTypeString] isEqualToString:@(text)]) return 0;
    NSArray *urls = [testBoard readObjectsForClasses:@[NSURL.class] options:@{NSPasteboardURLReadingFileURLsOnlyKey:@YES}];
    return urls.count == count;
}
int external_binary(int kind) {
    return [testBoard dataForType:kind == 0 ? NSPasteboardTypePNG : @"com.microsoft.waveform-audio"].length > 0;
}
void external_files(const char *first, const char *second) {
    [testBoard clearContents];
    [testBoard writeObjects:@[[NSURL fileURLWithPath:@(first)],[NSURL fileURLWithPath:@(second)]]];
}
static NSWindow *Window(void) {
    for (NSWindow *window in NSApp.windows)
        if ([window.title isEqualToString:@"Mac WinForms interaction probe"]) return window;
    [NSException raise:NSInternalInconsistencyException format:@"Probe window missing"];
    return nil;
}
static NSView *Descendant(NSView *view,BOOL (^matches)(NSView *)) {
    if (matches(view)) return view;
    for (NSView *sub in view.subviews) {
        NSView *found = Descendant(sub,matches);
        if (found) return found;
    }
    return nil;
}
int click_toolbar(const char *name) {
    NSButton *button = (NSButton *)Descendant(Window().contentView,^BOOL(NSView *v) {
        return [v isKindOfClass:NSButton.class] && [v.toolTip isEqualToString:@(name)];
    });
    if (!button || !button.enabled || !button.image) return 0;
    [button performClick:nil];
    return 1;
}
int move_thumb(int horizontal,double fraction) {
    NSScroller *bar = (NSScroller *)Descendant(Window().contentView,^BOOL(NSView *v) {
        return [v isKindOfClass:NSScroller.class] && v.frame.size.width > 0 &&
            ((v.frame.size.width > v.frame.size.height) == horizontal);
    });
    if (!bar || !bar.action || bar.knobProportion <= 0 || bar.knobProportion >= 1) return 0;
    bar.doubleValue = fraction;
    return [bar sendAction:bar.action to:bar.target];
}
void send_key(int code, const char *text, int modifiers) {
    NSWindow *window = Window();
    NSEventModifierFlags flags = (modifiers & 1 ? NSEventModifierFlagShift : 0) | (modifiers & 2 ? NSEventModifierFlagControl : 0);
    for (int i=0; i<2; i++) {
        NSEvent *e = [NSEvent keyEventWithType:i==0 ? NSEventTypeKeyDown : NSEventTypeKeyUp location:NSZeroPoint
            modifierFlags:flags timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:window.windowNumber
            context:nil characters:@(text) charactersIgnoringModifiers:@(text) isARepeat:NO keyCode:code];
        [window sendEvent:e];
    }
}

// AppKit accepts NSDraggingInfo from the window server. Supply its public event
// properties locally so this regression also runs with no user drag in flight.
@interface DropInfo : NSObject
@property NSPoint draggingLocation;
@property NSPasteboard *draggingPasteboard;
@property NSWindow *draggingDestinationWindow;
@property NSDragOperation draggingSourceOperationMask;
@end
@implementation DropInfo @end

int simulate_drop(int phase,int x,int y) {
    DropInfo *info = [[DropInfo alloc] init];
    NSWindow *window = Window();
    info.draggingPasteboard = testBoard;
    info.draggingDestinationWindow = window;
    info.draggingLocation = [window.contentView convertPoint:NSMakePoint(x,y) toView:nil];
    info.draggingSourceOperationMask = NSDragOperationCopy;
    id<NSDraggingInfo> event = (id)info;
    id<NSDraggingDestination> destination = (id)window;
    switch (phase) {
        case 1: return (int)[destination draggingEntered:event];
        case 2: return (int)[destination draggingUpdated:event];
        case 3: [destination draggingExited:event]; return 0;
        case 4: return [destination performDragOperation:event];
        default: return -1;
    }
}
