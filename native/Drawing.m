#import "Drawing.h"
#import <ImageIO/ImageIO.h>

static NSColor *MFColor(uint32_t argb) {
    return [NSColor colorWithSRGBRed:((argb >> 16) & 255)/255.0 green:((argb >> 8) & 255)/255.0
                                blue:(argb & 255)/255.0 alpha:(argb >> 24)/255.0];
}

@interface MFDrawingImage : NSObject
@property NSImage *original;
@property CGContextRef bitmap;
@property NSInteger width;
@property NSInteger height;
@property int format;
- (BOOL)makeBitmap;
- (CGImageRef)copyImage CF_RETURNS_RETAINED;
@end

@implementation MFDrawingImage
- (void)dealloc { if (_bitmap) CGContextRelease(_bitmap); }
- (BOOL)makeBitmap {
    if (self.bitmap) return YES;
    if (self.width < 1 || self.height < 1 || (NSUInteger)self.width > SIZE_MAX / 4 / (NSUInteger)self.height) return NO;
    CGColorSpaceRef color = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
    _bitmap = CGBitmapContextCreate(NULL,self.width,self.height,8,0,color,kCGImageAlphaPremultipliedLast|kCGBitmapByteOrder32Big);
    CGColorSpaceRelease(color);
    if (!self.bitmap) return NO;
    if (self.original) {
        [NSGraphicsContext saveGraphicsState];
        NSGraphicsContext.currentContext = [NSGraphicsContext graphicsContextWithCGContext:self.bitmap flipped:NO];
        [self.original drawInRect:NSMakeRect(0,0,self.width,self.height) fromRect:NSZeroRect
                        operation:NSCompositingOperationCopy fraction:1 respectFlipped:NO hints:nil];
        [NSGraphicsContext restoreGraphicsState];
        self.original = nil;
    }
    CGContextTranslateCTM(self.bitmap,0,self.height);
    CGContextScaleCTM(self.bitmap,1,-1);
    return YES;
}
- (CGImageRef)copyImage {
    if (![self makeBitmap]) return NULL;
    return CGBitmapContextCreateImage(self.bitmap);
}
@end

void *mf_image_create(int w,int h) {
    MFDrawingImage *image = [MFDrawingImage new];
    image.width = w;
    image.height = h;
    return [image makeBitmap] ? (__bridge_retained void *)image : NULL;
}

void *mf_image_load(const void *bytes,int length) {
    NSData *data = [NSData dataWithBytes:bytes length:length];
    NSImage *original = [[NSImage alloc] initWithData:data];
    if (!original) return NULL;
    NSImageRep *rep = original.representations.firstObject;
    MFDrawingImage *image = [MFDrawingImage new];
    image.original = original;
    image.width = rep.pixelsWide > 0 ? rep.pixelsWide : (NSInteger)original.size.width;
    image.height = rep.pixelsHigh > 0 ? rep.pixelsHigh : (NSInteger)original.size.height;
    CGImageSourceRef source = CGImageSourceCreateWithData((__bridge CFDataRef)data,NULL);
    if (source) {
        NSString *type = (__bridge NSString *)CGImageSourceGetType(source);
        if ([type isEqualToString:@"public.jpeg"]) image.format = 2;
        else if ([type isEqualToString:@"com.microsoft.bmp"]) image.format = 3;
        else if ([type isEqualToString:@"public.png"]) image.format = 1;
        CFRelease(source);
    }
    return (__bridge_retained void *)image;
}
int mf_image_size(void *handle,int *w,int *h) {
    MFDrawingImage *image = (__bridge MFDrawingImage *)handle;
    *w = (int)image.width;
    *h = (int)image.height;
    return image.format;
}
void mf_drawing_release(void *handle) { CFBridgingRelease(handle); }

