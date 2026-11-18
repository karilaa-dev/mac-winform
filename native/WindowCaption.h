#import <AppKit/AppKit.h>

// Occupy AppKit's native non-client title bar so the form's content view keeps its WinForms bounds.
@interface MFWindowCaption : NSTitlebarAccessoryViewController
- (instancetype)initWithWindow:(NSWindow *)window;
- (void)updateBorder:(int)border controlBox:(BOOL)controlBox minimize:(BOOL)minimize maximize:(BOOL)maximize;
- (void)setFormImage:(NSImage *)image;
- (void)refresh;
- (void)setMaximized:(BOOL)value;
- (BOOL)isMaximized;
@end
