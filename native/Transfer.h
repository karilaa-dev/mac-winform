#import <AppKit/AppKit.h>
void mf_drop_enable(NSView *view, void *peer, int enabled);
void mf_register_drop_window(NSWindow *window);
int mf_begin_drag(NSView *view,const char *json,int allowed);
