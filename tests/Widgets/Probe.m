#import <AppKit/AppKit.h>
#import <dlfcn.h>

static NSView *viewFor(void *handle) {
    static NSView *(*getView)(void *);
    if (!getView) {
        NSString *executable = NSProcessInfo.processInfo.arguments.firstObject;
        NSString *lib = [[executable stringByDeletingLastPathComponent] stringByAppendingPathComponent:@"libMacForms.dylib"];
        void *image = dlopen(lib.fileSystemRepresentation,RTLD_LAZY);
        getView = image ? dlsym(image,"MFWidgetView") : NULL;
    }
    return getView ? getView(handle) : nil;
}

int ProbeList(void *handle,int stage) {
    NSScrollView *scroll = (NSScrollView *)viewFor(handle);
    [scroll layoutSubtreeIfNeeded];
    if (stage == 3) {
        NSCollectionView *list = (NSCollectionView *)scroll.documentView;
        NSIndexPath *path = [NSIndexPath indexPathForItem:1 inSection:0];
        [list layoutSubtreeIfNeeded];
        NSCollectionViewItem *cell = [list itemAtIndexPath:path];
        return [list isKindOfClass:NSCollectionView.class] && [list numberOfItemsInSection:0] == 2 &&
            [list.selectionIndexPaths containsObject:path] && [cell.textField.stringValue hasPrefix:@"Second"];
    }
    NSTableView *table = (NSTableView *)scroll.documentView;
    if (![table isKindOfClass:NSTableView.class] || table.numberOfRows != 2) return 0;
    NSInteger row = stage == 1 ? 0 : 1;
    if (stage == 1 && table.headerView) return 0;
    if (stage == 2 && (table.numberOfColumns != 2 || table.tableColumns[1].width != 90 || !table.headerView)) return 0;
    NSView *cell = [table viewAtColumn:0 row:row makeIfNecessary:YES];
    NSButton *button = nil;
    for (NSView *sub in cell.subviews) if ([sub isKindOfClass:NSButton.class]) button=(NSButton *)sub;
    if (!button) return 0;
    [table selectRowIndexes:[NSIndexSet indexSetWithIndex:row] byExtendingSelection:NO];
    [button performClick:nil];
    return YES;
}
static NSButton *disclosure(NSOutlineView *tree) {
    NSTableRowView *row=[tree rowViewAtRow:0 makeIfNecessary:YES];
    for (NSView *child in row.subviews)
        if ([child.identifier isEqual:NSOutlineViewDisclosureButtonKey]) return (NSButton *)child;
    return nil;
}
int ProbeTree(void *handle) {
    NSScrollView *scroll=(NSScrollView *)viewFor(handle);
    NSOutlineView *tree=(NSOutlineView *)scroll.documentView;
    [tree layoutSubtreeIfNeeded];
    if (tree.window.firstResponder!=tree || tree.numberOfRows!=3) return NO;
    [disclosure(tree) performClick:nil];
    if (tree.numberOfRows!=1) return NO;
    [tree layoutSubtreeIfNeeded];
    [disclosure(tree) performClick:nil];
    if (tree.numberOfRows!=3) return NO;
    [tree selectRowIndexes:[NSIndexSet indexSetWithIndex:1] byExtendingSelection:NO];
    NSString *down=[NSString stringWithFormat:@"%C",(unichar)NSDownArrowFunctionKey];
    NSEvent *key=[NSEvent keyEventWithType:NSEventTypeKeyDown location:NSZeroPoint modifierFlags:0
        timestamp:0 windowNumber:tree.window.windowNumber context:nil characters:down
        charactersIgnoringModifiers:down isARepeat:NO keyCode:125];
    [tree keyDown:key];
    return tree.selectedRow==2;
}

int ProbeSplit(void *handle,int start,int finish) {
    NSView *view=viewFor(handle);
    if (!view.window) return NO;
    for (NSNumber *phase in @[@(NSEventTypeLeftMouseDown),@(NSEventTypeLeftMouseDragged),@(NSEventTypeLeftMouseUp)]) {
        NSEventType type=phase.unsignedIntegerValue;
        NSPoint point=[view convertPoint:NSMakePoint(type==NSEventTypeLeftMouseDown ? start : finish,30) toView:nil];
        NSEvent *event=[NSEvent mouseEventWithType:type location:point modifierFlags:0 timestamp:0
               windowNumber:view.window.windowNumber context:nil eventNumber:0 clickCount:1 pressure:1];
        if (type==NSEventTypeLeftMouseDown) [view mouseDown:event];
        else if (type==NSEventTypeLeftMouseDragged) [view mouseDragged:event];
        else [view mouseUp:event];
    }
    return YES;
}

int ProbeMask(void *handle) {
    NSScrollView *view = (NSScrollView *)viewFor(handle);
    NSTextView *editor = (NSTextView *)view.documentView;
    if (![editor isKindOfClass:NSTextView.class]) return 0;
    [editor insertText:@"x" replacementRange:NSMakeRange(0,0)];
    [editor insertText:@"12" replacementRange:NSMakeRange(0,0)];
    [editor insertText:@"34" replacementRange:editor.selectedRange];
    return [editor.string isEqualToString:@"12-34"];
}

int ProbeSpinnerLink(void *handle,int stage) {
    NSView *view=viewFor(handle);
    if (stage==1) {
        for (NSView *sub in view.subviews) if ([sub isKindOfClass:NSStepper.class]) {
            NSStepper *step=(NSStepper *)sub;
            step.integerValue=1; [step sendAction:step.action to:step.target]; return YES;
        }
        return NO;
    }
    if (![view isKindOfClass:NSTextView.class]) return NO;
    NSTextView *label=(NSTextView *)view;
    if (stage==2) {
        id link=[label.textStorage attribute:NSLinkAttributeName atIndex:15 effectiveRange:NULL];
        return link && [label.delegate textView:label clickedOnLink:link atIndex:15];
    }
    [label.window makeKeyAndOrderFront:nil];
    if (![label.window makeFirstResponder:label]) return NO;
    NSEvent *key=[NSEvent keyEventWithType:NSEventTypeKeyDown location:NSZeroPoint modifierFlags:0
              timestamp:0 windowNumber:label.window.windowNumber context:nil characters:@"\r"
              charactersIgnoringModifiers:@"\r" isARepeat:NO keyCode:36];
    [NSApp sendEvent:key];
    return YES;
}
