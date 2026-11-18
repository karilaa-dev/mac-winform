#import "TextEditing.h"
#import "ListWidgets.h"
#import <objc/runtime.h>

typedef int (*MFMaskInput)(void *,int,int,const char *);
static const char selectionKey, maskKey;

static NSTextView *MFText(NSView *view) {
    if ([view isKindOfClass:NSTextView.class]) return (NSTextView *)view;
    if ([view isKindOfClass:NSScrollView.class]) {
        NSView *document = ((NSScrollView *)view).documentView;
        return [document isKindOfClass:NSTextView.class] ? (NSTextView *)document : nil;
    }
    return [view isKindOfClass:NSTextField.class] ? (NSTextView *)[(NSTextField *)view currentEditor] : nil;
}
static NSRange MFClamped(NSRange range,NSUInteger length) {
    range.location = MIN(range.location,length);
    range.length = MIN(range.length,length-range.location);
    return range;
}
static NSRange MFSelection(NSView *view) {
    NSTextView *editor = MFText(view);
    if (editor) return editor.selectedRange;
    NSValue *stored = objc_getAssociatedObject(view,&selectionKey);
    NSString *value = [(NSTextField *)view stringValue];
    return MFClamped(stored ? stored.rangeValue : NSMakeRange(0,0),value.length);
}
static void MFMoveSelection(NSView *view,NSRange range) {
    NSTextView *editor = MFText(view);
    NSString *value = editor ? editor.string : [(NSTextField *)view stringValue];
    range = MFClamped(range,value.length);
    objc_setAssociatedObject(view,&selectionKey,[NSValue valueWithRange:range],OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    if (editor) editor.selectedRange = range;
}

void MFFieldBegan(NSTextField *field,NSDictionary *info) {
    NSTextView *editor = info[@"NSFieldEditor"];
    NSValue *stored = objc_getAssociatedObject(field,&selectionKey);
    if (stored && editor) editor.selectedRange = MFClamped(stored.rangeValue,editor.string.length);
}
void MFFieldFinished(NSTextField *field,NSDictionary *info) {
    NSTextView *editor = info[@"NSFieldEditor"];
    if (editor) objc_setAssociatedObject(field,&selectionKey,[NSValue valueWithRange:editor.selectedRange],OBJC_ASSOCIATION_RETAIN_NONATOMIC);
}
void mf_edit_selection(void *handle,int *start,int *length) {
    NSRange r = MFSelection(MFWidgetView(handle)); *start=(int)r.location; *length=(int)r.length;
}
void mf_edit_select(void *handle,int start,int length) {
    MFMoveSelection(MFWidgetView(handle),NSMakeRange(MAX(0,start),MAX(0,length)));
}
void mf_edit_replace(void *handle,const char *text) {
    NSView *view = MFWidgetView(handle); NSTextView *editor = MFText(view);
    NSRange range = MFSelection(view); NSString *value = @(text);
    if (editor) {
        [editor.textStorage replaceCharactersInRange:range withString:value];
        [editor didChangeText];
    } else {
        NSTextField *field = (NSTextField *)view;
        field.stringValue = [field.stringValue stringByReplacingCharactersInRange:range withString:value];
    }
    MFMoveSelection(view,NSMakeRange(range.location+value.length,0));
}
void mf_edit_scroll(void *handle) {
    NSTextView *editor = MFText(MFWidgetView(handle)); if (editor) [editor scrollRangeToVisible:editor.selectedRange];
}
void mf_edit_wrap(void *handle,int wrap) {
    NSView *view = MFWidgetView(handle);
    if (![view isKindOfClass:NSScrollView.class]) return;
    NSScrollView *scroll = (NSScrollView *)view;
    NSTextView *editor = MFText(view);
    editor.horizontallyResizable = !wrap;
    editor.textContainer.widthTracksTextView = wrap;
    editor.textContainer.containerSize = NSMakeSize(wrap ? MAX(1,scroll.contentSize.width) : CGFLOAT_MAX,CGFLOAT_MAX);
    scroll.hasHorizontalScroller = !wrap;
}
void mf_mask_install(void *handle,MFMaskInput handler) {
    objc_setAssociatedObject(MFWidgetView(handle),&maskKey,[NSValue valueWithPointer:(void *)handler],OBJC_ASSOCIATION_RETAIN_NONATOMIC);
}
BOOL MFShouldReplaceText(void *handle,NSRange range,NSString *replacement) {
    NSValue *value = objc_getAssociatedObject(MFWidgetView(handle),&maskKey);
    MFMaskInput callback = value ? value.pointerValue : NULL;
    return !callback || !replacement || callback(handle,(int)range.location,(int)range.length,replacement.UTF8String);
}
static NSDictionary *MFAttrs(NSTextView *editor) {
    return editor.selectedRange.length ? [editor.textStorage attributesAtIndex:editor.selectedRange.location effectiveRange:NULL] : editor.typingAttributes;
}
static void MFSetAttribute(NSTextView *editor,NSAttributedStringKey name,id value) {
    if (editor.selectedRange.length) [editor.textStorage addAttribute:name value:value range:editor.selectedRange];
    else {
        NSMutableDictionary *attrs = editor.typingAttributes.mutableCopy;
        attrs[name] = value; editor.typingAttributes = attrs;
    }
}
void mf_rich_color(void *handle,uint32_t argb,int background) {
    NSColor *color = [NSColor colorWithSRGBRed:((argb>>16)&255)/255.0 green:((argb>>8)&255)/255.0 blue:(argb&255)/255.0 alpha:(argb>>24)/255.0];
    MFSetAttribute(MFText(MFWidgetView(handle)),background ? NSBackgroundColorAttributeName : NSForegroundColorAttributeName,color);
}
uint32_t mf_rich_get_color(void *handle,int background) {
    NSColor *c = MFAttrs(MFText(MFWidgetView(handle)))[background ? NSBackgroundColorAttributeName : NSForegroundColorAttributeName];
    c=[(c ?: (background ? NSColor.whiteColor : NSColor.blackColor)) colorUsingColorSpace:NSColorSpace.sRGBColorSpace];
    return ((uint32_t)lround(c.alphaComponent*255)<<24) | ((uint32_t)lround(c.redComponent*255)<<16) | ((uint32_t)lround(c.greenComponent*255)<<8) | (uint32_t)lround(c.blueComponent*255);
}
void mf_rich_font(void *handle,const char *family,float size,int style) {
    NSTextView *editor=MFText(MFWidgetView(handle));
    NSFont *font=[NSFont fontWithName:@(family) size:size*4.0/3.0] ?: [NSFont systemFontOfSize:size*4.0/3.0];
    NSFontTraitMask traits = ((style&1) ? NSBoldFontMask : 0) | ((style&2) ? NSItalicFontMask : 0);
    if (traits) font=[NSFontManager.sharedFontManager convertFont:font toHaveTrait:traits];
    MFSetAttribute(editor,NSFontAttributeName,font);
    MFSetAttribute(editor,NSUnderlineStyleAttributeName,@((style&4) != 0));
    MFSetAttribute(editor,NSStrikethroughStyleAttributeName,@((style&8) != 0));
}
char *mf_rich_get_font(void *handle,float *points,int *style) {
    NSDictionary *attrs=MFAttrs(MFText(MFWidgetView(handle)));
    NSFont *f=attrs[NSFontAttributeName]; if (!f) return NULL;
    NSFontTraitMask traits=[NSFontManager.sharedFontManager traitsOfFont:f];
    *points=f.pointSize*0.75;
    *style=((traits & NSBoldFontMask) ? 1 : 0) | ((traits & NSItalicFontMask) ? 2 : 0) |
           ([attrs[NSUnderlineStyleAttributeName] intValue] ? 4 : 0) | ([attrs[NSStrikethroughStyleAttributeName] intValue] ? 8 : 0);
    return strdup(f.familyName.UTF8String);
}
char *mf_rich_get_rtf(void *handle,int selected) {
    NSTextView *editor = MFText(MFWidgetView(handle));
    NSRange range = selected ? editor.selectedRange : NSMakeRange(0,editor.string.length);
    NSData *bytes = [editor RTFFromRange:range];
    NSString *rtf = [[NSString alloc] initWithData:bytes encoding:NSISOLatin1StringEncoding];
    return strdup(rtf.UTF8String ?: "");
}
int mf_rich_set_rtf(void *handle,const char *utf8,int selected) {
    NSString *str=@(utf8);
    NSData *bytes=[str dataUsingEncoding:NSISOLatin1StringEncoding] ?: [str dataUsingEncoding:NSUTF8StringEncoding];
    NSAttributedString *attributed=str.length ? [[NSAttributedString alloc] initWithRTF:bytes documentAttributes:NULL] : [[NSAttributedString alloc] initWithString:@""];
    if (!attributed) return 0;
    NSTextView *editor=MFText(MFWidgetView(handle));
    NSRange range=selected ? editor.selectedRange : NSMakeRange(0,editor.string.length);
    [editor.textStorage replaceCharactersInRange:range withAttributedString:attributed];
    editor.selectedRange=NSMakeRange(selected ? range.location+attributed.length : 0,0);
    [editor didChangeText];
    return 1;
}

int mf_edit_history(void *handle,int command) {
    NSTextView *editor=MFText(MFWidgetView(handle));
    NSUndoManager *undo=editor.undoManager;
    switch (command) {
        case 0: return undo.canUndo;
        case 1: if (undo.canUndo) [undo undo]; break;
        case 2: return undo.canRedo;
        case 3: if (undo.canRedo) [undo redo]; break;
        case 4: if (editor) [undo removeAllActionsWithTarget:editor]; break;
    }
    return 0;
}
