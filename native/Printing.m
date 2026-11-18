#import <AppKit/AppKit.h>

static const CGFloat WfPoint = 72.0 / 100.0; // WinForms paper sizes use hundredths of an inch.

@interface MFPrintJob : NSObject
@property NSMutableArray<NSData *> *pages;
@property NSMutableData *currentPage;
@property CGContextRef context;
@end

@implementation MFPrintJob
- (instancetype)init {
    if ((self = [super init])) _pages = [NSMutableArray array];
    return self;
}
- (void)dealloc { if (_context) CGContextRelease(_context); }
@end

void *mf_print_job(void) { return (__bridge_retained void *)[MFPrintJob new]; }
void mf_print_release(void *job) { CFBridgingRelease(job); }

int mf_print_begin_page(void *handle, int width, int height, int left, int top, int antialias) {
    MFPrintJob *job = (__bridge MFPrintJob *)handle;
    if (job.context || width <= 0 || height <= 0) return 0;
    job.currentPage = [NSMutableData data];
    CGDataConsumerRef consumer = CGDataConsumerCreateWithCFData((__bridge CFMutableDataRef)job.currentPage);
    CGRect box = CGRectMake(0,0,width * WfPoint,height * WfPoint);
    CGContextRef context = CGPDFContextCreate(consumer,&box,NULL);
    CGDataConsumerRelease(consumer);
    if (!context) return 0;
    job.context = context;
    CGPDFContextBeginPage(context,NULL);
    CGContextTranslateCTM(context,0,box.size.height);
    CGContextScaleCTM(context,WfPoint,-WfPoint);
    CGContextTranslateCTM(context,left,top);
    CGContextSetShouldAntialias(context,antialias != 0);
    [NSGraphicsContext saveGraphicsState];
    NSGraphicsContext.currentContext = [NSGraphicsContext graphicsContextWithCGContext:context flipped:YES];
    return 1;
}
void mf_print_end_page(void *handle) {
    MFPrintJob *job = (__bridge MFPrintJob *)handle;
    if (!job.context) return;
    [NSGraphicsContext restoreGraphicsState];
    CGPDFContextEndPage(job.context);
    CGPDFContextClose(job.context);
    CGContextRelease(job.context);
    job.context = NULL;
    [job.pages addObject:job.currentPage];
    job.currentPage = nil;
}
const void *mf_print_page_bytes(void *handle, int index, int *length) {
    MFPrintJob *job = (__bridge MFPrintJob *)handle;
    NSData *page = job.pages[index];
    *length = (int)page.length;
    return page.bytes;
}

static CGPDFDocumentRef MFOpenPDF(NSData *data) {
    CGDataProviderRef source = CGDataProviderCreateWithCFData((__bridge CFDataRef)data);
    CGPDFDocumentRef doc = CGPDFDocumentCreateWithProvider(source);
    CGDataProviderRelease(source);
    return doc;
}

int mf_print_save(void *handle, const char *path) {
    MFPrintJob *job = (__bridge MFPrintJob *)handle;
    NSMutableData *data = [NSMutableData data];
    CGDataConsumerRef output = CGDataConsumerCreateWithCFData((__bridge CFMutableDataRef)data);
    CGContextRef context = CGPDFContextCreate(output,NULL,NULL);
    CGDataConsumerRelease(output);
    if (!context) return 0;
    for (NSData *bytes in job.pages) {
        CGPDFDocumentRef pdf = MFOpenPDF(bytes);
        CGPDFPageRef page = CGPDFDocumentGetPage(pdf,1);
        CGRect bounds = CGPDFPageGetBoxRect(page,kCGPDFMediaBox);
        NSDictionary *info = @{(__bridge NSString *)kCGPDFContextMediaBox:[NSData dataWithBytes:&bounds length:sizeof(bounds)]};
        CGPDFContextBeginPage(context,(__bridge CFDictionaryRef)info);
        CGContextDrawPDFPage(context,page);
        CGPDFContextEndPage(context);
        CGPDFDocumentRelease(pdf);
    }
    CGPDFContextClose(context);
    CGContextRelease(context);
    return [data writeToFile:@(path) options:NSDataWritingAtomic error:nil];
}

// A paginated view lets the actual PrintOperation apply printer, range, copy and spool settings.
@interface MFPrintedPages : NSView
@property NSArray<NSData *> *pages;
@property CGFloat stride;
@end
@implementation MFPrintedPages
- (BOOL)knowsPageRange:(NSRangePointer)range { *range = NSMakeRange(1,self.pages.count); return YES; }
- (NSRect)rectForPage:(NSInteger)page {
    CGPDFDocumentRef pdf = MFOpenPDF(self.pages[page-1]);
    CGRect bounds = CGPDFPageGetBoxRect(CGPDFDocumentGetPage(pdf,1),kCGPDFMediaBox);
    CGPDFDocumentRelease(pdf);
    return NSMakeRect(0,(page-1)*self.stride,bounds.size.width,bounds.size.height);
}
- (void)drawRect:(NSRect)dirty {
    NSInteger current = NSPrintOperation.currentOperation.currentPage;
    if (current < 1 || current > (NSInteger)self.pages.count) return;
    CGContextRef context = NSGraphicsContext.currentContext.CGContext;
    CGPDFDocumentRef pdf = MFOpenPDF(self.pages[current-1]);
    CGContextSaveGState(context);
    CGContextTranslateCTM(context,0,(current-1)*self.stride);
    CGContextDrawPDFPage(context,CGPDFDocumentGetPage(pdf,1));
    CGContextRestoreGState(context);
    CGPDFDocumentRelease(pdf);
}
@end

