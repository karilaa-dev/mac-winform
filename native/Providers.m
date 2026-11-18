#import <AppKit/AppKit.h>
#import "MacForms.h"

static NSString *MFProviderText(const char *text) { return text ? [NSString stringWithUTF8String:text] : @""; }
static NSDictionary *MFProviderOptions(const char *json) {
    return [NSJSONSerialization JSONObjectWithData:[MFProviderText(json) dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
}
static NSString *MFProviderJson(NSDictionary *value) {
    NSData *data=[NSJSONSerialization dataWithJSONObject:value options:0 error:nil];
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
}
static NSColor *MFProviderColor(uint32_t value) {
    return [NSColor colorWithSRGBRed:((value>>16)&255)/255.0 green:((value>>8)&255)/255.0 blue:(value&255)/255.0 alpha:((value>>24)&255)/255.0];
}
static int32_t MFProviderARGB(NSColor *color) {
    NSColor *rgb=[color colorUsingColorSpace:NSColorSpace.sRGBColorSpace];
    return ((uint32_t)lround(rgb.alphaComponent*255)<<24) | ((uint32_t)lround(rgb.redComponent*255)<<16) |
           ((uint32_t)lround(rgb.greenComponent*255)<<8) | (uint32_t)lround(rgb.blueComponent*255);
}
static uint32_t MFFlipRB(uint32_t rgb) { return ((rgb>>16)&255) | (rgb&0xff00) | ((rgb&255)<<16); }

typedef void (*MFDialogAction)(int,const char *);
@interface MFProviderPanel : NSObject <NSWindowDelegate>
@property NSPanel *panel;
@property NSDictionary *options;
@property MFDialogAction callback;
@property NSFont *font;
@property NSColorWell *ink;
@property NSButton *underline, *strike;
@property NSArray<NSButton *> *swatches;
@property NSMutableArray<NSNumber *> *palette;
@property NSInteger slot;
@property BOOL useFont;
- (NSDictionary *)selection;
@end

@implementation MFProviderPanel
- (void)changeFont:(NSFontManager *)manager {
    self.font=[manager convertFont:self.font];
}
- (BOOL)fontManager:(NSFontManager *)manager willIncludeFont:(NSString *)fontName {
    if (![self.options[@"FixedPitchOnly"] boolValue]) return YES;
    return [NSFont fontWithName:fontName size:12].isFixedPitch;
}
- (NSDictionary *)selection {
    if (!self.useFont)
        return @{@"Color":@(MFProviderARGB(NSColorPanel.sharedColorPanel.color)), @"Palette":self.palette};
    NSFont *font=[NSFontPanel.sharedFontPanel panelConvertFont:self.font] ?: self.font;
    CGFloat size=MAX(font.pointSize,[self.options[@"MinSize"] doubleValue]);
    CGFloat maximum=[self.options[@"MaxSize"] doubleValue];
    if (maximum>0) size=MIN(size,maximum);
    if ([self.options[@"FixedPitchOnly"] boolValue] && !font.isFixedPitch) font=[NSFont monospacedSystemFontOfSize:size weight:NSFontWeightRegular];
    NSFontTraitMask traits=[NSFontManager.sharedFontManager traitsOfFont:font];
    int style=(traits&NSBoldFontMask ? 1:0) | (traits&NSItalicFontMask ? 2:0);
    style|=self.underline ? (self.underline.state ? 4:0) | (self.strike.state ? 8:0) : [self.options[@"Style"] intValue]&12;
    return @{@"Family":font.familyName, @"Size":@(size), @"Style":@(style),
             @"Color":self.ink ? @(MFProviderARGB(self.ink.color)) : self.options[@"Color"]};
}
- (void)finish:(NSButton *)sender {
    NSModalResponse answer=sender.tag==1 ? NSModalResponseOK : NSModalResponseCancel;
    if (self.panel.sheetParent) [self.panel.sheetParent endSheet:self.panel returnCode:answer];
    else [NSApp stopModalWithCode:answer];
}
- (void)help:(id)sender { self.callback(2,NULL); }
- (void)apply:(id)sender { self.callback(1,MFProviderJson([self selection]).UTF8String); }
- (void)swatch:(NSButton *)sender {
    self.slot=sender.tag;
    for (NSButton *button in self.swatches) button.state=button==sender;
    NSColorPanel.sharedColorPanel.color=MFProviderColor(0xff000000u|MFFlipRB([self.palette[self.slot] unsignedIntValue]));
}
- (void)storeSwatch:(id)sender {
    NSColor *color=NSColorPanel.sharedColorPanel.color;
    self.palette[self.slot]=@(MFFlipRB(MFProviderARGB(color)));
    self.swatches[self.slot].bezelColor=color;
}
@end

static NSButton *MFProviderButton(NSString *title,id target,SEL action) {
    return [NSButton buttonWithTitle:title target:target action:action];
}
static NSStackView *MFProviderRow(NSArray *views) {
    NSStackView *row=[NSStackView stackViewWithViews:views]; row.spacing=5; return row;
}
char *mf_provider_dialog(void *ownerView,int kind,const char *json,MFDialogAction callback) {
    @autoreleasepool {
        NSDictionary *opts=MFProviderOptions(json);
        MFProviderPanel *controller=[MFProviderPanel new];
        controller.options=opts; controller.callback=callback; controller.useFont=kind==1;
        NSFontManager *manager=NSFontManager.sharedFontManager;
        id oldTarget=manager.target; SEL oldAction=manager.action;
        NSPanel *panel;
        NSView *previousAccessory;
        NSMutableArray *rows=[NSMutableArray array];
        id oldDelegate=nil;
        if (kind==0) {
            NSColorPanel *colors=NSColorPanel.sharedColorPanel; panel=colors;
            previousAccessory=colors.accessoryView; colors.showsAlpha=NO;
            colors.color=MFProviderColor([opts[@"Color"] unsignedIntValue]);
            colors.mode=[opts[@"FullOpen"] boolValue] ? NSColorPanelModeRGB : NSColorPanelModeColorList;
            controller.palette=[opts[@"Palette"] mutableCopy];
            NSMutableArray *buttons=[NSMutableArray array];
            for (NSUInteger i=0;i<controller.palette.count;i++) {
                NSButton *b=MFProviderButton(@" ",controller,@selector(swatch:));
                b.tag=i; b.bezelStyle=NSBezelStyleRounded; b.bezelColor=MFProviderColor(0xff000000u|MFFlipRB([controller.palette[i] unsignedIntValue]));
                b.toolTip=[NSString stringWithFormat:@"Custom color %lu",i+1]; [b setButtonType:NSButtonTypePushOnPushOff];
                [buttons addObject:b];
            }
            controller.swatches=buttons;
            if ([opts[@"AllowFullOpen"] boolValue]) {
                [rows addObject:MFProviderRow([buttons subarrayWithRange:NSMakeRange(0,8)])];
                NSMutableArray *last=[[buttons subarrayWithRange:NSMakeRange(8,8)] mutableCopy];
                [last addObject:MFProviderButton(@"Add color",controller,@selector(storeSwatch:))];
                [rows addObject:MFProviderRow(last)];
            }
        } else {
            NSFontPanel *fonts=NSFontPanel.sharedFontPanel; panel=fonts; previousAccessory=fonts.accessoryView;
            oldDelegate=fonts.delegate;
            if ([opts[@"FixedPitchOnly"] boolValue]) {
                fonts.delegate=controller; [fonts reloadDefaultFontFamilies];
            }
            int style=[opts[@"Style"] intValue];
            NSFontTraitMask traits=(style&1 ? NSBoldFontMask : 0) | (style&2 ? NSItalicFontMask : 0);
            CGFloat size=[opts[@"Size"] doubleValue];
            controller.font=[manager fontWithFamily:opts[@"Family"] traits:traits weight:5 size:size] ?: [NSFont systemFontOfSize:size];
            manager.target=controller; manager.action=@selector(changeFont:);
            [manager setSelectedFont:controller.font isMultiple:NO]; [fonts setPanelFont:controller.font isMultiple:NO];
            NSMutableArray *effects=[NSMutableArray array];
            if ([opts[@"ShowColor"] boolValue]) {
                controller.ink=[[NSColorWell alloc] initWithFrame:NSMakeRect(0,0,42,23)];
                controller.ink.color=MFProviderColor([opts[@"Color"] unsignedIntValue]); [effects addObject:controller.ink];
            }
            if ([opts[@"ShowEffects"] boolValue]) {
                controller.underline=[NSButton checkboxWithTitle:@"Underline" target:nil action:nil];
                controller.strike=[NSButton checkboxWithTitle:@"Strikethrough" target:nil action:nil];
                controller.underline.state=(style&4)!=0; controller.strike.state=(style&8)!=0;
                [effects addObjectsFromArray:@[controller.underline,controller.strike]];
            }
            if (effects.count) [rows addObject:MFProviderRow(effects)];
        }
        controller.panel=panel;
        NSMutableArray *actions=[NSMutableArray array];
        if ([opts[@"ShowHelp"] boolValue]) [actions addObject:MFProviderButton(@"Help",controller,@selector(help:))];
        if ([opts[@"ShowApply"] boolValue]) [actions addObject:MFProviderButton(@"Apply",controller,@selector(apply:))];
        NSButton *cancel=MFProviderButton(@"Cancel",controller,@selector(finish:)); cancel.keyEquivalent=@"\033";
        NSButton *accept=MFProviderButton(@"OK",controller,@selector(finish:)); accept.tag=1; accept.keyEquivalent=@"\r";
        [actions addObjectsFromArray:@[cancel,accept]]; [rows addObject:MFProviderRow(actions)];
        NSStackView *accessory=[NSStackView stackViewWithViews:rows]; accessory.orientation=NSUserInterfaceLayoutOrientationVertical;
        accessory.edgeInsets=NSEdgeInsetsMake(4,6,4,6); [accessory setFrameSize:accessory.fittingSize];
        if (kind==0) ((NSColorPanel *)panel).accessoryView=accessory; else ((NSFontPanel *)panel).accessoryView=accessory;
        NSWindow *parent=((__bridge NSView *)ownerView).window;
        __block NSModalResponse response=NSModalResponseContinue;
        id closing=[NSNotificationCenter.defaultCenter addObserverForName:NSWindowWillCloseNotification object:panel queue:nil usingBlock:^(NSNotification *_) {
            if (panel.sheetParent) [panel.sheetParent endSheet:panel returnCode:NSModalResponseCancel];
            else if (NSApp.modalWindow==panel) [NSApp stopModalWithCode:NSModalResponseCancel];
        }];
        if (parent && parent!=panel) {
            [parent beginSheet:panel completionHandler:^(NSModalResponse code){ response=code; }];
            while (response==NSModalResponseContinue) mf_pump();
        } else response=[NSApp runModalForWindow:panel];
        NSDictionary *selection=response==NSModalResponseOK ? [controller selection] : nil;
        [NSNotificationCenter.defaultCenter removeObserver:closing];
        [panel orderOut:nil];
        if (kind==0) ((NSColorPanel *)panel).accessoryView=previousAccessory;
        else {
            ((NSFontPanel *)panel).accessoryView=previousAccessory; manager.target=oldTarget; manager.action=oldAction;
            if ([opts[@"FixedPitchOnly"] boolValue]) { panel.delegate=oldDelegate; [(NSFontPanel *)panel reloadDefaultFontFamilies]; }
        }
        return selection ? strdup(MFProviderJson(selection).UTF8String) : NULL;
    }
}

typedef int (*MFBeforeTip)(int *,int *);
static NSTimeInterval MFLastTip;
@interface MFHoverTip : NSObject
@property (weak) NSView *anchor;
@property NSTrackingArea *tracking;
@property NSDictionary *options;
@property MFBeforeTip before;
@property NSPanel *window;
@property NSTimer *timer;
- (void)hide;
- (void)show:(NSString *)text point:(NSPoint)point duration:(NSInteger)duration;
@end
@implementation MFHoverTip
- (void)after:(NSInteger)milliseconds block:(void (^)(void))action {
    [self.timer invalidate];
    self.timer=[NSTimer timerWithTimeInterval:MAX(0.001,milliseconds/1000.0) repeats:NO block:^(NSTimer *_) { action(); }];
    [NSRunLoop.mainRunLoop addTimer:self.timer forMode:NSRunLoopCommonModes];
}
- (void)hide {
    [self.timer invalidate]; self.timer=nil;
    if(self.window) {
        MFLastTip=NSDate.timeIntervalSinceReferenceDate;
        [self.window.parentWindow removeChildWindow:self.window];
        [self.window orderOut:nil]; self.window=nil;
    }
}
- (void)mouseEntered:(NSEvent *)event {
    [self hide];
    if (![self.options[@"active"] boolValue] || ![self.options[@"text"] length]) return;
    NSInteger delay=[self.options[@"initial"] integerValue];
    if(NSDate.timeIntervalSinceReferenceDate-MFLastTip < delay/1000.0) delay=[self.options[@"reshow"] integerValue];
    __weak MFHoverTip *weak=self;
    [self after:delay block:^{
        MFHoverTip *tip=weak;
        [tip show:tip.options[@"text"] point:NSMakePoint(0,-1) duration:[tip.options[@"duration"] integerValue]];
    }];
}
- (void)mouseExited:(NSEvent *)event { [self hide]; }
- (void)show:(NSString *)text point:(NSPoint)point duration:(NSInteger)duration {
    [self hide]; NSDictionary *opts=self.options;
    NSView *view=self.anchor; NSWindow *parent=view.window;
    if (!parent || view.isHiddenOrHasHiddenAncestor || ![opts[@"active"] boolValue] || !text.length) return;
    if (![opts[@"showAlways"] boolValue] && !parent.isKeyWindow && !parent.isMainWindow) return;
    if ([opts[@"strip"] boolValue]) text=[text stringByReplacingOccurrencesOfString:@"&" withString:@""];
    NSString *prefix=opts[@"title"]; NSInteger icon=[opts[@"icon"] integerValue];
    if (icon>0 && icon<=3) prefix=[NSString stringWithFormat:@"%@ %@",@[@"",@"ⓘ",@"⚠",@"⛔"][icon],prefix];
    NSString *caption=prefix.length ? [NSString stringWithFormat:@"%@\n%@",prefix,text] : text;
    NSTextField *label=[NSTextField wrappingLabelWithString:caption]; label.font=[NSFont systemFontOfSize:12];
    label.textColor=MFProviderColor([opts[@"fore"] unsignedIntValue]);
    label.preferredMaxLayoutWidth=350;
    int width=ceil(MIN(350,label.intrinsicContentSize.width))+12, height=ceil(label.intrinsicContentSize.height)+8;
    if (!self.before(&width,&height) || !self.anchor || !view.window || width<=0 || height<=0) return;
    NSRect local=NSMakeRect(point.x,point.y<0 ? (view.isFlipped ? NSHeight(view.bounds) : 0) :
        (view.isFlipped ? point.y : NSHeight(view.bounds)-point.y),0,0);
    NSRect where=[parent convertRectToScreen:[view convertRect:local toView:nil]];
    NSRect screen=parent.screen.visibleFrame;
    CGFloat x=MAX(NSMinX(screen),MIN(NSMinX(where),NSMaxX(screen)-width));
    CGFloat y=NSMinY(where)-height-3; if (y<NSMinY(screen)) y=NSMaxY(where)+3;
    NSPanel *tip=[[NSPanel alloc] initWithContentRect:NSMakeRect(x,y,width,height) styleMask:NSWindowStyleMaskBorderless|NSWindowStyleMaskNonactivatingPanel backing:NSBackingStoreBuffered defer:NO];
    tip.releasedWhenClosed=NO; tip.opaque=NO; tip.backgroundColor=NSColor.clearColor; tip.hasShadow=YES;
    tip.ignoresMouseEvents=YES; tip.identifier=@"MacForms.ToolTip"; tip.animationBehavior=[opts[@"animation"] boolValue] ? NSWindowAnimationBehaviorUtilityWindow:NSWindowAnimationBehaviorNone;
    NSView *content=tip.contentView; content.wantsLayer=YES;
    content.layer.backgroundColor=MFProviderColor([opts[@"back"] unsignedIntValue]).CGColor;
    content.layer.cornerRadius=[opts[@"balloon"] boolValue] ? 8:2;
    label.frame=NSInsetRect(content.bounds,6,4); [content addSubview:label];
    self.window=tip; [parent addChildWindow:tip ordered:NSWindowAbove];
    if ([opts[@"fading"] boolValue] && [opts[@"animation"] boolValue]) {
        tip.alphaValue=0; [tip orderFront:nil];
        [NSAnimationContext runAnimationGroup:^(NSAnimationContext *ctx) { ctx.duration=0.09; tip.animator.alphaValue=1; } completionHandler:nil];
    } else [tip orderFront:nil];
    __weak MFHoverTip *weak=self;
    if(duration>0) [self after:duration block:^{ [weak hide]; }];
}
@end

void *mf_tip_attach(void *raw,MFBeforeTip before) {
    NSView *view=(__bridge NSView *)raw;
    MFHoverTip *tip=[MFHoverTip new]; tip.anchor=view; tip.before=before;
    tip.tracking=[[NSTrackingArea alloc] initWithRect:NSZeroRect options:NSTrackingMouseEnteredAndExited|NSTrackingInVisibleRect|NSTrackingActiveAlways owner:tip userInfo:nil];
    [view addTrackingArea:tip.tracking]; return (__bridge_retained void *)tip;
}
void mf_tip_config(void *raw,const char *json) {
    MFHoverTip *tip=(__bridge MFHoverTip *)raw; tip.options=MFProviderOptions(json);
    if(![tip.options[@"active"] boolValue]) [tip hide];
}
void mf_tip_show(void *raw,const char *text,int x,int y,int duration) { [(__bridge MFHoverTip *)raw show:MFProviderText(text) point:NSMakePoint(x,y) duration:duration]; }
void mf_tip_hide(void *raw) { [(__bridge MFHoverTip *)raw hide]; }
void mf_tip_release(void *raw) {
    MFHoverTip *tip=(__bridge_transfer MFHoverTip *)raw;
    [tip hide]; [tip.anchor removeTrackingArea:tip.tracking]; tip.anchor=nil;
}

#import "Drawing.h"

@interface MFErrorIndicator : NSObject
@property (weak) NSView *anchor;
@property NSImageView *glyph;
@property id frameObserver, parentObserver;
@property NSTimer *timer;
@property NSInteger alignment,padding;
@property NSString *message;
@property BOOL observing;
- (void)layout;
@end
@implementation MFErrorIndicator
- (void)layout {
    NSView *target=self.anchor,*parent=target.superview;
    if (!parent || self.message.length==0) { [self.glyph removeFromSuperview]; return; }
    if (self.glyph.superview!=parent) [parent addSubview:self.glyph positioned:NSWindowAbove relativeTo:target];
    NSRect bounds=target.frame;
    CGFloat x=self.alignment%2==1 ? NSMaxX(bounds)+self.padding : NSMinX(bounds)-16-self.padding;
    CGFloat top=parent.isFlipped ? NSMinY(bounds):NSMaxY(bounds)-16;
    CGFloat bottom=parent.isFlipped ? NSMaxY(bounds)-16:NSMinY(bounds);
    CGFloat y=self.alignment/2==0 ? top : self.alignment/2==1 ? NSMidY(bounds)-8:bottom;
    self.glyph.frame=NSMakeRect(x,floor(y),16,16);
    self.glyph.hidden=target.hidden;
}
- (void)observeValueForKeyPath:(NSString *)keyPath ofObject:(id)object change:(NSDictionary *)change context:(void *)context {
    [self layout];
}
@end
void *mf_error_attach(void *raw) {
    MFErrorIndicator *indicator=[MFErrorIndicator new];
    NSView *target=(__bridge NSView *)raw; indicator.anchor=target;
    indicator.glyph=[[NSImageView alloc] initWithFrame:NSMakeRect(0,0,16,16)];
    indicator.glyph.imageScaling=NSImageScaleProportionallyUpOrDown;
    indicator.glyph.identifier=@"MacForms.ErrorProvider";
    [target addObserver:indicator forKeyPath:@"hidden" options:0 context:NULL]; indicator.observing=YES;
    target.postsFrameChangedNotifications=YES;
    __weak MFErrorIndicator *weak=indicator;
    indicator.frameObserver=[NSNotificationCenter.defaultCenter addObserverForName:NSViewFrameDidChangeNotification object:target queue:nil usingBlock:^(NSNotification *_) { [weak layout]; }];
    indicator.parentObserver=[NSNotificationCenter.defaultCenter addObserverForName:@"MFViewReparented" object:target queue:nil usingBlock:^(NSNotification *_) { [weak layout]; }];
    return (__bridge_retained void *)indicator;
}
void mf_error_config(void *raw,const char *text,int align,int padding,int blink,int rate,void *image) {
    MFErrorIndicator *item=(__bridge MFErrorIndicator *)raw;
    NSString *message=MFProviderText(text); BOOL changed=![message isEqualToString:item.message];
    item.message=message; item.alignment=align; item.padding=padding;
    item.glyph.image=mf_native_image(image); item.glyph.toolTip=message;
    item.glyph.accessibilityLabel=message;
    [item layout]; [item.timer invalidate]; item.timer=nil; item.glyph.alphaValue=1;
    if (!message.length || rate<=0 || blink==2 || (blink==0 && !changed)) return;
    __weak MFErrorIndicator *weak=item; __block int count=0;
    item.timer=[NSTimer timerWithTimeInterval:rate/1000.0 repeats:YES block:^(NSTimer *timer) {
        MFErrorIndicator *strong=weak;
        if (!strong || (blink==0 && ++count>=8)) { [timer invalidate]; strong.glyph.alphaValue=1; return; }
        strong.glyph.alphaValue=strong.glyph.alphaValue>0 ? 0:1;
    }];
    [NSRunLoop.mainRunLoop addTimer:item.timer forMode:NSRunLoopCommonModes];
}
void mf_error_release(void *raw) {
    MFErrorIndicator *item=(__bridge_transfer MFErrorIndicator *)raw;
    [item.timer invalidate];
    if(item.observing) [item.anchor removeObserver:item forKeyPath:@"hidden"];
    [NSNotificationCenter.defaultCenter removeObserver:item.parentObserver];
    [NSNotificationCenter.defaultCenter removeObserver:item.frameObserver]; [item.glyph removeFromSuperview];
}
