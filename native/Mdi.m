#import "Mdi.h"
@implementation MFMdiFrame {
    NSView *_content;
    NSButton *_close;
    NSPoint _dragPoint, _dragOrigin;
}
- (BOOL)isFlipped { return YES; }
- (instancetype)initWithContent:(NSView *)content {
    if ((self = [super initWithFrame:NSZeroRect])) {
        _content = content;
        [self addSubview:content];
        _close = [NSButton buttonWithTitle:@"×" target:self action:@selector(closeChild:)];
        _close.bordered = NO;
        [_close setAccessibilityLabel:@"Close"];
        [self addSubview:_close];
        self.wantsLayer = YES;
        self.layer.masksToBounds = YES;
    }
    return self;
}
- (void)closeChild:(id)sender { if (_notify) _notify(5); }
- (void)setTitle:(NSString *)title { _title = [title copy]; self.needsDisplay = YES; }
- (void)layout {
    [super layout];
    _close.frame = NSMakeRect(MAX(0,NSWidth(self.bounds)-24),0,24,22);
    _content.frame = NSMakeRect(1,23,MAX(0,NSWidth(self.bounds)-2),MAX(0,NSHeight(self.bounds)-24));
}
- (void)drawRect:(NSRect)dirty {
    [NSColor.windowBackgroundColor setFill]; NSRectFill(dirty);
    [NSColor.separatorColor setStroke]; [NSBezierPath strokeRect:NSInsetRect(self.bounds,.5,.5)];
    [(_title ?: @"") drawInRect:NSMakeRect(7,3,MAX(0,NSWidth(self.bounds)-34),19)
         withAttributes:@{NSFontAttributeName:[NSFont systemFontOfSize:12],NSForegroundColorAttributeName:NSColor.labelColor}];
}
- (void)activateChild {
    if (self.superview.subviews.lastObject != self) [self.superview addSubview:self positioned:NSWindowAbove relativeTo:nil];
    if (_notify) _notify(45);
}
- (void)mouseDown:(NSEvent *)event {
    [self activateChild];
    _dragPoint = [self.superview convertPoint:event.locationInWindow fromView:nil];
    _dragOrigin = self.frame.origin;
}
- (void)mouseDragged:(NSEvent *)event {
    NSPoint p = [self.superview convertPoint:event.locationInWindow fromView:nil];
    self.frameOrigin = NSMakePoint(MAX(0,_dragOrigin.x+p.x-_dragPoint.x),MAX(0,_dragOrigin.y+p.y-_dragPoint.y));
    if (_notify) _notify(46);
}
@end
