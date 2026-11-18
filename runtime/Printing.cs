using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace System.Drawing.Printing
{
    internal static class PrintNative
    {
        private const string Lib = "MacForms";
        [DllImport(Lib)] internal static extern PrintJobHandle mf_print_job();
        [DllImport(Lib)] internal static extern void mf_print_release(IntPtr job);
        [DllImport(Lib)] internal static extern int mf_print_begin_page(PrintJobHandle job,int width,int height,int left,int top,int antialias);
        [DllImport(Lib)] internal static extern void mf_print_end_page(PrintJobHandle job);
        [DllImport(Lib)] internal static extern IntPtr mf_print_page_bytes(PrintJobHandle job,int index,out int length);
        [DllImport(Lib)] internal static extern int mf_print_save(PrintJobHandle job,string path);
        [DllImport(Lib)] internal static extern int mf_print_submit(PrintJobHandle job,string settings,string title);
        [DllImport(Lib)] internal static extern IntPtr mf_print_panel(IntPtr ownerView,string settings,int allowRange,int allowSelection);
        [DllImport(Lib)] internal static extern void mf_free(IntPtr pointer);
    }
    internal sealed class PrintJobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PrintJobHandle() : base(true) {}
        protected override bool ReleaseHandle() { PrintNative.mf_print_release(handle); return true; }
    }

    public enum PrintAction { PrintToFile, PrintToPreview, PrintToPrinter }
    public enum PrintRange { AllPages, Selection, SomePages, CurrentPage = 4194304 }
    public enum PaperKind { Custom, Letter, LetterSmall, Tabloid, Ledger, Legal, Statement, Executive, A3, A4 }

    public class Margins : ICloneable
    {
        private int left, right, top, bottom;
        public Margins() : this(100,100,100,100) {}
        public Margins(int left,int right,int top,int bottom) { Left=left; Right=right; Top=top; Bottom=bottom; }
        public int Left { get => left; set { ArgumentOutOfRangeException.ThrowIfNegative(value); left=value; } }
        public int Right { get => right; set { ArgumentOutOfRangeException.ThrowIfNegative(value); right=value; } }
        public int Top { get => top; set { ArgumentOutOfRangeException.ThrowIfNegative(value); top=value; } }
        public int Bottom { get => bottom; set { ArgumentOutOfRangeException.ThrowIfNegative(value); bottom=value; } }
        public object Clone() => MemberwiseClone();
        public override bool Equals(object? other) => other is Margins m && (Left,Right,Top,Bottom)==(m.Left,m.Right,m.Top,m.Bottom);
        public override int GetHashCode() => HashCode.Combine(Left,Right,Top,Bottom);
        public static bool operator ==(Margins? a,Margins? b) => Equals(a,b);
        public static bool operator !=(Margins? a,Margins? b) => !Equals(a,b);
    }
    public class PaperSize
    {
        public PaperSize() : this("",0,0) {}
        public PaperSize(string paperName,int width,int height) { PaperName=paperName; Width=width; Height=height; }
        public string PaperName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int RawKind { get; set; }
        public PaperKind Kind => (PaperKind)RawKind;
        internal PaperSize Copy() => (PaperSize)MemberwiseClone();
    }
    public class PageSettings : ICloneable
    {
        public PageSettings() : this(new PrinterSettings()) {}
        public PageSettings(PrinterSettings printerSettings) => PrinterSettings=printerSettings;
        public PrinterSettings PrinterSettings { get; set; }
        public bool Landscape { get; set; }
        public Margins Margins { get; set; } = new();
        public PaperSize PaperSize { get; set; } = new("Letter",850,1100) { RawKind=(int)PaperKind.Letter };
        public Rectangle Bounds => new(0,0,Landscape ? PaperSize.Height : PaperSize.Width,Landscape ? PaperSize.Width : PaperSize.Height);
        public object Clone() => new PageSettings(PrinterSettings) { Landscape=Landscape, Margins=(Margins)Margins.Clone(), PaperSize=PaperSize.Copy() };
    }
    public class PrinterSettings : ICloneable
    {
        private PageSettings? pageSettings;
        private short copies=1;
        public short Copies { get => copies; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); copies=value; } }
        public bool Collate { get; set; }
        public string PrinterName { get; set; } = "";
        public bool PrintToFile { get; set; }
        public string PrintFileName { get; set; } = "";
        public int FromPage { get; set; }
        public int ToPage { get; set; }
        public int MinimumPage { get; set; }
        public int MaximumPage { get; set; } = 9999;
        public PrintRange PrintRange { get; set; }
        public PageSettings DefaultPageSettings => pageSettings ??= new PageSettings(this);
        public object Clone()
        {
            var copy=(PrinterSettings)MemberwiseClone();
            copy.pageSettings=(PageSettings)DefaultPageSettings.Clone();
            copy.pageSettings.PrinterSettings=copy;
            return copy;
        }
        internal string Options(PageSettings page) => JsonSerializer.Serialize(new {
            PrinterName, Copies, Collate, Range=(int)PrintRange, FromPage, ToPage, page.Bounds.Width, page.Bounds.Height });
    }
    public class PrintEventArgs : CancelEventArgs
    {
        public PrintEventArgs() {}
        internal PrintEventArgs(PrintAction action) => PrintAction=action;
        public PrintAction PrintAction { get; }
    }
    public class QueryPageSettingsEventArgs : PrintEventArgs
    {
        public QueryPageSettingsEventArgs(PageSettings pageSettings) => PageSettings=pageSettings;
        public PageSettings PageSettings { get; set; }
    }
    public class PrintPageEventArgs : EventArgs
    {
        public PrintPageEventArgs(Graphics? graphics,Rectangle marginBounds,Rectangle pageBounds,PageSettings pageSettings)
        { Graphics=graphics; MarginBounds=marginBounds; PageBounds=pageBounds; PageSettings=pageSettings; }
        public Graphics? Graphics { get; internal set; }
        public Rectangle MarginBounds { get; }
        public Rectangle PageBounds { get; }
        public PageSettings PageSettings { get; }
        public bool Cancel { get; set; }
        public bool HasMorePages { get; set; }
    }
    public delegate void PrintEventHandler(object sender,PrintEventArgs e);
    public delegate void PrintPageEventHandler(object sender,PrintPageEventArgs e);
    public delegate void QueryPageSettingsEventHandler(object sender,QueryPageSettingsEventArgs e);

    public class PrintDocument : Component
    {
        private bool printing;
        private PrinterSettings printerSettings=new();
        private PageSettings? pageSettings;
        public PrinterSettings PrinterSettings
        {
            get => printerSettings;
            set { printerSettings=value; if (pageSettings!=null) pageSettings.PrinterSettings=value; }
        }
        public PageSettings DefaultPageSettings { get => pageSettings ?? printerSettings.DefaultPageSettings; set => pageSettings=value; }
        public PrintController PrintController { get; set; } = new StandardPrintController();
        public string DocumentName { get; set; } = "document";
        public bool OriginAtMargins { get; set; }
        public event PrintEventHandler? BeginPrint, EndPrint;
        public event PrintPageEventHandler? PrintPage;
        public event QueryPageSettingsEventHandler? QueryPageSettings;
        protected virtual void OnBeginPrint(PrintEventArgs e) => BeginPrint?.Invoke(this,e);
        protected virtual void OnEndPrint(PrintEventArgs e) => EndPrint?.Invoke(this,e);
        protected virtual void OnPrintPage(PrintPageEventArgs e) => PrintPage?.Invoke(this,e);
        protected virtual void OnQueryPageSettings(QueryPageSettingsEventArgs e) => QueryPageSettings?.Invoke(this,e);
        public void Print()
        {
            if (printing) throw new InvalidOperationException("A print job is already active for this document.");
            var controller=PrintController;
            var args=new PrintEventArgs(controller.IsPreview ? PrintAction.PrintToPreview :
                PrinterSettings.PrintToFile ? PrintAction.PrintToFile : PrintAction.PrintToPrinter);
            printing=true;
            try
            {
                OnBeginPrint(args);
                if (args.Cancel) { OnEndPrint(args); return; }
                controller.OnStartPrint(this,args);
                try
                {
                    var query=new QueryPageSettingsEventArgs((PageSettings)DefaultPageSettings.Clone());
                    while (!args.Cancel)
                    {
                        OnQueryPageSettings(query);
                        if (query.Cancel) { args.Cancel=true; break; }
                        var settings=query.PageSettings;
                        var bounds=settings.Bounds;
                        var m=settings.Margins;
                        var margins=new Rectangle(m.Left,m.Top,bounds.Width-m.Left-m.Right,bounds.Height-m.Top-m.Bottom);
                        var page=new PrintPageEventArgs(null,margins,bounds,settings);
                        page.Graphics=controller.OnStartPage(this,page);
                        try { OnPrintPage(page); }
                        finally
                        {
                            try { controller.OnEndPage(this,page); }
                            finally { page.Graphics?.Dispose(); }
                        }
                        if (page.Cancel) args.Cancel=true;
                        if (!page.HasMorePages) break;
                    }
                }
                catch { args.Cancel=true; throw; }
                finally
                {
                    try { OnEndPrint(args); }
                    catch { args.Cancel=true; throw; }
                    finally { controller.OnEndPrint(this,args); }
                }
            }
            finally { printing=false; }
        }
    }

    public abstract class PrintController
    {
        public virtual bool IsPreview => false;
        public virtual void OnStartPrint(PrintDocument document,PrintEventArgs e) {}
        public virtual Graphics? OnStartPage(PrintDocument document,PrintPageEventArgs e) => null;
        public virtual void OnEndPage(PrintDocument document,PrintPageEventArgs e) {}
        public virtual void OnEndPrint(PrintDocument document,PrintEventArgs e) {}
    }

    // Shared page renderer. Controllers own separate jobs so preview and output can be requested repeatedly.
    internal sealed class QuartzPages : IDisposable
    {
        internal PrintJobHandle Handle { get; } = PrintNative.mf_print_job();
        internal List<Size> Sizes { get; } = new();
        internal Graphics Begin(PrintDocument document,PrintPageEventArgs e,bool antialias)
        {
            var area=e.PageBounds;
            var offset=document.OriginAtMargins ? e.MarginBounds.Location : Point.Empty;
            if (PrintNative.mf_print_begin_page(Handle,area.Width,area.Height,offset.X,offset.Y,antialias?1:0)==0)
                throw new InvalidOperationException("Core Graphics could not create a PDF page with the requested paper size.");
            Sizes.Add(area.Size);
            return new Graphics(area,100);
        }
        internal void End() => PrintNative.mf_print_end_page(Handle);
        internal PreviewPageInfo Preview(int index)
        {
            var ptr=PrintNative.mf_print_page_bytes(Handle,index,out var size);
            var bytes=new byte[size];
            Marshal.Copy(ptr,bytes,0,size);
            using var stream=new MemoryStream(bytes,false);
            return new PreviewPageInfo(Image.FromStream(stream),Sizes[index]);
        }
        public void Dispose() => Handle.Dispose();
    }
    public class StandardPrintController : PrintController
    {
        private QuartzPages? job;
        public override void OnStartPrint(PrintDocument document,PrintEventArgs e) => job=new QuartzPages();
        public override Graphics OnStartPage(PrintDocument document,PrintPageEventArgs e) => job!.Begin(document,e,true);
        public override void OnEndPage(PrintDocument document,PrintPageEventArgs e) => job!.End();
        public override void OnEndPrint(PrintDocument document,PrintEventArgs e)
        {
            using var pages=job;
            job=null;
            if (e.Cancel || pages==null || pages.Sizes.Count==0) return;
            var settings=document.PrinterSettings;
            if (settings.PrintToFile)
            {
                if (string.IsNullOrWhiteSpace(settings.PrintFileName))
                    throw new InvalidOperationException("PrintFileName must specify a PDF path.");
                if (PrintNative.mf_print_save(pages.Handle,Path.GetFullPath(settings.PrintFileName))==0)
                    throw new IOException("Could not save print job to " + settings.PrintFileName);
            }
            else if (PrintNative.mf_print_submit(pages.Handle,settings.Options(document.DefaultPageSettings),document.DocumentName)==0)
                throw new InvalidOperationException("AppKit could not complete the print job. Check the selected printer.");
        }
    }
    public sealed class PreviewPageInfo(Image image,Size physicalSize)
    {
        public Image Image { get; } = image;
        public Size PhysicalSize { get; } = physicalSize;
    }
    public class PreviewPrintController : PrintController
    {
        private QuartzPages? job;
        private PreviewPageInfo[] pages=[];
        public bool UseAntiAlias { get; set; }
        public override bool IsPreview => true;
        public override void OnStartPrint(PrintDocument document,PrintEventArgs e) { pages=[]; job=new QuartzPages(); }
        public override Graphics OnStartPage(PrintDocument document,PrintPageEventArgs e) => job!.Begin(document,e,UseAntiAlias);
        public override void OnEndPage(PrintDocument document,PrintPageEventArgs e) => job!.End();
        public override void OnEndPrint(PrintDocument document,PrintEventArgs e)
        {
            using var rendered=job;
            job=null;
            if (!e.Cancel && rendered!=null)
                pages=Enumerable.Range(0,rendered.Sizes.Count).Select(rendered.Preview).ToArray();
        }
        public PreviewPageInfo[] GetPreviewPageInfo() => (PreviewPageInfo[])pages.Clone();
    }
}

