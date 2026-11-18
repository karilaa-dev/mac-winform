using System.Drawing.Printing;

namespace Printing;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var document = new PrintDocument { DocumentName = "Two-page proof", OriginAtMargins = true };
        document.DefaultPageSettings.PaperSize = new PaperSize("Proof", 500, 700);
        document.DefaultPageSettings.Margins = new Margins(35, 40, 45, 50);
        int index = 0, completed = 0;
        document.BeginPrint += (_, _) => index = 0;
        document.QueryPageSettings += (_, e) => e.PageSettings.Landscape = index == 1;
        document.PrintPage += (_, e) =>
        {
            var expected = index == 0 ? new Size(500, 700) : new Size(700, 500);
            if (e.PageBounds.Size != expected || e.MarginBounds.Location != new Point(35, 45))
                throw new InvalidOperationException("Incorrect paper or margin bounds");
            index++;
            var graphics = e.Graphics ?? throw new InvalidOperationException("Missing page graphics");
            graphics.FillRectangle(Brushes.Blue, 0, 0, 60, 30);
            graphics.DrawString("Proof page " + index, new Font("Arial", 13), Brushes.Black, 10, 48);
            e.HasMorePages = index < 2;
        };
        document.EndPrint += (_, e) => { if (!e.Cancel) completed++; };

        var controller = new PreviewPrintController();
        document.PrintController = controller;
        document.Print();
        var pages = controller.GetPreviewPageInfo();
        if (completed != 1 || pages.Length != 2 || pages[1].PhysicalSize != new Size(700, 500))
            throw new InvalidOperationException("Preview pagination failed");
        foreach (var page in pages) page.Image.Dispose();

        if (args.Length == 2 && args[0] == "--pdf")
        {
            document.PrintController = new StandardPrintController();
            document.PrinterSettings.PrintToFile = true;
            document.PrinterSettings.PrintFileName = Path.GetFullPath(args[1]);
            document.Print();
            if (completed != 2 || !File.Exists(document.PrinterSettings.PrintFileName))
                throw new InvalidOperationException("PDF print-to-file failed");
        }
        using var dialog = new PrintPreviewDialog { Document = document, UseAntiAlias = true };
        using var timer = new System.Windows.Forms.Timer { Interval = 400 };
        int tick = 0;
        timer.Tick += (_, _) =>
        {
            if (++tick == 1) dialog.PrintPreviewControl.StartPage = 1;
            else { timer.Stop(); dialog.Close(); }
        };
        timer.Start();
        dialog.ShowDialog();
        if (index != 2 || dialog.PrintPreviewControl.StartPage != 1)
            throw new InvalidOperationException("Preview control did not render and navigate to page two");
        Console.WriteLine("PASS: begin, page query, two page orientations, margins, preview, navigation and end.");
        return 0;
    }
}
