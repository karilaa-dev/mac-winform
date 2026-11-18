#import "WindowCaption.h"

typedef NS_ENUM(NSInteger, MFWindowAction) { MFMinimizeWindow, MFMaximizeWindow, MFCloseWindow };
static NSColor *CaptionColor(unsigned color) {
    return [NSColor colorWithSRGBRed:((color >> 16) & 255)/255.0 green:((color >> 8) & 255)/255.0 blue:(color & 255)/255.0 alpha:1];
}

@interface MFCaptionButton : NSButton
@property MFWindowAction command;
@property BOOL hovered;
@property BOOL restoredGlyph;
@end
@implementation MFCaptionButton {
    NSTrackingArea *_tracking;
}
- (BOOL)isFlipped { return YES; }
- (BOOL)acceptsFirstResponder { return NO; }
- (BOOL)acceptsFirstMouse:(NSEvent *)event { return YES; }
- (BOOL)mouseDownCanMoveWindow { return NO; }
- (void)updateTrackingAreas {
    [super updateTrackingAreas];
    if (_tracking) [self removeTrackingArea:_tracking];
    _tracking = [[NSTrackingArea alloc] initWithRect:NSZeroRect options:NSTrackingMouseEnteredAndExited | NSTrackingActiveAlways | NSTrackingInVisibleRect owner:self userInfo:nil];
    [self addTrackingArea:_tracking];
}
- (void)mouseEntered:(NSEvent *)event { self.hovered = YES; self.needsDisplay = YES; }
- (void)mouseExited:(NSEvent *)event { self.hovered = NO; self.needsDisplay = YES; }
- (void)highlight:(BOOL)flag { [super highlight:flag]; self.needsDisplay = YES; }
- (void)drawRect:(NSRect)dirty {
    BOOL hot = self.enabled && (self.hovered || self.highlighted);
    BOOL close = self.command == MFCloseWindow;
    if (hot) {
        unsigned color = close ? (self.highlighted ? 0xb62b2b : 0xc42b1c) : (self.highlighted ? 0xd6d6d6 : 0xe4e4e4);
        [CaptionColor(color) setFill]; NSRectFill(self.bounds);
    }
    NSColor *ink = close && hot ? NSColor.whiteColor : CaptionColor(!self.enabled ? 0xb3b3b3 : self.window.keyWindow ? 0x191919 : 0x686868);
    CGFloat x = floor(NSMidX(self.bounds) - 5) + .5, y = floor(NSMidY(self.bounds) - 5) + .5;
    NSBezierPath *path = [NSBezierPath bezierPath];
    if (close) {
        [path moveToPoint:NSMakePoint(x,y)]; [path lineToPoint:NSMakePoint(x+9,y+9)];
        [path moveToPoint:NSMakePoint(x+9,y)]; [path lineToPoint:NSMakePoint(x,y+9)];
    } else if (self.command == MFMinimizeWindow) {
        [path moveToPoint:NSMakePoint(x,y+5)]; [path lineToPoint:NSMakePoint(x+10,y+5)];
    } else if (self.restoredGlyph) {
        [path appendBezierPathWithRoundedRect:NSMakeRect(x,y+2,7,7) xRadius:1 yRadius:1];
        [path moveToPoint:NSMakePoint(x+2,y+1)]; [path lineToPoint:NSMakePoint(x+2,y)];
        [path lineToPoint:NSMakePoint(x+9,y)]; [path lineToPoint:NSMakePoint(x+9,y+7)]; [path lineToPoint:NSMakePoint(x+8,y+7)];
    } else {
        [path appendBezierPathWithRoundedRect:NSMakeRect(x,y,9,9) xRadius:1 yRadius:1];
    }
    [ink setStroke]; path.lineWidth = 1; [path stroke];
}
@end

@interface MFCaptionView : NSView
@property (weak) MFWindowCaption *controller;
@property (copy) NSArray<MFCaptionButton *> *buttons;
@property NSImage *formImage;
@property BOOL toolWindow;
@property BOOL showImage;
@property NSRect iconFrame;
@end