static unsigned char *MFPixel(MFDrawingImage *image,int x,int y) {
    if (x < 0 || y < 0 || x >= image.width || y >= image.height || ![image makeBitmap]) return NULL;
    return (unsigned char *)CGBitmapContextGetData(image.bitmap) + y*CGBitmapContextGetBytesPerRow(image.bitmap) + 4*x;
}
unsigned mf_pixel_get(void *handle,int x,int y) {
    unsigned char *p = MFPixel((__bridge MFDrawingImage *)handle,x,y);
    if (!p || !p[3]) return 0;
    unsigned alpha = p[3];
    unsigned r = MIN(255,(p[0]*255 + alpha/2)/alpha);
    unsigned g = MIN(255,(p[1]*255 + alpha/2)/alpha);
    unsigned b = MIN(255,(p[2]*255 + alpha/2)/alpha);
    return (alpha<<24) | (r<<16) | (g<<8) | b;
}
void mf_pixel_set(void *handle,int x,int y,unsigned color) {
    unsigned char *p = MFPixel((__bridge MFDrawingImage *)handle,x,y);
    if (!p) return;
    unsigned alpha = color >> 24;
    p[0] = (((color >> 16)&255)*alpha + 127)/255;
    p[1] = (((color >> 8)&255)*alpha + 127)/255;
    p[2] = ((color&255)*alpha + 127)/255;
    p[3] = alpha;
}
void *mf_image_encode(void *handle,int format,int *length) {
    MFDrawingImage *image = (__bridge MFDrawingImage *)handle;
    CFStringRef type = format == 1 ? CFSTR("public.png") : format == 2 ? CFSTR("public.jpeg") : CFSTR("com.microsoft.bmp");
    CGImageRef cg = [image copyImage];
    if (!cg) return NULL;
    NSMutableData *data = [NSMutableData data];
    CGImageDestinationRef out = CGImageDestinationCreateWithData((__bridge CFMutableDataRef)data,type,1,NULL);
    BOOL ok = NO;
    if (out) {
        CGImageDestinationAddImage(out,cg,NULL);
        ok = CGImageDestinationFinalize(out);
        CFRelease(out);
    }
    CGImageRelease(cg);
    if (!ok || data.length > INT_MAX) return NULL;
    void *copy = malloc(data.length);
    if (!copy) return NULL;
    memcpy(copy,data.bytes,data.length);
    *length = (int)data.length;
    return copy;
}

@interface MFDrawingContext : NSObject
@property NSGraphicsContext *graphics;
@property MFDrawingImage *image;
@end
@implementation MFDrawingContext
@end

void *mf_graphics_current(void) {
    if (!NSGraphicsContext.currentContext) return NULL;
    MFDrawingContext *g = [MFDrawingContext new];
    g.graphics = NSGraphicsContext.currentContext;
    return (__bridge_retained void *)g;
}
void *mf_graphics_image(void *handle) {
    MFDrawingImage *image = (__bridge MFDrawingImage *)handle;
    if (![image makeBitmap]) return NULL;
    MFDrawingContext *g = [MFDrawingContext new];
    g.image = image;
    g.graphics = [NSGraphicsContext graphicsContextWithCGContext:image.bitmap flipped:YES];
    return (__bridge_retained void *)g;
}
void mf_graphics_begin(void *handle,const float *matrix,int useClip,float x,float y,float w,float h,int smooth) {
    MFDrawingContext *g = (__bridge MFDrawingContext *)handle;
    [NSGraphicsContext saveGraphicsState];
    NSGraphicsContext.currentContext = g.graphics;
    CGContextRef context = g.graphics.CGContext;
    CGContextSaveGState(context);
    if (useClip) CGContextClipToRect(context,CGRectMake(x,y,w,h));
    CGContextConcatCTM(context,CGAffineTransformMake(matrix[0],matrix[1],matrix[2],matrix[3],matrix[4],matrix[5]));
    if (smooth >= 0) CGContextSetShouldAntialias(context,smooth);
}
void mf_graphics_end(void) {
    CGContextRestoreGState(NSGraphicsContext.currentContext.CGContext);
    [NSGraphicsContext restoreGraphicsState];
}

