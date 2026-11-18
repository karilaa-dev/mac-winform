#import "Browser.h"
#import <dlfcn.h>
#import <objc/runtime.h>
static char delegateKey;
@interface MFBrowserDelegate : NSObject <WKNavigationDelegate>
@property void *peer;
@property void (*notify)(void *,int);
@property BOOL (*decide)(void *,const char *);
@end
@implementation MFBrowserDelegate
- (void)webView:(WKWebView *)view decidePolicyForNavigationAction:(WKNavigationAction *)action decisionHandler:(void (^)(WKNavigationActionPolicy))handler {
    BOOL allow = !self.decide || !action.targetFrame.isMainFrame || self.decide(self.peer,action.request.URL.absoluteString.UTF8String);
    handler(allow ? WKNavigationActionPolicyAllow : WKNavigationActionPolicyCancel);
}
- (void)webView:(WKWebView *)view didCommitNavigation:(WKNavigation *)navigation {
    if (self.notify) self.notify(self.peer,41);
}
- (void)webView:(WKWebView *)view didFinishNavigation:(WKNavigation *)navigation {
    if (self.notify) self.notify(self.peer,42);
}
@end
WKWebView *MFMakeBrowser(void *peer,void (*notify)(void *,int)) {
    // Most WinForms applications never create a browser. Load this system framework on first use.
    static dispatch_once_t once;
    dispatch_once(&once,^{ dlopen("/System/Library/Frameworks/WebKit.framework/WebKit",RTLD_LAZY | RTLD_LOCAL); });
    Class browserClass = NSClassFromString(@"WKWebView");
    WKWebViewConfiguration *configuration = [NSClassFromString(@"WKWebViewConfiguration") new];
    if (!browserClass || !configuration) return nil;
    WKWebView *view = [[browserClass alloc] initWithFrame:NSZeroRect configuration:configuration];
    MFBrowserDelegate *delegate = [MFBrowserDelegate new];
    delegate.peer = peer;
    delegate.notify = notify;
    // WKWebView keeps only a weak reference to its navigation delegate.
    objc_setAssociatedObject(view,&delegateKey,delegate,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    view.navigationDelegate = delegate;
    return view;
}
void mf_browser_hook(void *ptr,BOOL (*handler)(void *,const char *)) {
    MFBrowserDelegate *delegate = objc_getAssociatedObject((__bridge WKWebView *)ptr,&delegateKey);
    delegate.decide = handler;
}
void mf_browser_load(void *ptr, const char *content,int isHtml) {
    WKWebView *v = (__bridge WKWebView *)ptr;
    if (isHtml) [v loadHTMLString:@(content) baseURL:nil];
    else {
        NSURL *url = [NSURL URLWithString:@(content)];
        if (url.isFileURL) [v loadFileURL:url allowingReadAccessToURL:url.URLByDeletingLastPathComponent];
        else if (url) [v loadRequest:[NSURLRequest requestWithURL:url]];
    }
}
int mf_browser_action(void *ptr,int action) {
    WKWebView *v = (__bridge WKWebView *)ptr;
    switch (action) {
        case 0: return v.canGoBack;
        case 1: return v.canGoForward;
        case 2: return [v goBack] != nil;
        case 3: return [v goForward] != nil;
        case 4: [v stopLoading]; break;
        case 5: [v reload]; break;
        case 6: return v.loading;
    }
    return 0;
}
char *mf_browser_string(void *ptr,int field) {
    WKWebView *v = (__bridge WKWebView *)ptr;
    if (field != 2) return strdup(((field ? v.title : v.URL.absoluteString) ?: @"").UTF8String);
    // WebKit serves DOM queries in its web process. Keep the main run loop processing the reply.
    __block NSString *html;
    [v evaluateJavaScript:@"document.documentElement.outerHTML" completionHandler:^(id result,NSError *error) {
        html = [result isKindOfClass:NSString.class] ? result : @"";
    }];
    NSDate *deadline = [NSDate dateWithTimeIntervalSinceNow:2];
    while (!html && deadline.timeIntervalSinceNow > 0)
        [NSRunLoop.currentRunLoop runUntilDate:[NSDate dateWithTimeIntervalSinceNow:0.01]];
    return strdup((html ?: @"").UTF8String);
}
