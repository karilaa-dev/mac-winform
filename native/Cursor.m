#import <AppKit/AppKit.h>
#import <objc/runtime.h>

void *mf_cursor_standard(int kind) {
    NSCursor *list[] = {NSCursor.arrowCursor,NSCursor.IBeamCursor,NSCursor.crosshairCursor,NSCursor.pointingHandCursor,
        NSCursor.operationNotAllowedCursor,NSCursor.resizeLeftRightCursor,NSCursor.resizeUpDownCursor,NSCursor.openHandCursor};
    return (__bridge void *)list[kind];
}
void *mf_cursor_load(const unsigned char *data,int length) {
    NSImage *image=[[NSImage alloc] initWithData:[NSData dataWithBytes:data length:length]];
    if (!image) return NULL;
    NSPoint hotspot=NSZeroPoint;
    // Windows CUR stores the hotspot in the first directory entry.
    if (length>=22 && data[0]==0 && data[1]==0 && data[2]==2 && data[3]==0)
        hotspot=NSMakePoint(data[10] | (data[11]<<8),data[12] | (data[13]<<8));
    return (__bridge_retained void *)[[NSCursor alloc] initWithImage:image hotSpot:hotspot];
}
void mf_cursor_release(void *cursor) { if(cursor) CFBridgingRelease(cursor); }
void mf_cursor_info(void *cursor,int *w,int *h,int *x,int *y) {
    NSCursor *item=(__bridge NSCursor *)cursor; NSSize size=item.image.size; NSPoint hot=item.hotSpot;
    *w=(int)size.width; *h=(int)size.height; *x=(int)hot.x; *y=(int)hot.y;
}
void mf_cursor_set_current(void *cursor) { [(__bridge NSCursor *)cursor set]; }
void mf_cursor_hide(int hide) { if(hide) [NSCursor hide]; else [NSCursor unhide]; }
void mf_cursor_location(int write,int *x,int *y) {
    if(write) CGWarpMouseCursorPosition(CGPointMake(*x,*y));
    else { NSPoint p=NSEvent.mouseLocation; *x=(int)floor(p.x); *y=(int)floor(NSMaxY(NSScreen.screens.firstObject.frame)-p.y); }
}
@interface MFCursorRect : NSView
@property NSCursor *shape;
@end
@implementation MFCursorRect
- (NSView *)hitTest:(NSPoint)point { return nil; }
- (void)resetCursorRects { [self addCursorRect:self.visibleRect cursor:self.shape]; }
- (BOOL)isAccessibilityElement { return NO; }
@end
void mf_view_cursor(NSView *view,void *cursor) {
    if (!view) return;
    static char key;
    MFCursorRect *rect=objc_getAssociatedObject(view,&key);
    if (!cursor) {
        [rect removeFromSuperview]; objc_setAssociatedObject(view,&key,nil,OBJC_ASSOCIATION_RETAIN_NONATOMIC); return;
    }
    if (!rect) {
        rect=[[MFCursorRect alloc] initWithFrame:view.bounds];
        rect.autoresizingMask=NSViewWidthSizable | NSViewHeightSizable;
        [view addSubview:rect positioned:NSWindowBelow relativeTo:nil];
        objc_setAssociatedObject(view,&key,rect,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    }
    rect.shape=(__bridge NSCursor *)cursor;
    [view.window invalidateCursorRectsForView:rect];
}
