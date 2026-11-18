#import <AppKit/AppKit.h>
static NSView *View(void *ptr) { return (__bridge NSView *)ptr; }
void SendOption(void *ptr,const char *code,int key) {
    NSWindow *w=View(ptr).window;
    NSString *text=@(code);
    NSEvent *e=[NSEvent keyEventWithType:NSEventTypeKeyDown location:NSZeroPoint modifierFlags:NSEventModifierFlagOption timestamp:0
                             windowNumber:w.windowNumber context:nil characters:text charactersIgnoringModifiers:text isARepeat:NO keyCode:key];
    [w sendEvent:e];
}
void Press(void *ptr) { [(NSButton *)View(ptr) performClick:nil]; }
void DragSlider(void *ptr, int x1, int y1, int x2, int y2) {
    NSView *view=View(ptr);
    NSEventType types[] = { NSEventTypeLeftMouseDown, NSEventTypeLeftMouseDragged, NSEventTypeLeftMouseUp };
    for (int i=0; i<3; i++) {
        NSPoint p=NSMakePoint(i ? x2 : x1, i ? y2 : y1);
        if (!view.isFlipped) p.y=NSHeight(view.bounds)-p.y;
        NSEvent *event=[NSEvent mouseEventWithType:types[i] location:[view convertPoint:p toView:nil]
                                     modifierFlags:0 timestamp:0 windowNumber:view.window.windowNumber context:nil
                                       eventNumber:0 clickCount:1 pressure:1];
        [view.window sendEvent:event];
    }
}
void ArrowRight(void *ptr) {
    NSView *view=View(ptr);
    [view.window makeFirstResponder:view];
    unichar key=NSRightArrowFunctionKey;
    NSString *text=[NSString stringWithCharacters:&key length:1];
    NSEvent *event=[NSEvent keyEventWithType:NSEventTypeKeyDown location:NSZeroPoint modifierFlags:0 timestamp:0
        windowNumber:view.window.windowNumber context:nil characters:text charactersIgnoringModifiers:text isARepeat:NO keyCode:124];
    [view.window sendEvent:event];
}
int HasChoiceAccent(void *ptr) {
    NSView *v=View(ptr);
    NSBitmapImageRep *rep=[v bitmapImageRepForCachingDisplayInRect:v.bounds];
    [v cacheDisplayInRect:v.bounds toBitmapImageRep:rep];
    rep=[NSBitmapImageRep imageRepWithData:[rep representationUsingType:NSBitmapImageFileTypePNG properties:@{}]];
    NSInteger scale=MAX(1,lround(rep.pixelsWide/NSWidth(v.bounds)));
    NSInteger blue=0;
    for (NSInteger x=0;x<13*scale && x<rep.pixelsWide;x++) for (NSInteger y=0;y<rep.pixelsHigh;y++) {
        NSUInteger sample[5]={0};
        [rep getPixel:sample atX:x y:y];
        if (sample[0]<4 && labs((long)sample[1]-95)<5 && labs((long)sample[2]-184)<5) blue++;
    }
    return blue>50*scale*scale;
}
int TitleMatches(void *ptr,const char *title) {
    NSView *v=View(ptr);
    NSString *shown=[v isKindOfClass:NSButton.class] ? ((NSButton *)v).title : ((NSTextField *)v).stringValue;
    return [shown isEqualToString:@(title)];
}
int PixelReport(void *ptr,int expectAfter) {
    NSView *v=View(ptr);
    NSBitmapImageRep *rep=[v bitmapImageRepForCachingDisplayInRect:v.bounds];
    [v cacheDisplayInRect:v.bounds toBitmapImageRep:rep];
    int red=0,blue=0,green=0,ink=0; double inkX=0,redX=0;
    for (NSInteger y=0;y<rep.pixelsHigh;y++)
        for (NSInteger x=0;x<rep.pixelsWide;x++) {
            NSColor *c=[[rep colorAtX:x y:y] colorUsingColorSpace:NSColorSpace.sRGBColorSpace];
            if (c.alphaComponent<.9) continue;
            CGFloat r=c.redComponent,g=c.greenComponent,b=c.blueComponent;
            if (r>.9 && g<.25 && b<.25) { red++; redX+=x; }
            if (b>.9 && r<.25 && g<.25) blue++;
            if (g>.9 && r<.25 && b<.25) green++;
            if (r<.2 && g<.2 && b<.2) { ink++; inkX+=x; }
        }
    BOOL passed=red>=12 && green>=40 && blue>=10 && ink>=4 &&
        (expectAfter ? redX/red > inkX/ink : redX/red < inkX/ink);
    if (!passed) fprintf(stderr,"Button capture %s %ldx%ld: image %d at %.1f, fill %d, border %d, text %d at %.1f\n",
        NSStringFromClass(v.class).UTF8String,(long)rep.pixelsWide,(long)rep.pixelsHigh,red,
        red ? redX/red : 0,green,blue,ink,ink ? inkX/ink : 0);
    return passed;
}
