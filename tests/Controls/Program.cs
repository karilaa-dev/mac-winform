namespace Controls;

// This project uses only public WinForms APIs. It also builds on Windows.
internal static class Program
{
    [STAThread]
    public static int Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "Mac WinForms control test", ClientSize = new Size(480, 320) };
        using var timer = new System.Windows.Forms.Timer { Interval = 120 };
        var tab = new TabControl { Dock = DockStyle.Fill };
        var page = new TabPage("Controls");
        var grid = new DataGridView { Dock = DockStyle.Top, Height = 120, ReadOnly = true };
        var canvas = new Panel { Dock = DockStyle.Fill };
        bool painted = false;
        canvas.Paint += (_, args) =>
        {
            args.Graphics.FillRectangle(Brushes.Blue, 2, 2, 32, 20);
            using var pen = new Pen(Color.Red);
            args.Graphics.DrawEllipse(pen, 10, 5, 12, 12);
            painted = true;
        };
        page.Controls.Add(canvas);
        page.Controls.Add(grid);
        tab.TabPages.Add(page);
        form.Controls.Add(tab);
        int result = 1;
        form.Shown += (_, _) =>
        {
            grid.DataSource = new[] { new { Name = "Album", Available = true } };
            timer.Start();
        };
        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            try
            {
                await Task.Delay(20);
                if (form.InvokeRequired || !painted || grid.CurrentRow?.Cells["Name"].Value?.ToString() != "Album")
                    throw new InvalidOperationException("Paint, async dispatch or grid binding failed.");
                using var modal = new Form();
                var accept = new Button { Text = "OK", DialogResult = DialogResult.OK };
                var input = new TextBox { Text = "editable", UseSystemPasswordChar = true };
                modal.Controls.Add(accept);
                modal.Controls.Add(input);
                modal.Shown += (_, _) => modal.BeginInvoke((Action)accept.PerformClick);
                if (modal.ShowDialog(form) != DialogResult.OK || !form.Enabled)
                    throw new InvalidOperationException("Dialog result or owner activation failed.");
                Console.WriteLine("PASS: custom paint, data binding, UI timer, async dispatch, modal dialog and owner activation");
                result = 0;
            }
            catch (Exception e) { Console.Error.WriteLine(e); }
            finally { form.Close(); }
        };
        Application.Run(form);
        return result;
    }
}
