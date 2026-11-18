#import "Buttons.h"
@protocol MFButtonEvents
- (void)sendEvent:(int)event;
@end
@implementation MFStyledButton {
    NSTrackingArea *hoverArea;
}
- (void)updateTrackingAreas {
    [super updateTrackingAreas];
    if (hoverArea) [self removeTrackingArea:hoverArea];
    hoverArea=nil;
    if (!self.managedDrawing && ![self isKindOfClass:MFChoiceButton.class]) return;
    hoverArea=[[NSTrackingArea alloc] initWithRect:NSZeroRect options:NSTrackingMouseEnteredAndExited|NSTrackingActiveInKeyWindow|NSTrackingInVisibleRect owner:self userInfo:nil];
    [self addTrackingArea:hoverArea];
}
- (void)mouseEntered:(NSEvent *)event { self.mouseInside=YES; self.needsDisplay=YES; [super mouseEntered:event]; }
- (void)mouseExited:(NSEvent *)event { self.mouseInside=NO; self.needsDisplay=YES; [super mouseExited:event]; }
- (void)drawRect:(NSRect)rect {
    if (!self.managedDrawing) { [super drawRect:rect]; return; }
    [(id<MFButtonEvents>)self.target sendEvent:72];
}
@end
void mf_button_drawing(void *view,int managed,int threeState,const char *caption) {
    MFStyledButton *b=(__bridge MFStyledButton *)view;
    if (![b isKindOfClass:MFStyledButton.class]) return;
    if (b.managedDrawing != (BOOL)managed) { b.managedDrawing=managed; [b updateTrackingAreas]; }
    b.allowsMixedState=threeState;
    b.title=@(caption);
    b.needsDisplay=YES;
}
int mf_button_flags(void *view) {
    MFStyledButton *b=(__bridge MFStyledButton *)view;
    return (b.highlighted?1:0)|(b.mouseInside?2:0)|(b.window.firstResponder==b?4:0)|(b.window.defaultButtonCell==b.cell?8:0);
}
void mf_label_title(void *view,const char *text) {
    NSTextField *label=(__bridge NSTextField *)view;
    if ([label isKindOfClass:NSTextField.class]) label.stringValue=@(text);
}

static NSColor *ChoiceColor(unsigned rgb) {
    return [NSColor colorWithSRGBRed:((rgb >> 16) & 255) / 255.0 green:((rgb >> 8) & 255) / 255.0
                                blue:(rgb & 255) / 255.0 alpha:1];
}