void mf_clear(unsigned color,float width,float height) {
    [MFColor(color) setFill];
    NSRectFillUsingOperation(NSMakeRect(0,0,width,height),NSCompositingOperationCopy);
}
void mf_shape(int kind,uint32_t argb,float lineWidth,float x,float y,float w,float h) {
    NSRect rect = NSMakeRect(x,y,w,h);
    NSBezierPath *path;
    if (kind == 2) {
        path = [NSBezierPath bezierPath];
        [path moveToPoint:NSMakePoint(x,y)];
        [path lineToPoint:NSMakePoint(w,h)];
    } else {
        path = kind == 1 ? [NSBezierPath bezierPathWithOvalInRect:rect] : [NSBezierPath bezierPathWithRect:rect];
    }
    [MFColor(argb) set];
    if (lineWidth < 0) [path fill];
    else { path.lineWidth = lineWidth; [path stroke]; }
}
void mf_polygon(const float *xy,int count,int close,int winding,unsigned argb,float width) {
    NSBezierPath *path = [NSBezierPath bezierPath];
    path.windingRule = winding ? NSWindingRuleNonZero : NSWindingRuleEvenOdd;
    [path moveToPoint:NSMakePoint(xy[0],xy[1])];
    for (int i=1;i<count;i++) [path lineToPoint:NSMakePoint(xy[2*i],xy[2*i+1])];
    if (close) [path closePath];
    [MFColor(argb) set];
    if (width < 0) [path fill];
    else { path.lineWidth = width; [path stroke]; }
}
void mf_arc(float x,float y,float w,float h,float angle,float sweep,int pie,unsigned argb,float width) {
    if (w<=0 || h<=0 || sweep==0) return;
    NSBezierPath *path = [NSBezierPath bezierPath];
    if (pie) [path moveToPoint:NSZeroPoint];
    [path appendBezierPathWithArcWithCenter:NSZeroPoint radius:1 startAngle:angle endAngle:angle+sweep clockwise:sweep<0];
    if (pie) [path closePath];
    NSAffineTransform *transform=[NSAffineTransform transform];
    [transform translateXBy:x+w/2 yBy:y+h/2];
    [transform scaleXBy:w/2 yBy:h/2];
    [path transformUsingAffineTransform:transform];
    [MFColor(argb) set];
    if (width < 0) [path fill];
    else { path.lineWidth = width; [path stroke]; }
}

static NSDictionary *MFTextStyle(const char *family,float size,int style,uint32_t argb) {
    NSFont *font = [NSFont fontWithName:@(family) size:size] ?: [NSFont systemFontOfSize:size];
    NSFontTraitMask traits = (style & 1 ? NSBoldFontMask : 0) | (style & 2 ? NSItalicFontMask : 0);
    if (traits) font = [NSFontManager.sharedFontManager convertFont:font toHaveTrait:traits];
    return @{NSFontAttributeName:font, NSForegroundColorAttributeName:MFColor(argb),
             NSUnderlineStyleAttributeName:@(style & 4 ? NSUnderlineStyleSingle : NSUnderlineStyleNone),
             NSStrikethroughStyleAttributeName:@(style & 8 ? NSUnderlineStyleSingle : NSUnderlineStyleNone)};
}
void mf_draw_string(const char *text,const char *family,float size,int style,uint32_t argb,float x,float y,float width,float height) {
    if (width <= 0 && height <= 0) {
        [@(text) drawAtPoint:NSMakePoint(x,y) withAttributes:MFTextStyle(family,size,style,argb)];
        return;
    }
    NSSize limit=NSMakeSize(width>0 ? width : CGFLOAT_MAX,height>0 ? height : CGFLOAT_MAX);
    [@(text) drawWithRect:NSMakeRect(x,y,limit.width,limit.height)
                  options:NSStringDrawingUsesLineFragmentOrigin|NSStringDrawingUsesFontLeading
               attributes:MFTextStyle(family,size,style,argb) context:nil];
}
void mf_measure_string(const char *text,const char *family,float size,int style,float width,float height,float *w,float *h) {
    NSSize limit=NSMakeSize(width>0 ? width : CGFLOAT_MAX,height>0 ? height : CGFLOAT_MAX);
    NSRect rect=[@(text) boundingRectWithSize:limit options:NSStringDrawingUsesLineFragmentOrigin|NSStringDrawingUsesFontLeading
                                    attributes:MFTextStyle(family,size,style,0xff000000) context:nil];
    *w=ceil(rect.size.width); *h=ceil(rect.size.height);
}

