namespace System.Drawing
{
    [Flags] public enum FontStyle { Regular = 0, Bold = 1, Italic = 2, Underline = 4, Strikeout = 8 }
    public enum ContentAlignment { TopLeft=1, TopCenter=2, TopRight=4, MiddleLeft=16, MiddleCenter=32, MiddleRight=64, BottomLeft=256, BottomCenter=512, BottomRight=1024 }
}
namespace System.Windows.Forms
{
    [Flags] public enum AnchorStyles { None=0, Top=1, Bottom=2, Left=4, Right=8 }
    public enum DockStyle { None, Top, Bottom, Left, Right, Fill }
    public enum AutoScaleMode { None, Font, Dpi, Inherit }
    public enum FormStartPosition { Manual, CenterScreen, WindowsDefaultLocation, WindowsDefaultBounds, CenterParent }
    public enum FormBorderStyle { None, FixedSingle, Fixed3D, FixedDialog, Sizable, FixedToolWindow, SizableToolWindow }
    public enum ComboBoxStyle { Simple, DropDown, DropDownList }
    public enum DialogResult { None, OK, Cancel, Abort, Retry, Ignore, Yes, No, TryAgain=10, Continue=11 }
    public enum MessageBoxButtons { OK, OKCancel, AbortRetryIgnore, YesNoCancel, YesNo, RetryCancel, CancelTryContinue }
    public enum MessageBoxIcon { None=0, Hand=16, Error=16, Stop=16, Question=32, Exclamation=48, Warning=48, Asterisk=64, Information=64 }
    public enum SizeType { AutoSize, Absolute, Percent }
    public enum HighDpiMode { DpiUnaware, SystemAware, PerMonitor, PerMonitorV2, DpiUnawareGdiScaled }
    public readonly record struct Padding(int Left,int Top,int Right,int Bottom)
    {
        public Padding(int all) : this(all, all, all, all) {}
    }
}
