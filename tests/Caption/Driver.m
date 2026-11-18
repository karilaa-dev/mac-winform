#import <AppKit/AppKit.h>

static NSWindow *Window(void *view) { return ((__bridge NSView *)view).window; }

static NSView *Caption(NSWindow *w) {
    return (w.styleMask & NSWindowStyleMaskTitled) ? w.titlebarAccessoryViewControllers.firstObject.view : nil;
}

static NSButton *Button(NSWindow *w, NSString *subrole) {
    for (NSView *view in Caption(w).subviews)
        if ([view isKindOfClass:NSButton.class] && [view.accessibilitySubrole isEqualToString:subrole])
            return (NSButton *)view;
    return nil;
}

int CaptionHeight(void *view) {
    NSWindow *w = Window(view);
    return (int)lround(NSHeight(w.frame) - NSHeight(w.contentView.bounds));
}

// Verify physical hit targets as well as accessibility metadata and enabled state.
int Buttons(void *view) {
    NSWindow *w = Window(view);
    NSView *frame = w.contentView.superview;
    [frame layoutSubtreeIfNeeded];
    int mask = 0, bit = 1;
    for (NSString *role in @[NSAccessibilityMinimizeButtonSubrole,NSAccessibilityZoomButtonSubrole,NSAccessibilityCloseButtonSubrole]) {
        NSButton *b = Button(w,role);
        if (b && !b.hidden && b.enabled) {
            NSPoint center = [b convertPoint:NSMakePoint(NSMidX(b.bounds),NSMidY(b.bounds)) toView:frame.superview];
            NSView *hit = [frame hitTest:center];
            if (hit == b) mask |= bit;
            else fprintf(stderr, "Caption button %s at %s hit %s (frame %s, caption %s)\n",
                role.UTF8String,NSStringFromPoint(center).UTF8String,NSStringFromClass(hit.class).UTF8String,
                NSStringFromRect(frame.frame).UTF8String,NSStringFromRect(Caption(w).frame).UTF8String);
        }
        bit <<= 1;
    }
    return mask;
}

int WidthMatches(void *view) {
    NSWindow *w = Window(view);
    return fabs(NSWidth(Caption(w).frame) - NSWidth(w.contentView.bounds)) < 1;
}

void ClickCaption(void *view, int command) {
    NSWindow *w = Window(view);
    NSArray *roles = @[NSAccessibilityMinimizeButtonSubrole,NSAccessibilityZoomButtonSubrole,NSAccessibilityCloseButtonSubrole];
    [Button(w,roles[command]) performClick:nil];
}

int HasTitle(void *view,const char *text) { return [Window(view).title isEqualToString:@(text)]; }

int HasDottedTabFocus(void *view) {
    NSView *tab = (NSView *)Window(view).firstResponder;
    if (![tab isKindOfClass:NSButton.class] || NSWidth(tab.bounds) < 18) return 0;
    NSBitmapImageRep *image = [tab bitmapImageRepForCachingDisplayInRect:tab.bounds];
    [tab cacheDisplayInRect:tab.bounds toBitmapImageRep:image];
    NSInteger scale = lround(image.pixelsWide / NSWidth(tab.bounds));
    for (NSInteger y = 0; y < image.pixelsHigh; y++) {
        BOOL previous = NO;
        int transitions = 0;
        for (NSInteger x = 4; x < 17; x++) {
            NSColor *pixel = [[image colorAtX:x*scale y:y] colorUsingColorSpace:NSColorSpace.sRGBColorSpace];
            BOOL dark = pixel.redComponent < .25;
            if (x > 4 && dark != previous) transitions++;
            previous = dark;
        }
        if (transitions == 12) return 1;
    }
    return 0;
}
