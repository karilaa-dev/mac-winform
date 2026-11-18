#import "ScrollBar.h"

// Event numbers 100..108 mirror WinForms ScrollEventType.
@interface MFStandaloneScroller : NSScroller
@property(copy) void (^deliver)(int);
@property BOOL horizontal;
@property BOOL trackingKnob;
@end
@implementation MFStandaloneScroller
- (BOOL)acceptsFirstResponder { return self.enabled; }
- (void)scrolled:(id)sender {
    int kind;
    switch (self.hitPart) {
        case NSScrollerDecrementPage: kind = 102; break;
        case NSScrollerIncrementPage: kind = 103; break;
        default: kind = 105; self.trackingKnob = YES; break;
    }
    self.deliver(kind);
}
- (void)mouseDown:(NSEvent *)event {
    self.trackingKnob = NO;
    [super mouseDown:event];
    if (self.trackingKnob) self.deliver(104);
    self.deliver(108);
}
- (void)scrollWheel:(NSEvent *)event {
    CGFloat delta = self.horizontal && event.scrollingDeltaX != 0 ? event.scrollingDeltaX : event.scrollingDeltaY;
    if (delta != 0 && self.enabled) {
        self.deliver(delta > 0 ? 100 : 101);
        self.deliver(108);
    }
}
@end
NSScroller *MFCreateScrollBar(BOOL horizontal,void (^send)(int)) {
    NSRect frame = horizontal ? NSMakeRect(0,0,80,17) : NSMakeRect(0,0,17,80);
    MFStandaloneScroller *bar = [[MFStandaloneScroller alloc] initWithFrame:frame];
    bar.horizontal = horizontal;
    bar.scrollerStyle = NSScrollerStyleLegacy;
    bar.deliver = send;
    bar.target = bar;
    bar.action = @selector(scrolled:);
    return bar;
}
