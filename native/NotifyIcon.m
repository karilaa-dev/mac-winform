#import <AppKit/AppKit.h>
#import "Drawing.h"

NSMenu *mf_native_menu(void *handle);
typedef void (*MFStatusEvent)(void *,int,int,int,int,int);
@class MFStatusPeer;
@interface MFStatusHitView : NSView
@property(weak) MFStatusPeer *peer;
@property NSTrackingArea *area;
@end
@interface MFStatusBalloonView : NSView
@property(weak) MFStatusPeer *peer;
@end
@interface MFStatusPeer : NSObject <NSPopoverDelegate>
@property NSStatusItem *item;
@property NSPopover *balloon;
@property NSTimer *timer;
@property MFStatusEvent callback;
- (void)send:(int)phase button:(int)button clicks:(int)clicks point:(NSPoint)p;
@end

@implementation MFStatusHitView
- (BOOL)isAccessibilityElement { return NO; } // The underlying NSStatusBarButton provides the accessible action.
- (void)updateTrackingAreas {
    [super updateTrackingAreas];
    if (self.area) [self removeTrackingArea:self.area];
    self.area=[[NSTrackingArea alloc] initWithRect:NSZeroRect options:NSTrackingInVisibleRect|NSTrackingMouseMoved|NSTrackingActiveAlways owner:self userInfo:nil];
    [self addTrackingArea:self.area];
}
- (void)mouse:(NSEvent *)e phase:(int)phase button:(int)button {
    NSPoint p=[self convertPoint:e.locationInWindow fromView:nil];
    [self.peer send:phase button:button clicks:phase==3 ? 0 : (int)e.clickCount point:p];
}
- (void)mouseDown:(NSEvent *)e { [self mouse:e phase:1 button:1]; }
- (void)mouseUp:(NSEvent *)e { [self mouse:e phase:2 button:1]; }
- (void)rightMouseDown:(NSEvent *)e { [self mouse:e phase:1 button:2]; }
- (void)rightMouseUp:(NSEvent *)e { [self mouse:e phase:2 button:2]; }
- (void)otherMouseDown:(NSEvent *)e { [self mouse:e phase:1 button:3]; }
- (void)otherMouseUp:(NSEvent *)e { [self mouse:e phase:2 button:3]; }
- (void)mouseMoved:(NSEvent *)e { [self mouse:e phase:3 button:0]; }
- (void)mouseDragged:(NSEvent *)e { [self mouse:e phase:3 button:1]; }
@end
@implementation MFStatusBalloonView
- (NSView *)hitTest:(NSPoint)p { return [super hitTest:p] ? self : nil; }
- (void)mouseDown:(NSEvent *)event {
    MFStatusPeer *peer=self.peer;
    NSPopover *clicked=peer.balloon;
    [peer send:6 button:0 clicks:0 point:NSZeroPoint];
    [clicked close];
}
@end
@implementation MFStatusPeer
- (void)send:(int)phase button:(int)button clicks:(int)clicks point:(NSPoint)p {
    if (self.callback) self.callback((__bridge void *)self,phase,button,clicks,(int)p.x,(int)p.y);
}
- (void)activate:(id)sender {
    [self send:1 button:1 clicks:1 point:NSZeroPoint];
    [self send:2 button:1 clicks:1 point:NSZeroPoint];
}
- (void)popoverDidShow:(NSNotification *)note {
    if (note.object==self.balloon) [self send:4 button:0 clicks:0 point:NSZeroPoint];
}
- (void)popoverDidClose:(NSNotification *)note {
    if (note.object!=self.balloon) return;
    [self.timer invalidate];
    self.timer=nil;
    self.balloon=nil;
    [self send:5 button:0 clicks:0 point:NSZeroPoint];
}
@end

