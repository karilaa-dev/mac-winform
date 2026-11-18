using System.Drawing;
using System.Windows.Forms;

namespace Desktop;
internal static class Program
{
    static void Expect(bool condition,string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var main = new Form { IsMdiContainer = true, ClientSize = new(700,500) };
        var bars = new ToolStripContainer { Dock = DockStyle.Top, Height = 60 };
        var top = new ToolStrip(new ToolStripButton("Refresh"));
        bars.TopToolStripPanel.Join(top);
        main.Controls.Add(bars);
        main.Show();
        using var first = new Form { MdiParent = main, Text = "Documents" };
        using var second = new Form { MdiParent = main, Text = "Preview" };
        var calendar = new MonthCalendar { MaxSelectionCount = 3 };
        var scroll = new Panel { AutoScroll = true, Dock = DockStyle.Fill, AutoScrollMinSize = new(800,800) };
        first.Controls.Add(scroll);
        scroll.Controls.Add(calendar);
        var browser = new WebBrowser { Dock = DockStyle.Fill };
        second.Controls.Add(browser);
        first.Show();
        second.Show();
        Expect(main.MdiChildren.Length == 2 && main.ActiveMdiChild == second,"MDI child activation");
        first.Activate();
        Expect(main.ActiveMdiChild == first,"switch to first MDI child");
        main.LayoutMdi(MdiLayout.TileVertical);
        Expect(first.Left < second.Left && first.Height > 200,"tiled child windows");
        Expect(bars.ContentPanel.Top == top.Height,"joined toolbar occupies one row");
        bars.TopToolStripPanelVisible = false;
        Expect(bars.ContentPanel.Top == 0,"hidden toolbar releases content area");
        calendar.SetSelectionRange(new(2025,3,1),new(2025,3,6));
        Expect(calendar.SelectionRange.End == new DateTime(2025,3,3),"calendar selection limit");
        scroll.AutoScrollPosition = new(18,35);
        Expect(scroll.AutoScrollPosition == new Point(-18,-35),"scroll position");
        var point = new Point(3,7);
        Expect(calendar.PointToClient(calendar.PointToScreen(point)) == point,"screen coordinate round trip");
        Expect(Screen.FromControl(main).WorkingArea.Width > 0,"attached screen");
        var axButton = new Button { Text = "Choose", AccessibleName = "Choose date", AccessibleDescription = "Select a day" };
        scroll.Controls.Add(axButton);
        bool pressed = false;
        axButton.Click += (_,_) => pressed = true;
        axButton.AccessibilityObject.DoDefaultAction();
        Expect(pressed && axButton.AccessibilityObject.Name == "Choose date" && axButton.AccessibilityObject.Role == AccessibleRole.PushButton,
            "accessible button identity and activation");
        var paint = new Panel { BackColor = Color.Magenta, Size = new(18,18) };
        scroll.Controls.Add(paint);
        using (var capture = new Bitmap(24,24))
        {
            paint.DrawToBitmap(capture,new Rectangle(3,3,18,18));
            Expect(capture.GetPixel(8,8).ToArgb() == Color.Magenta.ToArgb(),"DrawToBitmap native view");
        }
        bool loaded = false;
        browser.DocumentCompleted += (_,_) => loaded = true;
        browser.DocumentText = "<html><head><title>Offline Forms Probe</title></head><body>Ready</body></html>";
        var limit = DateTime.UtcNow.AddSeconds(20);
        while (!loaded && DateTime.UtcNow < limit) Application.DoEvents();
        Expect(loaded && browser.DocumentTitle == "Offline Forms Probe", "offline browser navigation");
        Expect(browser.DocumentText.Contains("Offline Forms Probe"),"read current WebKit DOM");
        bool cancelled = false;
        browser.Navigating += (_,e) => { if (e.Url?.AbsoluteUri == "about:blank#blocked") { e.Cancel = true; cancelled = true; } };
        browser.Navigate("about:blank#blocked");
        limit = DateTime.UtcNow.AddSeconds(3);
        while (!cancelled && DateTime.UtcNow < limit) Application.DoEvents();
        Expect(cancelled && browser.DocumentTitle == "Offline Forms Probe","cancelled navigation");
        bool veto = true;
        second.FormClosing += (_,e) => { if (e.CloseReason == CloseReason.MdiFormClosing) e.Cancel = veto; };
        main.Close();
        Expect(!main.IsDisposed && main.MdiChildren.Length == 2,"child can veto parent close");
        veto = false;
        main.Close();
        Expect(first.IsDisposed && second.IsDisposed,"parent closes both documents");
        using var context = new ApplicationContext(new Form { Text = "ApplicationContext" });
        context.MainForm!.Shown += (_,_) => context.MainForm!.BeginInvoke(new Action(() =>
        {
            Expect(Application.OpenForms.Count == 1 && Application.MessageLoop,"main form registered in message loop");
            context.MainForm.Close();
        }));
        Application.Run(context);
        Console.WriteLine("Desktop controls passed");
    }
}