static unsigned ChoiceEdge(BOOL enabled,BOOL marked,BOOL pressed,BOOL hot) {
    if (!enabled) return 0xbfbfbf;
    if (marked) return pressed ? 0x004578 : hot ? 0x006cbe : 0x005fb8;
    return pressed ? 0x474747 : hot ? 0x404040 : 0x626262;
}
static unsigned ChoiceFace(BOOL enabled,BOOL pressed) { return !enabled ? 0xf4f4f4 : pressed ? 0xe7e7e7 : 0xf3f3f3; }
enum { ChoiceFill, ChoiceBorder, ChoiceMark };
unsigned mf_choice_color(int radio,int visualState,int part) {
    int interaction = visualState > 0 ? (visualState-1)%4+1 : 1;
    BOOL enabled=interaction!=4, pressed=interaction==3;
    unsigned edge=ChoiceEdge(enabled,visualState>=5,pressed,interaction==2);
    unsigned color;
    if (part == ChoiceMark) color = radio ? ChoiceFace(enabled,pressed) : 0xffffff;
    else color = part == ChoiceBorder || (visualState >= 5 && !radio) ? edge : ChoiceFace(enabled,pressed);
    return 0xff000000 | color;
}
void MFDrawChoiceGlyph(BOOL flipped, NSRect rect, BOOL radio, NSControlStateValue state,
                       BOOL enabled, BOOL pressed, BOOL hot) {
    BOOL marked = state != NSControlStateValueOff;
    NSColor *edge = ChoiceColor(ChoiceEdge(enabled,marked,pressed,hot));
    NSColor *fill = marked && !radio ? edge : ChoiceColor(ChoiceFace(enabled,pressed));
    [NSGraphicsContext saveGraphicsState];
    NSAffineTransform *transform = [NSAffineTransform transform];
    [transform translateXBy:NSMinX(rect) yBy:(flipped ? NSMinY(rect) : NSMaxY(rect))];
    [transform scaleXBy:NSWidth(rect)/13.0 yBy:(flipped ? 1 : -1)*NSHeight(rect)/13.0];
    [transform concat];
    NSRect box = NSMakeRect(0,0,13,13);
    NSBezierPath *outline = radio ? [NSBezierPath bezierPathWithOvalInRect:box] :
                                    [NSBezierPath bezierPathWithRoundedRect:box xRadius:3 yRadius:3];
    [edge setFill]; [outline fill];
    if (radio || !marked) {
        CGFloat thickness = radio && marked ? 4 : 1;
        NSRect inside = NSInsetRect(box,thickness,thickness);
        NSBezierPath *interior = radio ? [NSBezierPath bezierPathWithOvalInRect:inside] :
                                         [NSBezierPath bezierPathWithRoundedRect:inside xRadius:2 yRadius:2];
        [fill setFill]; [interior fill];
    } else {
        [NSColor.whiteColor setStroke];
        NSBezierPath *mark = [NSBezierPath bezierPath];
        if (state == NSControlStateValueMixed) {
            [mark moveToPoint:NSMakePoint(3.5,6.5)]; [mark lineToPoint:NSMakePoint(9.5,6.5)];
            mark.lineWidth = 1.5;
        } else {
            [mark moveToPoint:NSMakePoint(3.4,6.8)]; [mark lineToPoint:NSMakePoint(5.6,9)];
            [mark lineToPoint:NSMakePoint(9.6,4.6)];
            mark.lineWidth = .75;
        }
        mark.lineCapStyle = NSLineCapStyleRound;
        mark.lineJoinStyle = NSLineJoinStyleRound;
        [mark stroke];
    }
    [NSGraphicsContext restoreGraphicsState];
}

void mf_draw_choice(int x,int y,int width,int height,int radio,int visualState) {
    int interaction = visualState > 0 ? (visualState-1)%4+1 : 1;
    NSControlStateValue state = visualState >= 9 ? NSControlStateValueMixed :
                                visualState >= 5 ? NSControlStateValueOn : NSControlStateValueOff;
    MFDrawChoiceGlyph(YES,NSMakeRect(x,y,width,height),radio,state,interaction!=4,interaction==3,interaction==2);
}

@implementation MFChoiceButton
- (void)drawRect:(NSRect)dirty {
    if (self.managedDrawing) { [super drawRect:dirty]; return; }
    NSRect area = self.bounds;
    CGFloat top = MAX(0,floor((NSHeight(area)-13)/2)-1);
    CGFloat glyphY = self.isFlipped ? top : NSHeight(area)-top-13;
    MFDrawChoiceGlyph(self.isFlipped,NSMakeRect(0,glyphY,13,13),self.radio,self.state,self.enabled,self.highlighted,self.mouseInside);
    if (!self.title.length) return;
    NSDictionary *attributes = @{NSFontAttributeName:self.font ?: [NSFont systemFontOfSize:12],
                                 NSForegroundColorAttributeName:self.enabled ? NSColor.blackColor : NSColor.disabledControlTextColor};
    NSSize measured = [self.title sizeWithAttributes:attributes];
    CGFloat height = ceil(measured.height);
    NSRect title = NSMakeRect(19,floor((NSHeight(area)-height)/2),MAX(0,NSWidth(area)-19),height);
    [self.title drawInRect:title withAttributes:attributes];
    if (self.window.firstResponder == self && self.window.isKeyWindow) {
        title.size.width = MIN(NSWidth(title),ceil(measured.width));
        NSBezierPath *focus = [NSBezierPath bezierPathWithRect:NSInsetRect(title,-.5,-.5)];
        CGFloat dash[] = {1,1};
        [focus setLineDash:dash count:2 phase:0]; [NSColor.darkGrayColor setStroke]; [focus stroke];
    }
}
@end