void *mf_status_create(MFStatusEvent callback) {
    MFStatusPeer *peer=[MFStatusPeer new];
    peer.callback=callback;
    peer.item=[NSStatusBar.systemStatusBar statusItemWithLength:NSSquareStatusItemLength];
    peer.item.visible=YES;
    NSStatusBarButton *button=peer.item.button;
    if (!button) return NULL;
    button.target=peer;
    button.action=@selector(activate:);
    MFStatusHitView *hit=[[MFStatusHitView alloc] initWithFrame:button.bounds];
    hit.peer=peer;
    hit.autoresizingMask=NSViewWidthSizable|NSViewHeightSizable;
    [button addSubview:hit];
    return (__bridge_retained void *)peer;
}
void mf_status_update(void *handle,void *image,const char *text) {
    MFStatusPeer *peer=(__bridge MFStatusPeer *)handle;
    NSImage *icon=[mf_native_image(image) copy];
    icon.size=NSMakeSize(18,18);
    peer.item.button.image=icon;
    peer.item.button.toolTip=@(text);
    [peer.item.button setAccessibilityLabel:@(text)];
}
void mf_status_menu(void *handle,void *menuHandle) {
    MFStatusPeer *peer=(__bridge MFStatusPeer *)handle;
    NSStatusBarButton *button=peer.item.button;
    [mf_native_menu(menuHandle) popUpMenuPositioningItem:nil atLocation:NSMakePoint(0,NSHeight(button.bounds)) inView:button];
}
void mf_status_destroy(void *handle) {
    MFStatusPeer *peer=(__bridge_transfer MFStatusPeer *)handle;
    peer.callback=NULL;
    [peer.timer invalidate];
    peer.balloon.delegate=nil;
    [peer.balloon close];
    peer.item.button.target=nil;
    peer.item.visible=NO;
    [NSStatusBar.systemStatusBar removeStatusItem:peer.item];
}
void mf_status_balloon(void *handle,int timeout,const char *title,const char *text,int icon) {
    MFStatusPeer *peer=(__bridge MFStatusPeer *)handle;
    [peer.balloon close];
    if (!peer.callback) return; // A Closed handler may dispose or hide the status item.
    NSPopover *balloon=[NSPopover new];
    balloon.behavior=NSPopoverBehaviorTransient;
    balloon.animates=NO;
    balloon.delegate=peer;
    NSViewController *content=[NSViewController new];
    MFStatusBalloonView *view=[MFStatusBalloonView new];
    view.peer=peer;
    const CGFloat inset=12, width=270;
    CGFloat left=icon ? 40 : inset;
    NSTextField *heading=[NSTextField labelWithString:@(title)];
    heading.font=[NSFont boldSystemFontOfSize:NSFont.systemFontSize];
    NSTextField *body=[NSTextField wrappingLabelWithString:@(text)];
    CGFloat textWidth=width-left-inset;
    body.preferredMaxLayoutWidth=textWidth;
    CGFloat bodyHeight=MAX(18,MIN(240,ceil([body.cell cellSizeForBounds:NSMakeRect(0,0,textWidth,10000)].height)));
    CGFloat height=bodyHeight+2*inset+(heading.stringValue.length ? 21 : 0);
    view.frame=NSMakeRect(0,0,width,height);
    body.frame=NSMakeRect(left,inset,textWidth,bodyHeight);
    [view addSubview:body];
    if (heading.stringValue.length) {
        heading.frame=NSMakeRect(left,inset+bodyHeight+3,textWidth,18);
        [view addSubview:heading];
    }
    if (icon) {
        NSArray *symbols=@[@"",@"info.circle",@"exclamationmark.triangle",@"xmark.octagon"];
        NSImageView *image=[NSImageView imageViewWithImage:[NSImage imageWithSystemSymbolName:symbols[icon] accessibilityDescription:nil]];
        image.frame=NSMakeRect(inset,height-inset-18,18,18);
        [view addSubview:image];
    }
    content.view=view;
    balloon.contentViewController=content;
    balloon.contentSize=view.frame.size;
    peer.balloon=balloon;
    [balloon showRelativeToRect:peer.item.button.bounds ofView:peer.item.button preferredEdge:NSRectEdgeMinY];
    if (peer.balloon!=balloon || !peer.callback) return;
    __weak NSPopover *weakBalloon=balloon;
    peer.timer=[NSTimer timerWithTimeInterval:(timeout>0 ? timeout/1000.0 : 10.0) repeats:NO block:^(NSTimer *t){ [weakBalloon close]; }];
    [NSRunLoop.mainRunLoop addTimer:peer.timer forMode:NSRunLoopCommonModes];
}
