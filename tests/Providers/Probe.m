#import <AppKit/AppKit.h>
#import <dlfcn.h>

static NSView *viewFor(void *handle) {
    static NSView *(*getView)(void *);
    if (!getView) {
        NSString *path=[NSProcessInfo.processInfo.arguments.firstObject.stringByDeletingLastPathComponent stringByAppendingPathComponent:@"libMacForms.dylib"];
        getView=dlsym(dlopen(path.fileSystemRepresentation,RTLD_LAZY),"mf_widget_view");
    }
    return getView(handle);
}
static NSButton *button(NSView *root,NSString *title) {
    if ([root isKindOfClass:NSButton.class] && [((NSButton *)root).title isEqualToString:title]) return (NSButton *)root;
    for (NSView *child in root.subviews) { NSButton *found=button(child,title); if (found) return found; }
    return nil;
}
static int panelChecks;
void PrepareDialog(void *handle,int kind,int confirm) {
    NSWindow *owner=handle ? viewFor(handle).window:nil;
    panelChecks=0;
    NSTimer *timer=[NSTimer timerWithTimeInterval:0.1 repeats:YES block:^(NSTimer *tick) {
        if(kind==2) {
            NSSavePanel *sheet=(NSSavePanel *)owner.attachedSheet;
            if(![sheet isKindOfClass:NSOpenPanel.class]) return;
            [tick invalidate]; panelChecks=1; [sheet cancel:nil]; return;
        }
        NSPanel *panel=kind==0 ? NSColorPanel.sharedColorPanel:(NSPanel *)NSFontPanel.sharedFontPanel;
        if (owner ? panel.sheetParent!=owner : NSApp.modalWindow!=panel) return;
        NSView *bar=kind==0 ? ((NSColorPanel *)panel).accessoryView:((NSFontPanel *)panel).accessoryView;
        NSButton *done=button(bar,confirm ? @"OK":@"Cancel");
        if (!done) return;
        [tick invalidate]; panelChecks=1;
        if (kind==0) {
            ((NSColorPanel *)panel).color=[NSColor colorWithSRGBRed:0x12/255.0 green:0x34/255.0 blue:0x56/255.0 alpha:1];
            [button(bar,@"Add color") performClick:nil];
        } else {
            NSFont *font=[NSFont fontWithName:@"Menlo-Bold" size:19];
            [(NSFontPanel *)panel setPanelFont:font isMultiple:NO];
            [button(bar,@"Underline") setState:NSControlStateValueOn];
            if (confirm) [button(bar,@"Apply") performClick:nil];
        }
        if (confirm) [button(bar,@"Help") performClick:nil];
        [done performClick:nil];
    }];
    [NSRunLoop.mainRunLoop addTimer:timer forMode:NSRunLoopCommonModes];
    [NSRunLoop.mainRunLoop addTimer:timer forMode:NSModalPanelRunLoopMode];
}
int PanelChecked(void) { return panelChecks; }
static NSPanel *visibleTip(NSWindow *owner) {
    for (NSPanel *panel in owner.childWindows)
        if([panel.identifier isEqualToString:@"MacForms.ToolTip"] && panel.isVisible) return panel;
    return nil;
}
int ProbeHint(void *raw,int stage) {
    NSView *v=viewFor(raw);
    NSPanel *tip=visibleTip(v.window);
    if (stage==2) return tip==nil;
    if (stage==3) return tip && NSWidth(tip.frame)==160 && NSHeight(tip.frame)==45;
    for (NSTrackingArea *area in v.trackingAreas) {
        if (![NSStringFromClass([area.owner class]) isEqualToString:@"MFHoverTip"]) continue;
        NSEvent *e=[NSEvent enterExitEventWithType:stage==0 ? NSEventTypeMouseEntered:NSEventTypeMouseExited location:NSZeroPoint modifierFlags:0 timestamp:0 windowNumber:v.window.windowNumber context:nil eventNumber:1 trackingNumber:0 userData:nil];
        if(stage==0) [area.owner mouseEntered:e]; else [area.owner mouseExited:e];
        return 1;
    }
    return 0;
}
int ProbeError(void *raw,const char *message,int dx,int dy) {
    NSView *v=viewFor(raw); NSString *expected=[NSString stringWithUTF8String:message];
    for (NSImageView *glyph in v.superview.subviews) {
        if(![glyph.identifier isEqualToString:@"MacForms.ErrorProvider"]) continue;
        return expected.length && [expected isEqualToString:glyph.toolTip] && [expected isEqualToString:glyph.accessibilityLabel] &&
            glyph.image && NSMinX(glyph.frame)==NSMinX(v.frame)+dx && NSMinY(glyph.frame)==NSMinY(v.frame)+dy && glyph.hidden==v.hidden;
    }
    return !expected.length;
}
