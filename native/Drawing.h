#import <AppKit/AppKit.h>

// Return an AppKit representation of the managed image. Callers that keep it retain it.
NSImage *mf_native_image(void *imageHandle);
