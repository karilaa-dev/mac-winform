#import <AppKit/AppKit.h>
#import <dlfcn.h>
int ClickTab(void *handle,int index,int selected) {
    NSString *path=[NSProcessInfo.processInfo.arguments.firstObject.stringByDeletingLastPathComponent stringByAppendingPathComponent:@"libMacForms.dylib"];
    NSView *(*get)(void *)=dlsym(dlopen(path.fileSystemRepresentation,RTLD_LAZY),"mf_widget_view");
    NSView *view=get(handle); NSTabView *pages=nil; NSButton *button=nil;
    for (NSView *child in view.subviews) {
        if([child isKindOfClass:NSButton.class] && child.tag==index) button=(NSButton *)child;
        if([child isKindOfClass:NSTabView.class]) pages=(NSTabView *)child;
    }
    if(!button || !pages) return 0;
    [button performClick:nil];
    return [pages indexOfTabViewItem:pages.selectedTabViewItem]==selected && button.tag==index;
}
