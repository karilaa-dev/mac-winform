#import "Transfer.h"
#import "Keyboard.h"
#import <objc/runtime.h>

static NSPasteboard *Board(const char *name) {
    return name ? [NSPasteboard pasteboardWithName:@(name)] : NSPasteboard.generalPasteboard;
}
static NSDictionary *TextFormats(void) {
    return @{@"UnicodeText":NSPasteboardTypeString,@"HTML Format":NSPasteboardTypeHTML,
             @"Rich Text Format":NSPasteboardTypeRTF,@"Csv":@"public.comma-separated-values-text"};
}
static NSDictionary *BinaryFormats(void) {
    return @{@"Bitmap":NSPasteboardTypePNG,@"WaveAudio":@"com.microsoft.waveform-audio"};
}
static NSString *const privateFormat = @"org.macwinforms.data.v1";
static NSArray<NSPasteboardItem *> *Items(NSDictionary *data) {
    NSMutableArray *items = [NSMutableArray array];
    NSPasteboardItem *first = [[NSPasteboardItem alloc] init];
    // Preserve custom string and byte payloads as JSON, without object deserialization.
    [first setData:[NSJSONSerialization dataWithJSONObject:data options:0 error:nil] forType:privateFormat];
    for (NSString *key in TextFormats()) {
        NSString *value = data[key];
        if ([value isKindOfClass:NSString.class]) [first setString:value forType:TextFormats()[key]];
    }
    for (NSString *key in BinaryFormats()) {
        NSDictionary *entry = data[key];
        if (![entry isKindOfClass:NSDictionary.class] || ![entry[@"bytes"] isKindOfClass:NSString.class]) continue;
        NSData *bytes = [[NSData alloc] initWithBase64EncodedString:entry[@"bytes"] options:0];
        if (bytes) [first setData:bytes forType:BinaryFormats()[key]];
    }
    [items addObject:first];
    for (NSString *path in data[@"FileDrop"]) {
        NSPasteboardItem *item = items.count == 1 && ![first.types containsObject:NSPasteboardTypeFileURL] ? first : [[NSPasteboardItem alloc] init];
        [item setString:[NSURL fileURLWithPath:path].absoluteString forType:NSPasteboardTypeFileURL];
        if (item != first) [items addObject:item];
    }
    return items;
}
static NSDictionary *Payload(NSPasteboard *board) {
    NSData *custom = [board dataForType:privateFormat];
    id decoded = custom ? [NSJSONSerialization JSONObjectWithData:custom options:0 error:nil] : nil;
    if ([decoded isKindOfClass:NSDictionary.class]) return decoded;
    NSMutableDictionary *values = [NSMutableDictionary dictionary];
    for (NSString *key in TextFormats()) {
        NSString *text = [board stringForType:TextFormats()[key]];
        if (text) values[key] = text;
    }
    for (NSString *key in BinaryFormats()) {
        NSData *bytes = [board dataForType:BinaryFormats()[key]];
        if (bytes) values[key] = @{@"bytes":[bytes base64EncodedStringWithOptions:0]};
    }
    if (!values[@"Bitmap"]) {
        NSData *tiff = [board dataForType:NSPasteboardTypeTIFF];
        if (tiff) {
            NSBitmapImageRep *rep = [NSBitmapImageRep imageRepWithData:tiff];
            NSData *png = [rep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
            if (png) values[@"Bitmap"] = @{@"bytes":[png base64EncodedStringWithOptions:0]};
        }
    }
    NSArray *files = [board readObjectsForClasses:@[NSURL.class] options:@{NSPasteboardURLReadingFileURLsOnlyKey:@YES}];
    if (files.count) values[@"FileDrop"] = [files valueForKey:@"path"];
    return values;
}
static char *Json(id data) {
    NSData *bytes = [NSJSONSerialization dataWithJSONObject:data options:0 error:nil];
    return strdup(([[NSString alloc] initWithData:bytes encoding:NSUTF8StringEncoding] ?: @"{}").UTF8String);
}
char *mf_board_read(const char *name) { @autoreleasepool { return Json(Payload(Board(name))); } }
int mf_board_write(const char *name,const char *json) {
    @autoreleasepool {
        NSPasteboard *board = Board(name);
        NSDictionary *values = [NSJSONSerialization JSONObjectWithData:[@(json) dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
        if (![values isKindOfClass:NSDictionary.class]) return 0;
        [board clearContents];
        return !values.count || [board writeObjects:Items(values)];
    }
}
static NSDragOperation Operation(int effects) {
    return ((effects & 1) ? NSDragOperationCopy : 0) | ((effects & 2) ? NSDragOperationMove : 0) | ((effects & 4) ? NSDragOperationLink : 0);
}
static int Effects(NSDragOperation ops) {
    return ((ops & NSDragOperationCopy) ? 1 : 0) | ((ops & NSDragOperationMove) ? 2 : 0) | ((ops & NSDragOperationLink) ? 4 : 0);
}
typedef int (*MFDragCallback)(void *,int,const char *,int,int,int,int);
static MFDragCallback deliver;
static char destinationKey, activeKey;
void mf_set_drag_callback(MFDragCallback callback) { deliver = callback; }
void mf_drop_enable(NSView *view,void *peer,int enabled) {
    if (!view) return;
    objc_setAssociatedObject(view,&destinationKey,enabled ? [NSValue valueWithPointer:peer] : nil,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
}
void mf_register_drop_window(NSWindow *window) {
    // AllowDrop can be set before a view has a window, and controls can be reparented.
    // The destination hit-test below selects only currently enabled drop targets.
    [window registerForDraggedTypes:@[NSPasteboardTypeString,NSPasteboardTypeFileURL,NSPasteboardTypeRTF,NSPasteboardTypeHTML,NSPasteboardTypePNG,NSPasteboardTypeTIFF,@"com.microsoft.waveform-audio",@"public.comma-separated-values-text",privateFormat]];
}
static int Send(void *target,int phase,id<NSDraggingInfo> info) {
    if (!deliver || !target) return 0;
    NSPoint loc = [info.draggingDestinationWindow convertPointToScreen:info.draggingLocation];
    int state = (int)NSEvent.pressedMouseButtons & 3;
    NSEventModifierFlags f = NSEvent.modifierFlags;
    if (f & NSEventModifierFlagShift) state |= 4;
    if (f & NSEventModifierFlagControl) state |= 8;
    if (f & NSEventModifierFlagOption) state |= 32;
    char *payload = phase == 1 || phase == 4 ? Json(Payload(info.draggingPasteboard)) : NULL;
    int effect = deliver(target,phase,payload,(int)loc.x,(int)(NSMaxY(NSScreen.screens.firstObject.frame)-loc.y),state,Effects(info.draggingSourceOperationMask));
    free(payload);
    return effect;
}
@implementation MFWindow (MFDrop)
- (NSDragOperation)draggingEntered:(id<NSDraggingInfo>)info { return [self draggingUpdated:info]; }
- (NSDragOperation)draggingUpdated:(id<NSDraggingInfo>)info {
    NSView *view = [self.contentView hitTest:[self.contentView.superview convertPoint:info.draggingLocation fromView:nil]];
    NSValue *target = nil;
    for (;view && !target;view=view.superview) target = objc_getAssociatedObject(view,&destinationKey);
    NSValue *old = objc_getAssociatedObject(self,&activeKey);
    BOOL changed = old.pointerValue != target.pointerValue;
    if (changed) {
        Send(old.pointerValue,3,info);
        objc_setAssociatedObject(self,&activeKey,target,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    }
    return Operation(Send(target.pointerValue,changed ? 1 : 2,info));
}
- (void)draggingExited:(id<NSDraggingInfo>)info {
    NSValue *target = objc_getAssociatedObject(self,&activeKey);
    objc_setAssociatedObject(self,&activeKey,nil,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    Send(target.pointerValue,3,info);
}
- (BOOL)performDragOperation:(id<NSDraggingInfo>)info {
    NSValue *target = objc_getAssociatedObject(self,&activeKey);
    objc_setAssociatedObject(self,&activeKey,nil,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    return Send(target.pointerValue,4,info) != 0;
}
@end
@interface MFDragSource : NSObject <NSDraggingSource>
@property int allowed;
@property int outcome;
@property BOOL finished;
@end
@implementation MFDragSource
- (NSDragOperation)draggingSession:(NSDraggingSession *)session sourceOperationMaskForDraggingContext:(NSDraggingContext)context {
    return Operation(self.allowed);
}
- (void)draggingSession:(NSDraggingSession *)session endedAtPoint:(NSPoint)point operation:(NSDragOperation)operation {
    self.outcome = Effects(operation);
    self.finished = YES;
}
@end
int mf_begin_drag(NSView *view,const char *json,int allowed) {
    NSEvent *event = NSApp.currentEvent;
    if (!view.window || event.window != view.window || (event.type != NSEventTypeLeftMouseDragged && event.type != NSEventTypeLeftMouseDown)) return 0;
    NSDictionary *payload = [NSJSONSerialization JSONObjectWithData:[@(json) dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
    NSMutableArray *items = [NSMutableArray array];
    NSPoint point = [view convertPoint:event.locationInWindow fromView:nil];
    for (NSPasteboardItem *item in Items(payload)) {
        NSDraggingItem *drag = [[NSDraggingItem alloc] initWithPasteboardWriter:item];
        NSImage *icon = [NSImage imageWithSystemSymbolName:@"doc.on.doc" accessibilityDescription:@"Drag"];
        [drag setDraggingFrame:NSMakeRect(point.x,point.y,24,24) contents:icon];
        [items addObject:drag];
    }
    MFDragSource *source = [[MFDragSource alloc] init];
    source.allowed = allowed;
    [view beginDraggingSessionWithItems:items event:event source:source];
    while (!source.finished) { @autoreleasepool {
        NSEvent *next = [NSApp nextEventMatchingMask:NSEventMaskAny untilDate:[NSDate dateWithTimeIntervalSinceNow:0.03] inMode:NSDefaultRunLoopMode dequeue:YES];
        if (next) [NSApp sendEvent:next];
    } }
    return source.outcome;
}
