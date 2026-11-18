#import "Mdi.h"
#import "Keyboard.h"
#import <Carbon/Carbon.h>
#import <objc/runtime.h>

typedef int (*MFKeyCallback)(void *handle, int phase, int data);
static char peerKey;
static MFKeyCallback callback;
typedef int (*MFFocusCallback)(void *previous, void *next, int committed);
static MFFocusCallback focusCallback;
void mf_set_focus_callback(MFFocusCallback value) { focusCallback = value; }
void mf_set_key_callback(MFKeyCallback value) { callback = value; }
void mf_key_view(NSView *view, void *handle) {
    if (view) objc_setAssociatedObject(view,&peerKey,handle ? [NSValue valueWithPointer:handle] : nil,OBJC_ASSOCIATION_RETAIN_NONATOMIC);
}
static void *Peer(NSView *view) {
    for (;view;view=view.superview) {
        NSValue *value = objc_getAssociatedObject(view,&peerKey);
        if (value) return value.pointerValue;
    }
    return NULL;
}
int mf_key_modifiers(void) {
    NSEventModifierFlags flags = NSEvent.modifierFlags;
    return ((flags & NSEventModifierFlagShift) ? 0x10000 : 0)
        | ((flags & NSEventModifierFlagControl) ? 0x20000 : 0)
        | ((flags & NSEventModifierFlagOption) ? 0x40000 : 0);
}
static int CharacterKey(NSString *characters) {
    if (characters.length != 1) return 0;
    unichar key = [characters.uppercaseString characterAtIndex:0];
    if (key >= NSF1FunctionKey && key <= NSF24FunctionKey) return 112 + key - NSF1FunctionKey;
    if ((key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9') || key == ' ') return key;
    switch (key) {
        case ';': return 186; case '=': return 187; case ',': return 188; case '-': return 189;
        case '.': return 190; case '/': return 191; case 96: return 192; case '[': return 219;
        case 92: return 220; case ']': return 221; case 39: return 222; default: return 0;
    }
}
static int VirtualKey(NSEvent *event) {
    switch (event.keyCode) {
        case 36: case 76: return 13;
        case 48: return 9;
        case 51: return 8;
        case 53: return 27;
        case 55: return 91;
        case 54: return 92;
        case 56: case 60: return 16;
        case 57: return 20;
        case 58: case 61: return 18;
        case 59: case 62: return 17;
        case 114: return 45;
        case 115: return 36;
        case 116: return 33;
        case 117: return 46;
        case 119: return 35;
        case 121: return 34;
        case 123: return 37;
        case 124: return 39;
        case 125: return 40;
        case 126: return 38;
    }
    int key = CharacterKey(event.charactersIgnoringModifiers);
    if (key) return key;
    if (event.modifierFlags & NSEventModifierFlagShift) {
        // AppKit retains Shift in charactersIgnoringModifiers (e.g. "!" on the 1 key).
        key = CharacterKey([event charactersByApplyingModifiers:0]);
        if (key) return key;
    }
    // A non-Latin letter is still a WinForms virtual key. KeyPress and the
    // field editor receive the event's original text independently of this map.
    static const unsigned char latinKeys[] = {
        [kVK_ANSI_A]='A', [kVK_ANSI_B]='B', [kVK_ANSI_C]='C', [kVK_ANSI_D]='D', [kVK_ANSI_E]='E',
        [kVK_ANSI_F]='F', [kVK_ANSI_G]='G', [kVK_ANSI_H]='H', [kVK_ANSI_I]='I', [kVK_ANSI_J]='J',
        [kVK_ANSI_K]='K', [kVK_ANSI_L]='L', [kVK_ANSI_M]='M', [kVK_ANSI_N]='N', [kVK_ANSI_O]='O',
        [kVK_ANSI_P]='P', [kVK_ANSI_Q]='Q', [kVK_ANSI_R]='R', [kVK_ANSI_S]='S', [kVK_ANSI_T]='T',
        [kVK_ANSI_U]='U', [kVK_ANSI_V]='V', [kVK_ANSI_W]='W', [kVK_ANSI_X]='X', [kVK_ANSI_Y]='Y', [kVK_ANSI_Z]='Z'
    };
    return event.keyCode < sizeof(latinKeys) ? latinKeys[event.keyCode] : 0;
}
static void *ResponderPeer(NSResponder *responder) {
    if ([responder isKindOfClass:NSTextView.class] && ((NSTextView *)responder).isFieldEditor)
        responder = (NSResponder *)((NSTextView *)responder).delegate;
    return [responder isKindOfClass:NSView.class] ? Peer((NSView *)responder) : NULL;
}
@implementation MFWindow {
    BOOL changingFocus;
}
- (BOOL)makeFirstResponder:(NSResponder *)responder {
    if (!focusCallback || changingFocus) return [super makeFirstResponder:responder];
    void *old = ResponderPeer(self.firstResponder);
    void *next = ResponderPeer(responder);

    changingFocus = YES;
    @try {
        if (old != next && old && next && !focusCallback(old,next,0)) return NO;
        BOOL ok = [super makeFirstResponder:responder];
        void *actual = ResponderPeer(self.firstResponder);
        if (ok) focusCallback(old,actual,1);
        return ok;
    } @finally { changingFocus = NO; }
}
- (void)sendEvent:(NSEvent *)event {
    if (event.type == NSEventTypeLeftMouseDown) {
        NSView *root = self.contentView;
        NSPoint point = [root convertPoint:event.locationInWindow fromView:nil];
        for (NSView *view = [root hitTest:[root convertPoint:point toView:root.superview]]; view; view = view.superview)
            if ([view isKindOfClass:MFMdiFrame.class]) { [(MFMdiFrame *)view activateChild]; break; }
    }
    if (!callback || (event.type != NSEventTypeKeyDown && event.type != NSEventTypeKeyUp)) {
        [super sendEvent:event]; return;
    }
    NSResponder *focus = self.firstResponder;
    if ([focus isKindOfClass:NSTextView.class] && ((NSTextView *)focus).isFieldEditor)
        focus = (NSResponder *)((NSTextView *)focus).delegate;
    void *peer = [focus isKindOfClass:NSView.class] ? Peer((NSView *)focus) : NULL;
    if (!peer) peer = Peer(self.contentView);
    if (!peer) { [super sendEvent:event]; return; }
    NSEventModifierFlags flags = event.modifierFlags;
    int key = VirtualKey(event) | ((flags & NSEventModifierFlagShift) ? 0x10000 : 0)
        | ((flags & NSEventModifierFlagControl) ? 0x20000 : 0) | ((flags & NSEventModifierFlagOption) ? 0x40000 : 0);
    BOOL down = event.type == NSEventTypeKeyDown;
    if (callback(peer,down ? 1 : 2,key)) return;
    if (down && (flags & NSEventModifierFlagOption) && !(flags & (NSEventModifierFlagControl | NSEventModifierFlagCommand))) {
        NSString *plain=event.charactersIgnoringModifiers;
        if (plain.length == 1 && callback(peer,4,[plain characterAtIndex:0])) return;
    }
    if (down && !(flags & NSEventModifierFlagCommand)) {
        NSString *characters = event.characters;
        NSMutableString *replacement = [NSMutableString string];
        BOOL changed = NO;
        for (NSUInteger i=0; i<characters.length; i++) {
            unichar c = [characters characterAtIndex:i];
            if (c >= 0xf700 && c <= 0xf8ff) break;
            int value = callback(peer,3,c == 127 ? 8 : c);
            if (value < 0) { changed = YES; continue; }
            if (value != (c == 127 ? 8 : c)) { changed = YES; c = (unichar)value; }
            [replacement appendString:[NSString stringWithCharacters:&c length:1]];
        }
        if (changed) {
            if (replacement.length && [self.firstResponder conformsToProtocol:@protocol(NSTextInputClient)])
                [(id<NSTextInputClient>)self.firstResponder insertText:replacement replacementRange:NSMakeRange(NSNotFound,0)];
            return;
        }
    }
    [super sendEvent:event];
}
@end
