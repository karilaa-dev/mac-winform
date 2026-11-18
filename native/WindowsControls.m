#import "WindowsControls.h"

@implementation MFProgressIndicator
- (void)drawRect:(NSRect)dirty {
    if (self.indeterminate) { [super drawRect:dirty]; return; }
    NSRect bounds = self.bounds;
    [[NSColor colorWithWhite:0.74 alpha:1] setFill]; NSRectFill(bounds);
    NSRect content = NSInsetRect(bounds,1,1);
    [[NSColor colorWithWhite:0.90 alpha:1] setFill]; NSRectFill(content);
    double range = self.maxValue - self.minValue;
    double progress = range > 0 ? MAX(0,MIN(1,(self.doubleValue-self.minValue)/range)) : 0;
    content.size.width = round(content.size.width * progress);
    [[NSColor colorWithSRGBRed:0.05 green:0.50 blue:0.05 alpha:1] setFill]; NSRectFill(content);
}
- (NSSize)intrinsicContentSize { return NSMakeSize(NSViewNoIntrinsicMetric,NSViewNoIntrinsicMetric); }
@end


// The WinForms track occupies the leading 27 pixels of a normal 45-pixel
// control. Draw and track against the same endpoints as the Windows common control.
@implementation MFTrackBar {
    BOOL draggingThumb;
    CGFloat dragOffset;
}
- (BOOL)isFlipped { return YES; }
- (CGFloat)axisLength { return self.vertical ? NSHeight(self.bounds) : NSWidth(self.bounds); }
- (CGFloat)axisStart { return MIN(13, floor(self.axisLength/2)); }
- (CGFloat)axisTravel { return MAX(0,self.axisLength-2*self.axisStart-1); }
- (CGFloat)positionForValue:(double)value {
    double range = self.maxValue-self.minValue;
    return self.axisStart + (range > 0 ? round((value-self.minValue)*self.axisTravel/range) : 0);
}
- (void)drawRect:(NSRect)dirty {
    [NSGraphicsContext saveGraphicsState];
    if (self.vertical) {
        NSAffineTransform *t = [NSAffineTransform transform];
        [t translateXBy:0 yBy:NSHeight(self.bounds)];
        [t rotateByDegrees:-90];
        [t concat];
    }

    BOOL leadingTicks = self.tickStyle == 1 || self.tickStyle == 3;
    BOOL trailingTicks = self.tickStyle == 2 || self.tickStyle == 3;
    CGFloat top = leadingTicks ? 8 : 2;
    CGFloat trackY = top+6;
    NSRect track = NSMakeRect(8,trackY,MAX(0,self.axisLength-16),4);
    [[NSColor colorWithSRGBRed:214/255.0 green:214/255.0 blue:214/255.0 alpha:1] setFill];
    NSRectFill(track);
    if (NSWidth(track)>2) {
        [[NSColor colorWithSRGBRed:231/255.0 green:234/255.0 blue:234/255.0 alpha:1] setFill];
        NSRectFill(NSInsetRect(track,1,1));
    }

    if (leadingTicks || trailingTicks) {
        [[NSColor colorWithSRGBRed:196/255.0 green:196/255.0 blue:196/255.0 alpha:1] setFill];
        long long range = (long long)(self.maxValue-self.minValue);
        long long frequency = MAX(1,self.tickFrequency);
        // Dense value ranges can only display as many distinct ticks as there are pixels.
        long long stride = frequency * MAX(1LL,(long long)ceil(range/(double)frequency/MAX(1,self.axisTravel)));
        for (long long offset=0;;offset=MIN(range,offset+stride)) {
            CGFloat pos = [self positionForValue:self.minValue+offset];
            BOOL end = (offset == 0 || offset == range);
            if (leadingTicks) NSRectFill(NSMakeRect(pos,end ? 1 : 2,1,end ? 4 : 3));
            if (trailingTicks) NSRectFill(NSMakeRect(pos,top+20,1,end ? 4 : 3));
            if (offset >= range) break;
        }
    }

    CGFloat x = [self positionForValue:self.doubleValue];
    NSBezierPath *thumb;
    if (leadingTicks == trailingTicks) {
        thumb = [NSBezierPath bezierPathWithRoundedRect:NSMakeRect(x-5,top,11,19) xRadius:1 yRadius:1];
    } else {
        CGFloat first = trailingTicks ? top : top+19;
        CGFloat direction = trailingTicks ? 1 : -1;
        thumb = [NSBezierPath bezierPath];
        [thumb moveToPoint:NSMakePoint(x-5,first)];
        [thumb lineToPoint:NSMakePoint(x+6,first)];
        [thumb lineToPoint:NSMakePoint(x+6,first+14*direction)];
        [thumb lineToPoint:NSMakePoint(x+.5,first+19.5*direction)];
        [thumb lineToPoint:NSMakePoint(x-5,first+14*direction)];
        [thumb closePath];
    }
    NSColor *accent = [NSColor colorWithSRGBRed:0 green:120/255.0 blue:215/255.0 alpha:1];
    [(!self.enabled ? NSColor.disabledControlTextColor : draggingThumb ? [NSColor colorWithWhite:.38 alpha:1] : accent) setFill];
    [thumb fill];
    [NSGraphicsContext restoreGraphicsState];

    if (self.window.firstResponder == self && self.window.isKeyWindow) {
        NSBezierPath *focus = [NSBezierPath bezierPathWithRect:NSInsetRect(self.bounds,.5,.5)];
        CGFloat dash[] = {1,1}; [focus setLineDash:dash count:2 phase:0];
        [NSColor.darkGrayColor setStroke]; [focus stroke];
    }
}