namespace System.Windows.Forms
{
    using System.Drawing.Printing;

    public class PrintPreviewControl : Control
    {
        private PrintDocument? document;
        private PreviewPageInfo[]? pages;
        private int startPage, rows=1, columns=1;
        private double zoom=.3;
        private bool autoZoom=true;
        public PrintDocument? Document
        {
            get => document;
            set { document=value; startPage=0; InvalidatePreview(); }
        }
        public bool UseAntiAlias { get; set; }
        public bool AutoZoom { get => autoZoom; set { autoZoom=value; Invalidate(); } }
        public double Zoom { get => zoom; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); zoom=value; AutoZoom=false; } }
        public int Rows { get => rows; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); rows=value; Invalidate(); } }
        public int Columns { get => columns; set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); columns=value; Invalidate(); } }
        public int StartPage
        {
            get => startPage;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                int selected=pages==null ? value : Math.Min(value,Math.Max(0,pages.Length-1));
                if (startPage==selected) return;
                startPage=selected;
                OnStartPageChanged(EventArgs.Empty);
                Invalidate();
            }
        }
        public event EventHandler? StartPageChanged;
        protected virtual void OnStartPageChanged(EventArgs e) => StartPageChanged?.Invoke(this,e);
        private void ReleasePages()
        {
            if (pages!=null) foreach (var page in pages) page.Image.Dispose();
            pages=null;
        }
        public void InvalidatePreview() { ReleasePages(); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.DimGray);
            if (document!=null && pages==null)
            {
                var original=document.PrintController;
                var preview=new PreviewPrintController { UseAntiAlias=UseAntiAlias };
                document.PrintController=preview;
                try { document.Print(); pages=preview.GetPreviewPageInfo(); }
                finally { document.PrintController=original; }
                StartPage=Math.Min(startPage,Math.Max(0,pages.Length-1));
            }
            if (pages!=null)
            {
                int cellWidth=Math.Max(1,Width/columns), cellHeight=Math.Max(1,Height/rows);
                for (int i=0;i<rows*columns && startPage+i<pages.Length;i++)
                {
                    var page=pages[startPage+i];
                    var size=page.PhysicalSize;
                    double scale=autoZoom ? Math.Min((double)(cellWidth-16)/size.Width,(double)(cellHeight-16)/size.Height) : zoom*.96;
                    if (scale<=0) continue;
                    int width=(int)(size.Width*scale), height=(int)(size.Height*scale);
                    int x=(i%columns)*cellWidth+Math.Max(6,(cellWidth-width)/2);
                    int y=(i/columns)*cellHeight+Math.Max(6,(cellHeight-height)/2);
                    e.Graphics.FillRectangle(Brushes.White,x,y,width,height);
                    e.Graphics.DrawImage(page.Image,x,y,width,height);
                }
            }
            base.OnPaint(e);
        }
        protected override void Dispose(bool disposing) { if (disposing) ReleasePages(); base.Dispose(disposing); }
    }
    public sealed class PrintPreviewDialog : Form
    {
        public PrintPreviewDialog()
        {
            Text="Print preview";
            ClientSize=new Size(740,650);
            PrintPreviewControl=new PrintPreviewControl { Dock=DockStyle.Fill };
            var toolbar=new FlowLayoutPanel { Height=33, Dock=DockStyle.Top, WrapContents=false };
            var previous=new Button { Text="Previous" };
            var next=new Button { Text="Next" };
            var print=new Button { Text="Print…" };
            previous.Click+=(_,_) => PrintPreviewControl.StartPage=Math.Max(0,PrintPreviewControl.StartPage-1);
            next.Click+=(_,_) => PrintPreviewControl.StartPage++;
            print.Click+=(_,_) =>
            {
                if (Document==null) return;
                using var dialog=new PrintDialog { Document=Document };
                if (dialog.ShowDialog(this)==DialogResult.OK) Document.Print();
            };
            toolbar.Controls.AddRange([previous,next,print]);
            Controls.AddRange([PrintPreviewControl,toolbar]);
        }
        public PrintPreviewControl PrintPreviewControl { get; }
        public PrintDocument? Document { get => PrintPreviewControl.Document; set => PrintPreviewControl.Document=value; }
        public bool UseAntiAlias { get => PrintPreviewControl.UseAntiAlias; set { PrintPreviewControl.UseAntiAlias=value; PrintPreviewControl.InvalidatePreview(); } }
    }
    public sealed class PrintDialog : CommonDialog
    {
        private PrinterSettings settings=new();
        public PrintDocument? Document { get; set; }
        public PrinterSettings PrinterSettings
        {
            get => Document?.PrinterSettings ?? settings;
            set { settings=value; if (Document!=null) Document.PrinterSettings=value; }
        }
        public bool AllowSomePages { get; set; }
        public bool AllowSelection { get; set; }
        public bool PrintToFile { get => PrinterSettings.PrintToFile; set => PrinterSettings.PrintToFile=value; }
        private sealed record Choice(string PrinterName,short Copies,bool Collate,int Range,int FromPage,int ToPage,int Width,int Height,string SavePath);
        public override void Reset() { Document=null; settings=new(); AllowSelection=false; AllowSomePages=false; }
        protected override bool RunDialog(IntPtr hwndOwner)
        {
            Application.Initialize();
            var p=PrinterSettings;
            var page=Document?.DefaultPageSettings ?? p.DefaultPageSettings;
            var ptr=PrintNative.mf_print_panel(OwnerView(hwndOwner),p.Options(page),AllowSomePages?1:0,AllowSelection?1:0);
            if (ptr==0) return false;
            try
            {
                var selected=JsonSerializer.Deserialize<Choice>(Marshal.PtrToStringUTF8(ptr)!)!;
                p.PrinterName=selected.PrinterName;
                p.Copies=selected.Copies;
                p.Collate=selected.Collate;
                p.FromPage=selected.FromPage;
                p.ToPage=selected.ToPage;
                p.PrintRange=(PrintRange)selected.Range;
                p.PrintToFile=!string.IsNullOrEmpty(selected.SavePath);
                p.PrintFileName=selected.SavePath;
                if (page.Bounds.Size!=new Size(selected.Width,selected.Height))
                {
                    page.Landscape=selected.Width>selected.Height;
                    page.PaperSize=new PaperSize("Selected paper",Math.Min(selected.Width,selected.Height),Math.Max(selected.Width,selected.Height));
                }
                return true;
            }
            finally { PrintNative.mf_free(ptr); }
        }
    }
}
