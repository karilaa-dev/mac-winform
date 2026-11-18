// Adapted from dotnet/winforms v8.0.20 (MIT). See notices/dotnet-winforms.txt.
using System.ComponentModel;
namespace System.Windows.Forms;


public sealed class SelectionRange
{
    private DateTime start = DateTime.MinValue.Date;

    private DateTime end = DateTime.MaxValue.Date;

    public SelectionRange()
    {
    }

    public SelectionRange(DateTime lower, DateTime upper)
    {
        //NOTE: simcooke: we explicitly DO NOT want to throw an exception here - just silently
        //                swap them around. This is because the win32 control can return non-
        //                normalized ranges.

        // We use lower.Date and upper.Date to remove any time component
        if (lower < upper)
        {
            start = lower.Date;
            end = upper.Date;
        }
        else
        {
            start = upper.Date;
            end = lower.Date;
        }
    }

    public SelectionRange(SelectionRange range)
    {
        start = range.start;
        end = range.end;
    }

    public DateTime End
    {
        get
        {
            return end;
        }
        set
        {
            end = value.Date;
        }
    }

    public DateTime Start
    {
        get
        {
            return start;
        }
        set
        {
            start = value.Date;
        }
    }

    public override string ToString()
    {
        return $"SelectionRange: Start: {start}, End: {end}";
    }
}


public class DateRangeEventArgs : EventArgs
{
    public DateRangeEventArgs(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    public DateTime Start { get; }

    public DateTime End { get; }
}


public delegate void DateRangeEventHandler(object? sender, DateRangeEventArgs e);


public class WebBrowserNavigatingEventArgs : CancelEventArgs
{
    public WebBrowserNavigatingEventArgs(Uri? url, string? targetFrameName)
    {
        Url = url;
        TargetFrameName = targetFrameName;
    }

    public Uri? Url { get; }

    public string? TargetFrameName { get; }
}


public delegate void WebBrowserNavigatingEventHandler(object? sender, WebBrowserNavigatingEventArgs e);


public class WebBrowserNavigatedEventArgs : EventArgs
{
    public WebBrowserNavigatedEventArgs(Uri? url)
    {
        Url = url;
    }

    public Uri? Url { get; }
}


public delegate void WebBrowserNavigatedEventHandler(object? sender, WebBrowserNavigatedEventArgs e);


public class WebBrowserDocumentCompletedEventArgs : EventArgs
{
    public WebBrowserDocumentCompletedEventArgs(Uri? url)
    {
        Url = url;
    }

    public Uri? Url { get; }
}


public delegate void WebBrowserDocumentCompletedEventHandler(object? sender, WebBrowserDocumentCompletedEventArgs e);


public enum WebBrowserReadyState
{
    Uninitialized = 0,
    Loading = 1,
    Loaded = 2,
    Interactive = 3,
    Complete = 4
}
