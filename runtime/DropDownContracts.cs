// Licensed to the .NET Foundation under the MIT license. See notices/dotnet-winforms.txt.
using System.ComponentModel;
namespace System.Windows.Forms;


public enum ToolStripDropDownCloseReason
{
    AppFocusChange,
    AppClicked,
    ItemClicked,
    Keyboard,
    CloseCalled
}



public delegate void ToolStripDropDownClosedEventHandler(object? sender, ToolStripDropDownClosedEventArgs e);



public class ToolStripDropDownClosedEventArgs : EventArgs
{
    public ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason reason)
    {
        CloseReason = reason;
    }

    public ToolStripDropDownCloseReason CloseReason { get; }
}
