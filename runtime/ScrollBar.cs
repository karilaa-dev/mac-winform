using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public enum ScrollEventType { SmallDecrement, SmallIncrement, LargeDecrement, LargeIncrement, ThumbPosition, ThumbTrack, First, Last, EndScroll }
public enum ScrollOrientation { HorizontalScroll, VerticalScroll }
public delegate void ScrollEventHandler(object? sender, ScrollEventArgs e);
public class ScrollEventArgs(ScrollEventType type,int oldValue,int newValue,ScrollOrientation scroll) : EventArgs
{
    public ScrollEventArgs(ScrollEventType type,int newValue) : this(type,-1,newValue,ScrollOrientation.HorizontalScroll) {}
    public ScrollEventArgs(ScrollEventType type,int newValue,ScrollOrientation scroll) : this(type,-1,newValue,scroll) {}
    public ScrollEventArgs(ScrollEventType type,int oldValue,int newValue) : this(type,oldValue,newValue,ScrollOrientation.HorizontalScroll) {}
    public ScrollEventType Type { get; } = type;
    public int OldValue { get; } = oldValue;
    public int NewValue { get; set; } = newValue;
    public ScrollOrientation ScrollOrientation { get; } = scroll;
}

public abstract class ScrollBar : Control
{
    [DllImport("MacForms")] private static extern void mf_scrollbar_set(IntPtr view,double position,double proportion);
    [DllImport("MacForms")] private static extern double mf_scrollbar_position(IntPtr view);
    private int minimum, maximum = 100, position, large = 10, small = 1;
    private long Range => (long)maximum - minimum + 1;
    // User scrolling stops one page before the upper limit. Programmatic Value can reach Maximum.
    private int LastPosition => (int)(maximum - (long)Math.Max(1,LargeChange) + 1);
    private ScrollOrientation Orientation => this is HScrollBar ? ScrollOrientation.HorizontalScroll : ScrollOrientation.VerticalScroll;
    public ScrollBar() { TabStop = false; Margin = new Padding(0); }
    public int Maximum { get => maximum; set { maximum = value; minimum = Math.Min(minimum,value); ClampPosition(); } }
    public int Minimum { get => minimum; set { minimum = value; maximum = Math.Max(maximum,value); ClampPosition(); } }
    public int LargeChange
    {
        get => (int)Math.Min(large,Range);
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); large = value; Sync(); }
    }
    public int SmallChange
    {
        get => Math.Min(small,LargeChange);
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); small = value; }
    }
    public int Value
    {
        get => position;
        set
        {
            if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
            if (position == value) return;
            position = value;
            Sync();
            OnValueChanged(EventArgs.Empty);
        }
    }
    public bool ScaleScrollBarForDpiChange { get; set; } = true;
    public event EventHandler? ValueChanged;
    public event ScrollEventHandler? Scroll;
    protected virtual void OnValueChanged(EventArgs e) => ValueChanged?.Invoke(this,e);
    protected virtual void OnScroll(ScrollEventArgs se) => Scroll?.Invoke(this,se);
    private void ClampPosition()
    {
        int next = Math.Clamp(position,minimum,maximum);
        if (position == next) Sync();
        else Value = next;
    }
    private void Sync()
    {
        if (NativeHandle == 0) return;
        long travel = (long)LastPosition - minimum;
        double fraction = travel > 0 ? Math.Min(1,(position-(long)minimum)/(double)travel) : 0;
        mf_scrollbar_set(NativeHandle,fraction,Math.Max(1,LargeChange)/(double)Range);
    }
    internal override void ApplyNative() { base.ApplyNative(); Sync(); }
    internal override void OnNative(int ev)
    {
        if (ev is >= 100 and <= 108) ApplyScroll((ScrollEventType)(ev-100));
        else base.OnNative(ev);
    }
    private void ApplyScroll(ScrollEventType type)
    {
        long next = type switch
        {
            ScrollEventType.SmallDecrement => (long)position - SmallChange,
            ScrollEventType.SmallIncrement => (long)position + SmallChange,
            ScrollEventType.LargeDecrement => (long)position - LargeChange,
            ScrollEventType.LargeIncrement => (long)position + LargeChange,
            ScrollEventType.First => minimum,
            ScrollEventType.Last => LastPosition,
            ScrollEventType.ThumbPosition or ScrollEventType.ThumbTrack => minimum + (long)Math.Round(mf_scrollbar_position(NativeHandle) * ((long)LastPosition - minimum)),
            _ => position
        };
        int proposed = type == ScrollEventType.EndScroll ? position : (int)Math.Clamp(next,minimum,LastPosition);
        var e = new ScrollEventArgs(type,position,proposed,Orientation);
        OnScroll(e);
        if (position == e.NewValue) Sync();
        else Value = e.NewValue;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled) return;
        ScrollEventType? type = e.KeyCode switch
        {
            Keys.Left or Keys.Up => ScrollEventType.SmallDecrement,
            Keys.Right or Keys.Down => ScrollEventType.SmallIncrement,
            Keys.PageUp => ScrollEventType.LargeDecrement,
            Keys.PageDown => ScrollEventType.LargeIncrement,
            Keys.Home => ScrollEventType.First,
            Keys.End => ScrollEventType.Last,
            _ => null
        };
        if (!type.HasValue) return;
        ApplyScroll(type.Value);
        ApplyScroll(ScrollEventType.EndScroll);
        e.SuppressKeyPress = true;
    }
}
public class HScrollBar : ScrollBar
{
    internal override int Kind => 36;
    public HScrollBar() => Size = new(80,17);
}
public class VScrollBar : ScrollBar
{
    internal override int Kind => 37;
    public VScrollBar() => Size = new(17,80);
}