@implementation MFCaptionView
- (BOOL)isFlipped { return YES; }
- (NSSize)intrinsicContentSize { return NSMakeSize(NSViewNoIntrinsicMetric,self.toolWindow ? 24 : 32); }
- (BOOL)acceptsFirstMouse:(NSEvent *)event { return YES; }
- (BOOL)mouseDownCanMoveWindow { return NO; }
- (void)layout {
    [super layout];
    CGFloat x = NSWidth(self.bounds);
    // AppKit's legacy title bar in a stock .NET apphost can clip a taller accessory.
    NSRect area = NSIsEmptyRect(self.visibleRect) ? self.bounds : self.visibleRect;
    NSUInteger count = 0;
    for (MFCaptionButton *b in self.buttons) if (!b.hidden) count++;
    CGFloat width = MIN(self.toolWindow ? 30 : 46, count ? floor(x / count) : x);
    for (MFCaptionButton *b in self.buttons.reverseObjectEnumerator) if (!b.hidden) {
        x -= width;
        b.frame = NSMakeRect(x,NSMinY(area),width,NSHeight(area));
    }
    self.iconFrame = self.formImage && self.showImage && x >= 32 ? NSMakeRect(8,floor(NSMidY(area)-8),16,16) : NSZeroRect;
}
- (void)drawRect:(NSRect)dirty {
    [CaptionColor(self.window.keyWindow ? 0xf3f3f3 : 0xf6f6f6) setFill]; NSRectFill(dirty);
    CGFloat left = self.toolWindow ? 8 : 16;
    if (!NSIsEmptyRect(self.iconFrame)) {
        [self.formImage drawInRect:self.iconFrame fromRect:NSZeroRect operation:NSCompositingOperationSourceOver
            fraction:self.window.keyWindow ? 1 : .7 respectFlipped:YES hints:nil];
        left = NSMaxX(self.iconFrame) + 8;
    }
    CGFloat right = NSWidth(self.bounds);
    for (MFCaptionButton *b in self.buttons) if (!b.hidden) { right = NSMinX(b.frame); break; }
    if (right <= left) return;
    NSFont *font = [NSFont fontWithName:@"Segoe UI" size:12] ?: [NSFont systemFontOfSize:12];
    NSMutableParagraphStyle *style = [NSMutableParagraphStyle new];
    style.lineBreakMode = NSLineBreakByTruncatingTail;
    NSDictionary *attrs = @{NSFontAttributeName:font,NSForegroundColorAttributeName:CaptionColor(self.window.keyWindow ? 0x191919 : 0x696969),NSParagraphStyleAttributeName:style};
    CGFloat height = ceil([@"Ag" sizeWithAttributes:attrs].height);
    [self.window.title drawInRect:NSMakeRect(left,floor(NSMidY(self.visibleRect)-height/2),MAX(0,right-left-4),height) withAttributes:attrs];
}
- (void)mouseDown:(NSEvent *)event {
    if (event.clickCount == 2) {
        if (!self.buttons[MFMaximizeWindow].hidden && self.buttons[MFMaximizeWindow].enabled)
            [self.controller setMaximized:!self.controller.isMaximized];
        return;
    }
    [self.window performWindowDragWithEvent:event];
}
@end