NSImage *mf_native_image(void *handle) {
    if (!handle) return nil;
    MFDrawingImage *image = (__bridge MFDrawingImage *)handle;
    NSImage *source = image.original;
    if (!source) {
        CGImageRef cg=[image copyImage];
        if (!cg) return nil;
        source=[[NSImage alloc] initWithCGImage:cg size:NSMakeSize(image.width,image.height)];
        CGImageRelease(cg);
    }
    return source;
}
void mf_image_draw(void *handle,float x,float y,float w,float h,float sx,float sy,float sw,float sh) {
    MFDrawingImage *image = (__bridge MFDrawingImage *)handle;
    NSImage *source = mf_native_image(handle);
    if (!source) return;
    // AppKit source coordinates use a bottom-left origin even in a flipped destination.
    NSRect crop=NSMakeRect(sx*source.size.width/image.width,(image.height-sy-sh)*source.size.height/image.height,
                           sw*source.size.width/image.width,sh*source.size.height/image.height);
    [source drawInRect:NSMakeRect(x,y,w,h) fromRect:crop operation:NSCompositingOperationSourceOver fraction:1 respectFlipped:YES hints:nil];
}

static NSFont *MFFontForFamily(NSString *family, int style, CGFloat size) {
    NSFontTraitMask traits = (style & 1 ? NSBoldFontMask : 0) | (style & 2 ? NSItalicFontMask : 0);
    NSFont *font = [NSFontManager.sharedFontManager fontWithFamily:family traits:traits weight:5 size:size];
    if (!font) font = [NSFont fontWithName:family size:size];
    if (!font) font = [NSFont systemFontOfSize:size];
    if (traits) font = [NSFontManager.sharedFontManager convertFont:font toHaveTrait:traits];
    return font;
}
float mf_font_height(const char *family,int style,float size) {
    NSFont *font=MFFontForFamily(@(family),style,size);
    return (float)(font.ascender-font.descender+font.leading);
}
char *mf_font_families(void) {
    NSArray *names = [NSFontManager.sharedFontManager.availableFontFamilies sortedArrayUsingSelector:@selector(localizedCaseInsensitiveCompare:)];
    NSData *json=[NSJSONSerialization dataWithJSONObject:names options:0 error:nil];
    return strdup([[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding].UTF8String);
}

// TextRenderer follows underline/mnemonic conventions from WinForms and lets
// AppKit lay out the composed string, including bidirectional text and glyph clusters.
void mf_format_text(const char *input,const char *family,float size,int style,uint32_t ink,uint32_t bg,
                    uint32_t flags,float x,float y,float width,float height,int draw,float *outW,float *outH) {
    enum { Center=1, Right=2, VCenter=4, Bottom=8, WordBreak=16, SingleLine=32, ExpandTabs=64,
           NoClip=256, NoPrefix=2048, PathEllipsis=16384, EndEllipsis=32768, RightToLeft=131072,
           WordEllipsis=262144, HidePrefix=1048576, PrefixOnly=2097152, NoPadding=268435456, SidePadding=536870912 };
    NSString *raw=@(input);
    if (flags & SingleLine) {
        raw=[raw stringByReplacingOccurrencesOfString:@"\r\n" withString:@" "];
        raw=[raw stringByReplacingOccurrencesOfString:@"\n" withString:@" "];
        raw=[raw stringByReplacingOccurrencesOfString:@"\r" withString:@" "];
    }
    NSMutableString *text=[NSMutableString string];
    NSMutableIndexSet *mnemonics=[NSMutableIndexSet indexSet];
    for (NSUInteger i=0;i<raw.length;) {
        NSRange next=[raw rangeOfComposedCharacterSequenceAtIndex:i];
        NSString *piece=[raw substringWithRange:next];
        i=NSMaxRange(next);
        if (!(flags & NoPrefix) && [piece isEqualToString:@"&"]) {
            if (i>=raw.length) break;
            next=[raw rangeOfComposedCharacterSequenceAtIndex:i];
            piece=[raw substringWithRange:next];
            i=NSMaxRange(next);
            if (!(flags & HidePrefix) && ![piece isEqualToString:@"&"])
                [mnemonics addIndexesInRange:NSMakeRange(text.length,piece.length)];
        }
        [text appendString:piece];
    }
    *outW=*outH=0;
    if (!text.length) return;
    NSMutableDictionary *attributes=[MFTextStyle(family,size,style,ink) mutableCopy];
    NSFont *font=attributes[NSFontAttributeName];
    CGFloat glyphHeight=ceil(font.ascender-font.descender+font.leading), overhang=glyphHeight/6;
    CGFloat left=0,right=0;
    if (!(flags & NoPadding)) {
        CGFloat factor=(flags & SidePadding)?2:1;
        left=ceil(overhang*factor);
        right=ceil(overhang*(factor+0.5));
    }
    NSMutableParagraphStyle *paragraph=[[NSMutableParagraphStyle alloc] init];
    if (flags & RightToLeft) paragraph.baseWritingDirection=NSWritingDirectionRightToLeft;
    if (flags & ExpandTabs) {
        paragraph.tabStops=@[];
        paragraph.defaultTabInterval=8*[@" " sizeWithAttributes:attributes].width;
    }
    if (flags & PathEllipsis) paragraph.lineBreakMode=NSLineBreakByTruncatingMiddle;
    else if (flags & (EndEllipsis|WordEllipsis)) paragraph.lineBreakMode=NSLineBreakByTruncatingTail;
    else paragraph.lineBreakMode=(flags & WordBreak) && !(flags & SingleLine) ? NSLineBreakByWordWrapping : NSLineBreakByClipping;
    if (bg >> 24) attributes[NSBackgroundColorAttributeName]=MFColor(bg);
    if (flags & PrefixOnly) attributes[NSForegroundColorAttributeName]=NSColor.clearColor;
    attributes[NSParagraphStyleAttributeName]=paragraph;
    NSMutableAttributedString *styled=[[NSMutableAttributedString alloc] initWithString:text attributes:attributes];
    [mnemonics enumerateRangesUsingBlock:^(NSRange range,BOOL *stop) {
        [styled addAttribute:NSUnderlineStyleAttributeName value:@(NSUnderlineStyleSingle) range:range];
        [styled addAttribute:NSUnderlineColorAttributeName value:MFColor(ink) range:range];
    }];
    CGFloat available=width>0 ? MAX(1,width-left-right) : CGFLOAT_MAX;
    BOOL constrain=(flags & (PathEllipsis|EndEllipsis|WordEllipsis)) || ((flags & WordBreak) && !(flags & SingleLine));
    NSStringDrawingOptions opts=NSStringDrawingUsesLineFragmentOrigin|NSStringDrawingUsesFontLeading;
    if (flags & (EndEllipsis|PathEllipsis|WordEllipsis)) opts|=NSStringDrawingTruncatesLastVisibleLine;
    NSRect measured=[styled boundingRectWithSize:NSMakeSize(constrain ? available : CGFLOAT_MAX,CGFLOAT_MAX) options:opts context:nil];
    CGFloat w=ceil(measured.size.width),h=ceil(measured.size.height);
    *outW=w+left+right; *outH=h;
    if (!draw || width<=0 || height<=0) return;
    [NSGraphicsContext saveGraphicsState];
    if (!(flags & NoClip)) NSRectClip(NSMakeRect(x,y,width,height));
    CGFloat dx=left,dy=0;
    if (flags & Center) dx+=(available-w)/2;
    else if (flags & Right) dx+=available-w;
    if (flags & VCenter) dy=(height-h)/2;
    else if (flags & Bottom) dy=height-h;
    NSRect rect=NSMakeRect(x+floor(dx),y+floor(dy),constrain ? MIN(available,w+1) : w+1,h+1);
    [styled drawWithRect:rect options:opts context:nil];
    [NSGraphicsContext restoreGraphicsState];
}
