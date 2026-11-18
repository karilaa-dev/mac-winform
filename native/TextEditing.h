#import <AppKit/AppKit.h>
BOOL MFShouldReplaceText(void *handle,NSRange range,NSString *replacement);
void MFFieldFinished(NSTextField *field,NSDictionary *info);
void MFFieldBegan(NSTextField *field,NSDictionary *info);
