using System.Runtime.InteropServices;

// Exercise the AppKit controls actually displayed by this process. The probe
// uses no desktop-wide key injection and needs no Accessibility permission.
internal static class NativeInput
{
    internal static string WindowTitle { get; set; } = "DataGridView binding and editing";
    const string ObjC = "/usr/lib/libobjc.A.dylib";
    [DllImport(ObjC)] static extern nint objc_getClass(string name);
    [DllImport(ObjC)] static extern nint sel_registerName(string name);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern nint Send(nint obj,nint selector);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern nint Send(nint obj,nint selector,nint arg);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern byte ActionSend(nint obj,nint selector,nint action,nint target);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern nint StringSend(nint obj,nint selector,string arg);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern double Component(nint obj,nint selector);
    [DllImport(ObjC, EntryPoint="objc_msgSend")] static extern nint CellView(nint obj,nint selector,nint column,nint row,byte create);
    static nint Get(nint obj,string selector) => Send(obj,sel_registerName(selector));
    static nint Window
    {
        get
        {
            var windows = Get(Get(objc_getClass("NSApplication"),"sharedApplication"),"windows");
            for (int i=0; i<(int)Get(windows,"count"); i++)
            {
                var window = Send(windows,sel_registerName("objectAtIndex:"),i);
                if (Marshal.PtrToStringUTF8(Get(Get(window,"title"),"UTF8String")) == WindowTitle)
                    return window;
            }
            throw new InvalidOperationException("Missing test window");
        }
    }
    internal static void ReplaceText(string value)
    {
        nint editor = Get(Window,"firstResponder");
        if (Send(editor,sel_registerName("isKindOfClass:"),objc_getClass("NSTextView")) == 0)
            throw new InvalidOperationException("AppKit first responder: " + Marshal.PtrToStringUTF8(Get(Get(editor,"className"),"UTF8String")));
        Send(editor,sel_registerName("selectAll:"),0);
        var text = StringSend(objc_getClass("NSString"),sel_registerName("stringWithUTF8String:"),value);
        Send(editor,sel_registerName("insertText:"),text);
    }
    internal static void LeaveField() => Send(Window,sel_registerName("makeFirstResponder:"),0);
    internal static int AccessibilitySelectionCount(string attribute)
    {
        var table = FindTable(Get(Window,"contentView"));
        return (int)Get(Get(table,attribute),"count");
    }
    internal static System.Drawing.Color CellTextColor(int row,int column)
    {
        var table = FindTable(Get(Window,"contentView"));
        var field = CellView(table,sel_registerName("viewAtColumn:row:makeIfNecessary:"),column,row,1);
        var space = Get(objc_getClass("NSColorSpace"),"sRGBColorSpace");
        var color = Send(Get(field,"textColor"),sel_registerName("colorUsingColorSpace:"),space);
        int Channel(string name) => (int)Math.Round(Component(color,sel_registerName(name))*255);
        return System.Drawing.Color.FromArgb(Channel("redComponent"),Channel("greenComponent"),Channel("blueComponent"));
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public double X, Y; }
    [DllImport(ObjC,EntryPoint="objc_msgSend")]
    private static extern nint CreateKey(nint cls,nint sel,nint type,NativePoint position,nuint flags,double seconds,nint window,nint context,nint characters,nint unmodified,byte repeat,ushort keyCode);
    internal static void DeleteSelectedRow() => KeyDown("\u007f",51);
    internal static void MoveRight() => KeyDown("\uf703",124);
    static void KeyDown(string character, ushort key)
    {
        nint table = FindTable(Get(Window,"contentView"));
        var text = StringSend(objc_getClass("NSString"),sel_registerName("stringWithUTF8String:"),character);
        var ev = CreateKey(objc_getClass("NSEvent"),sel_registerName("keyEventWithType:location:modifierFlags:timestamp:windowNumber:context:characters:charactersIgnoringModifiers:isARepeat:keyCode:"),10,default,0,0,Get(Window,"windowNumber"),0,text,text,0,key);
        Send(table,sel_registerName("keyDown:"),ev);
    }
    internal static void ToggleCheckbox(int row,int col)
    {
        var table = FindTable(Get(Window,"contentView"));
        if (table == 0) throw new InvalidOperationException("AppKit table missing");
        var box = CellView(table,sel_registerName("viewAtColumn:row:makeIfNecessary:"),col,row,1);
        if (Send(box,sel_registerName("isKindOfClass:"),objc_getClass("NSButton")) == 0)
            throw new InvalidOperationException("AppKit checkbox missing");
        Send(box,sel_registerName("performClick:"),0);
    }
    internal static void SelectChoice(int row,int col,int index)
    {
        var table = FindTable(Get(Window,"contentView"));
        var box = CellView(table,sel_registerName("viewAtColumn:row:makeIfNecessary:"),col,row,1);
        if (Send(box,sel_registerName("isKindOfClass:"),objc_getClass("NSPopUpButton")) == 0)
            throw new InvalidOperationException("Expected a native pop-up in the table cell");
        Send(box,sel_registerName("selectItemAtIndex:"),index);
        if (ActionSend(box,sel_registerName("sendAction:to:"),Get(box,"action"),Get(box,"target"))==0)
            throw new InvalidOperationException("Native combo action failed");
    }
    static nint FindTable(nint view)
    {
        if (Send(view,sel_registerName("isKindOfClass:"),objc_getClass("NSTableView")) != 0) return view;
        var children = Get(view,"subviews");
        int count = (int)Get(children,"count");
        for (int i = 0; i < count; i++)
        {
            var found = FindTable(Send(children,sel_registerName("objectAtIndex:"),i));
            if (found != 0) return found;
        }
        return 0;
    }
}
