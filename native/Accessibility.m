#import <AppKit/AppKit.h>
#import <objc/runtime.h>

void mf_ax_properties(NSView *view,const char *name,const char *help,int role) {
    static char originalRole;
    if (!view) return;
    id accessibility = view;
    if (name || help || role > 0) [accessibility setAccessibilityElement:YES];
    [accessibility setAccessibilityLabel:name ? @(name) : nil];
    [accessibility setAccessibilityHelp:help ? @(help) : nil];
    NSString *saved = objc_getAssociatedObject(view,&originalRole);
    if (role < 0) { if (saved) [accessibility setAccessibilityRole:saved]; return; }
    if (!saved && [accessibility accessibilityRole])
        objc_setAssociatedObject(view,&originalRole,[accessibility accessibilityRole],OBJC_ASSOCIATION_COPY_NONATOMIC);
    // AccessibleRole values are shared with the Microsoft reference metadata.
    static NSDictionary<NSNumber *,NSString *> *roles;
    static dispatch_once_t once;
    dispatch_once(&once,^{
        roles = @{@9:NSAccessibilityWindowRole,@11:NSAccessibilityMenuRole,@12:NSAccessibilityMenuItemRole,
            @20:NSAccessibilityGroupRole,@22:NSAccessibilityToolbarRole,@24:NSAccessibilityTableRole,
            @28:NSAccessibilityRowRole,@29:NSAccessibilityCellRole,@30:NSAccessibilityLinkRole,@33:NSAccessibilityListRole,
            @35:NSAccessibilityOutlineRole,@40:NSAccessibilityImageRole,@41:NSAccessibilityStaticTextRole,
            @42:NSAccessibilityTextFieldRole,@43:NSAccessibilityButtonRole,@44:NSAccessibilityCheckBoxRole,
            @45:NSAccessibilityRadioButtonRole,@46:NSAccessibilityComboBoxRole,@48:NSAccessibilityProgressIndicatorRole,
            @51:NSAccessibilitySliderRole};
    });
    if (roles[@(role)]) [accessibility setAccessibilityRole:roles[@(role)]];
}
void mf_ax_notify(NSView *view,int ev) {
    if (!view) return;
    NSAccessibilityNotificationName note = NSAccessibilityLayoutChangedNotification;
    if (ev == 0x8005) note = NSAccessibilityFocusedUIElementChangedNotification;
    else if (ev == 0x800c) note = NSAccessibilityTitleChangedNotification;
    else if (ev == 0x800e) note = NSAccessibilityValueChangedNotification;
    NSAccessibilityPostNotification(view,note);
}