static NSPrintInfo *MFPrintSettings(const char *json) {
    NSDictionary *s = [NSJSONSerialization JSONObjectWithData:[@(json) dataUsingEncoding:NSUTF8StringEncoding] options:0 error:nil];
    NSPrintInfo *info = [NSPrintInfo.sharedPrintInfo copy];
    NSString *name = s[@"PrinterName"];
    if (name.length) {
        NSPrinter *printer = [NSPrinter printerWithName:name];
        if (!printer) return nil;
        info.printer = printer;
    }
    info.paperSize = NSMakeSize([s[@"Width"] doubleValue]*WfPoint,[s[@"Height"] doubleValue]*WfPoint);
    info.leftMargin = info.rightMargin = info.topMargin = info.bottomMargin = 0;
    info.horizontallyCentered = NO;
    info.verticallyCentered = NO;
    info.horizontalPagination = NSPrintingPaginationModeClip;
    info.verticalPagination = NSPrintingPaginationModeClip;
    NSMutableDictionary *settings = info.dictionary;
    settings[NSPrintCopies] = s[@"Copies"];
    settings[NSPrintMustCollate] = s[@"Collate"];
    if ([s[@"Range"] intValue] == 2) {
        settings[NSPrintAllPages] = @NO;
        settings[NSPrintFirstPage] = s[@"FromPage"];
        settings[NSPrintLastPage] = s[@"ToPage"];
    }
    return info;
}

int mf_print_submit(void *handle, const char *json, const char *title) {
    MFPrintJob *job = (__bridge MFPrintJob *)handle;
    NSPrintInfo *info = MFPrintSettings(json);
    if (!info || !info.printer || !job.pages.count) return 0;
    CGFloat width = 0, stride = 0;
    for (NSData *data in job.pages) {
        CGPDFDocumentRef pdf = MFOpenPDF(data);
        CGSize size = CGPDFPageGetBoxRect(CGPDFDocumentGetPage(pdf,1),kCGPDFMediaBox).size;
        width = MAX(width,size.width);
        stride = MAX(stride,size.height);
        CGPDFDocumentRelease(pdf);
    }
    MFPrintedPages *view = [[MFPrintedPages alloc] initWithFrame:NSMakeRect(0,0,width,stride*job.pages.count)];
    view.pages = job.pages;
    view.stride = stride;
    NSPrintOperation *op = [NSPrintOperation printOperationWithView:view printInfo:info];
    op.jobTitle = @(title);
    op.showsPrintPanel = NO;
    op.showsProgressPanel = YES;
    return op.runOperation;
}

@interface MFPrintResponse : NSObject
@property NSInteger result;
@end
@implementation MFPrintResponse
- (void)printPanelDidEnd:(NSPrintPanel *)panel returnCode:(NSInteger)result contextInfo:(void *)info { self.result=result; }
@end

extern void mf_pump(void);
char *mf_print_panel(void *ownerView,const char *json,int allowRange,int allowSelection) {
    NSPrintInfo *info = MFPrintSettings(json);
    if (!info) return NULL;
    NSPrintPanel *panel = [NSPrintPanel printPanel];
    if (allowRange) panel.options |= NSPrintPanelShowsPageRange;
    else panel.options &= ~NSPrintPanelShowsPageRange;
    if (allowSelection) panel.options |= NSPrintPanelShowsPrintSelection;
    else panel.options &= ~NSPrintPanelShowsPrintSelection;
    NSWindow *parent=((__bridge NSView *)ownerView).window;
    if (parent) {
        MFPrintResponse *response=[MFPrintResponse new]; response.result=NSModalResponseContinue;
        if (@available(macOS 14.0,*)) {
            [panel beginSheetUsingPrintInfo:info onWindow:parent completionHandler:^(NSPrintPanelResult r) { response.result=r; }];
        } else {
            [panel beginSheetWithPrintInfo:info modalForWindow:parent delegate:response didEndSelector:@selector(printPanelDidEnd:returnCode:contextInfo:) contextInfo:NULL];
        }
        while (response.result==NSModalResponseContinue) mf_pump();
        if (response.result!=NSModalResponseOK) return NULL;
    } else if ([panel runModalWithPrintInfo:info] != NSModalResponseOK) return NULL;
    NSDictionary *s = info.dictionary;
    NSURL *file = s[NSPrintJobSavingURL];
    BOOL save = [info.jobDisposition isEqual:NSPrintSaveJob] && file.isFileURL;
    NSDictionary *result = @{ @"PrinterName":info.printer.name ?: @"", @"Copies":s[NSPrintCopies] ?: @1,
        @"Collate":s[NSPrintMustCollate] ?: @NO, @"Range": [s[NSPrintSelectionOnly] boolValue] ? @1 :
            s[NSPrintAllPages] && ![s[NSPrintAllPages] boolValue] ? @2 : @0,
        @"FromPage":s[NSPrintFirstPage] ?: @1, @"ToPage":s[NSPrintLastPage] ?: @1,
        @"Width":@(lround(info.paperSize.width/WfPoint)), @"Height":@(lround(info.paperSize.height/WfPoint)),
        @"SavePath":save ? file.path : @"" };
    NSData *data = [NSJSONSerialization dataWithJSONObject:result options:0 error:nil];
    return strdup([[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding].UTF8String);
}