- (CGFloat)axisPoint:(NSEvent *)event {
    NSPoint point = [self convertPoint:event.locationInWindow fromView:nil];
    return self.vertical ? NSHeight(self.bounds)-point.y : point.x;
}
- (void)applyPosition:(double)value {
    value = MIN(self.maxValue,MAX(self.minValue,value));
    if (value != self.doubleValue) {
        self.doubleValue = value;
        [self sendAction:self.action to:self.target];
    }
    self.needsDisplay = YES;
}
- (void)keyDown:(NSEvent *)event {
    if (!self.enabled) return;
    if (event.charactersIgnoringModifiers.length == 1 &&
        !(event.modifierFlags & (NSEventModifierFlagCommand | NSEventModifierFlagControl | NSEventModifierFlagOption))) {
        switch ([event.charactersIgnoringModifiers characterAtIndex:0]) {
            case NSRightArrowFunctionKey:
            case NSUpArrowFunctionKey: [self applyPosition:self.doubleValue+1]; return;
            case NSLeftArrowFunctionKey:
            case NSDownArrowFunctionKey: [self applyPosition:self.doubleValue-1]; return;
            case NSHomeFunctionKey: [self applyPosition:self.minValue]; return;
            case NSEndFunctionKey: [self applyPosition:self.maxValue]; return;
        }
    }
    [super keyDown:event];
}
- (void)trackPoint:(CGFloat)axis {
    double fraction = self.axisTravel > 0 ? (axis-dragOffset-self.axisStart)/self.axisTravel : 0;
    double value = round(self.minValue+MIN(1,MAX(0,fraction))*(self.maxValue-self.minValue));
    [self applyPosition:value];
}
- (void)mouseDown:(NSEvent *)event {
    if (!self.enabled) return;
    [self.window makeFirstResponder:self];
    CGFloat axis = [self axisPoint:event];
    CGFloat offset = axis-[self positionForValue:self.doubleValue];
    dragOffset = fabs(offset)<=5.5 ? offset : 0;
    draggingThumb = YES;
    [self trackPoint:axis];
}
- (void)mouseDragged:(NSEvent *)event {
    if (draggingThumb && self.enabled) [self trackPoint:[self axisPoint:event]];
}
- (void)mouseUp:(NSEvent *)event {
    if (draggingThumb && self.enabled) [self trackPoint:[self axisPoint:event]];
    draggingThumb = NO;
    self.needsDisplay = YES;
}
@end
