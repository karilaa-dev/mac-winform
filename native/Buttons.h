#import <AppKit/AppKit.h>
@interface MFStyledButton : NSButton
@property BOOL managedDrawing;
@property BOOL mouseInside;
@end

@interface MFChoiceButton : MFStyledButton
@property BOOL radio;
@end

// Render the common 13-unit Windows choice mark into a flipped or unflipped host.
void MFDrawChoiceGlyph(BOOL flipped, NSRect rect, BOOL radio, NSControlStateValue state,
                       BOOL enabled, BOOL pressed, BOOL hot);
