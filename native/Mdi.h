#import <Cocoa/Cocoa.h>
@interface MFMdiFrame : NSView
@property (nonatomic, copy) NSString *title;
@property (nonatomic, copy) void (^notify)(int);
- (instancetype)initWithContent:(NSView *)content;
- (void)activateChild;
@end
