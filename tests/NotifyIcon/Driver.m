#import <AppKit/AppKit.h>

// All input is addressed directly to the native objects of this one test component.
static __weak NSStatusItem *observed;
static NSView *HitView(NSStatusItem *item) {
    for (NSView *v in item.button.subviews)
        if ([v isKindOfClass:NSClassFromString(@"MFStatusHitView")]) return v;
    return nil;
}
int observe_item(void *handle,const char *text) {
    observed=[(__bridge id)handle valueForKey:@"item"];
    return (observed ? 1 : 0) | (observed.visible ? 2 : 0) | (observed.button.image ? 4 : 0) |
        ([observed.button.toolTip isEqualToString:@(text)] ? 8 : 0) | (HitView(observed) ? 16 : 0);
}
int item_removed(void) { return !observed || !observed.visible; }
int item_mouse(int right,int clicks) {
    NSView *view=HitView(observed);
    if (!view) return 0;
    for (int phase=0;phase<2;phase++) {
        NSEventType type=right ? (phase ? NSEventTypeRightMouseUp : NSEventTypeRightMouseDown) :
            (phase ? NSEventTypeLeftMouseUp : NSEventTypeLeftMouseDown);
        NSEvent *e=[NSEvent mouseEventWithType:type location:[view convertPoint:NSMakePoint(8,8) toView:nil] modifierFlags:0
            timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:view.window.windowNumber context:nil eventNumber:1 clickCount:clicks pressure:1];
        if (right) { if (phase) [view rightMouseUp:e]; else [view rightMouseDown:e]; }
        else { if (phase) [view mouseUp:e]; else [view mouseDown:e]; }
    }
    return 1;
}
void select_own_menu(void *nativeMenu) {
    NSMenu *menu=(__bridge NSMenu *)nativeMenu;
    NSTimer *timer=[NSTimer timerWithTimeInterval:0.1 repeats:NO block:^(NSTimer *t) {
        [menu performActionForItemAtIndex:0];
        [menu cancelTracking];
    }];
    [NSRunLoop.mainRunLoop addTimer:timer forMode:NSEventTrackingRunLoopMode];
    [NSRunLoop.mainRunLoop addTimer:timer forMode:NSRunLoopCommonModes];
}
int balloon_state(void *handle,int click) {
    NSPopover *popover=[(__bridge id)handle valueForKey:@"balloon"];
    if (!popover.shown) return 0;
    NSView *view=popover.contentViewController.view;
    if (click) {
        NSEvent *e=[NSEvent mouseEventWithType:NSEventTypeLeftMouseDown location:[view convertPoint:NSMakePoint(8,8) toView:nil]
            modifierFlags:0 timestamp:NSProcessInfo.processInfo.systemUptime windowNumber:view.window.windowNumber context:nil
            eventNumber:1 clickCount:1 pressure:1];
        [view mouseDown:e];
    }
    return 1;
}
