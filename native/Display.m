#import <AppKit/AppKit.h>
static NSArray *MFScreenRect(NSRect r,CGFloat top) {
    return @[@((int)round(NSMinX(r))),@((int)round(top-NSMaxY(r))),@((int)round(NSWidth(r))),@((int)round(NSHeight(r)))];
}
char *mf_screens(void) {
    NSArray<NSScreen *> *screens = NSScreen.screens;
    CGFloat top = NSMaxY(screens.firstObject.frame);
    NSMutableArray *result = [NSMutableArray array];
    for (NSScreen *screen in screens)
        [result addObject:@{@"name":screen.localizedName,@"bounds":MFScreenRect(screen.frame,top),@"work":MFScreenRect(screen.visibleFrame,top),
                            @"depth":@(NSBitsPerPixelFromDepth(screen.depth))}];
    NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:nil];
    return strdup([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding].UTF8String);
}

void *mf_view_bitmap(NSView *view,int *size) {
    *size = 0;
    if (!view || NSIsEmptyRect(view.bounds)) return NULL;
    [view layoutSubtreeIfNeeded];
    NSBitmapImageRep *rep = [view bitmapImageRepForCachingDisplayInRect:view.bounds];
    if (!rep) return NULL;
    [view cacheDisplayInRect:view.bounds toBitmapImageRep:rep];
    NSData *data = [rep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
    if (!data.length || data.length > INT_MAX) return NULL;
    void *copy = malloc(data.length);
    if (copy) { memcpy(copy,data.bytes,data.length); *size = (int)data.length; }
    return copy;
}
