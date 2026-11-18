using System.Runtime.InteropServices;
namespace System.Windows.Forms;

public class MonthCalendar : Control
{
    [DllImport("MacForms")] private static extern void mf_calendar(IntPtr h,double min,double max,double start,double end,int multiple);
    [DllImport("MacForms")] private static extern double mf_calendar_end(IntPtr h);
    private DateTime min = new(1753,1,1), max = new(9998,12,31), start = DateTime.Today,end = DateTime.Today;
    private int count = 7;
    internal override int Kind => 32;
    public MonthCalendar() { Size = new(225,162); }
    public DateTime MinDate { get => min; set { if (value > max) throw new ArgumentOutOfRangeException(nameof(value)); min = value.Date; SetSelectionRange(start < min ? min : start,end < min ? min : end); Sync(); } }
    public DateTime MaxDate { get => max; set { if (value < min) throw new ArgumentOutOfRangeException(nameof(value)); max = value.Date; SetSelectionRange(start > max ? max : start,end > max ? max : end); Sync(); } }
    public int MaxSelectionCount { get => count; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); count = value; SetSelectionRange(start,end); Sync(); } }
    public DateTime SelectionStart { get => start; set => SetSelectionRange(value,value > end ? value : end); }
    public DateTime SelectionEnd { get => end; set => SetSelectionRange(value < start ? value : start,value); }
    public SelectionRange SelectionRange { get => new(start,end); set => SetSelectionRange(value.Start,value.End); }
    public event DateRangeEventHandler? DateChanged, DateSelected;
    public void SetDate(DateTime date) => SetSelectionRange(date,date);
    public void SetSelectionRange(DateTime date1,DateTime date2)
    {
        var first = date1.Date; var last = date2.Date;
        if (first < min || first > max || last < min || last > max) throw new ArgumentOutOfRangeException(nameof(date1));
        if (last < first) last = first;
        if ((last-first).Days >= count) last = first.AddDays(count-1);
        if (start == first && end == last) return;
        start = first; end = last; Sync();
        DateChanged?.Invoke(this,new(start,end));
    }
    private static long Seconds(DateTime dt) => new DateTimeOffset(dt).ToUnixTimeSeconds();
    private void Sync() { if (NativeHandle != 0) mf_calendar(NativeHandle,Seconds(min),Seconds(max),Seconds(start),Seconds(end),count>1 ? 1 : 0); }
    internal override void ApplyNative() => Sync();
    internal override void OnNative(int ev)
    {
        if (ev == 7)
        {
            DateTime first = DateTimeOffset.FromUnixTimeSeconds(Native.mf_get_date_value(NativeHandle)).LocalDateTime;
            DateTime last = DateTimeOffset.FromUnixTimeSeconds((long)mf_calendar_end(NativeHandle)).LocalDateTime;
            SetSelectionRange(first,last); Sync(); DateSelected?.Invoke(this,new(start,end));
        }
        else base.OnNative(ev);
    }
}