@implementation MFWindowCaption {
    __weak NSWindow *_host;
    MFCaptionView *_caption;
    NSRect _unzoomedFrame;
    NSInteger _border;
}
- (instancetype)initWithWindow:(NSWindow *)window {
    if ((self = [super init])) {
        _host = window;
        self.layoutAttribute = NSLayoutAttributeRight;
        _caption = [[MFCaptionView alloc] initWithFrame:NSMakeRect(0,0,NSWidth(window.frame),32)];
        _caption.controller = self;
        self.view = _caption;
        NSArray *names = @[@"Minimize",@"Maximize",@"Close"];
        NSMutableArray *buttons = [NSMutableArray array];
        for (NSInteger i=0;i<3;i++) {
            MFCaptionButton *b = [[MFCaptionButton alloc] initWithFrame:NSZeroRect];
            b.command = i; b.title = @""; b.bordered = NO; b.focusRingType = NSFocusRingTypeNone;
            b.target = self; b.action = @selector(performAction:);
            [b setAccessibilityLabel:names[i]];
            [b setAccessibilitySubrole:@[NSAccessibilityMinimizeButtonSubrole,NSAccessibilityZoomButtonSubrole,NSAccessibilityCloseButtonSubrole][i]];
            b.toolTip = names[i];
            [_caption addSubview:b]; [buttons addObject:b];
        }
        _caption.buttons = buttons;
    }
    return self;
}
- (void)setFormImage:(NSImage *)image { _caption.formImage = image; [self refresh]; }
- (void)updateBorder:(int)border controlBox:(BOOL)controlBox minimize:(BOOL)minimize maximize:(BOOL)maximize {
    _border = border;
    BOOL titled = (_host.styleMask & NSWindowStyleMaskTitled) != 0;
    if (!titled) return;
    _host.titleVisibility = NSWindowTitleHidden;
    for (NSNumber *type in @[@(NSWindowCloseButton),@(NSWindowMiniaturizeButton),@(NSWindowZoomButton)])
        [_host standardWindowButton:type.integerValue].hidden = YES;
    _caption.toolWindow = border == 5 || border == 6;
    [_caption invalidateIntrinsicContentSize];
    _caption.showImage = controlBox && !_caption.toolWindow;
    BOOL showPair = controlBox && !_caption.toolWindow && (minimize || maximize);
    _caption.buttons[MFMinimizeWindow].hidden = !showPair;
    _caption.buttons[MFMinimizeWindow].enabled = minimize;
    _caption.buttons[MFMaximizeWindow].hidden = !showPair;
    _caption.buttons[MFMaximizeWindow].enabled = maximize;
    _caption.buttons[MFCloseWindow].hidden = !controlBox;
    [self refresh];
    if (![_host.titlebarAccessoryViewControllers containsObject:self]) [_host addTitlebarAccessoryViewController:self];
    [self refresh];
}
- (BOOL)isMaximized {
    return !NSEqualRects(_unzoomedFrame,NSZeroRect) || ((_host.styleMask & NSWindowStyleMaskResizable) && _host.zoomed);
}
- (void)setMaximized:(BOOL)value {
    if (value == self.isMaximized) return;
    if (_host.styleMask & NSWindowStyleMaskResizable) [_host zoom:nil];
    else if (value) { _unzoomedFrame = _host.frame; [_host setFrame:_host.screen.visibleFrame display:YES]; }
    else { NSRect restore = _unzoomedFrame; _unzoomedFrame = NSZeroRect; [_host setFrame:restore display:YES]; }
    [self refresh];
}
- (void)performAction:(MFCaptionButton *)button {
    switch (button.command) {
        case MFMinimizeWindow: [_host miniaturize:button]; break;
        case MFMaximizeWindow: [self setMaximized:!self.isMaximized]; break;
        case MFCloseWindow: [_host performClose:button]; break;
    }
}
- (void)refresh {
    if (!_host || _border == 0) return;
    NSSize size = NSMakeSize(NSWidth(_host.frame),_caption.toolWindow ? 24 : 32);
    if (!NSEqualSizes(_caption.frame.size,size)) [_caption setFrameSize:size];
    MFCaptionButton *zoom = _caption.buttons[MFMaximizeWindow];
    zoom.restoredGlyph = self.isMaximized;
    NSString *name = zoom.restoredGlyph ? @"Restore" : @"Maximize";
    [zoom setAccessibilityLabel:name]; zoom.toolTip = name;
    _caption.needsLayout = YES; _caption.needsDisplay = YES;
    for (NSButton *b in _caption.buttons) b.needsDisplay = YES;
}
@end
