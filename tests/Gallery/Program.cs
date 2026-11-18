using System.ComponentModel;
using System.Text.Json;

namespace Gallery;

public sealed class Track
{
    public string Title { get; set; } = "";
    public int Rating { get; set; }
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 0 && (args.Length != 2 || args[0] != "--probe"))
        {
            Console.Error.WriteLine("Usage: Gallery [--probe output-directory]");
            return 2;
        }
        string? output = args.Length == 2 ? Path.GetFullPath(args[1]) : null;
        ApplicationConfiguration.Initialize();
        using var window = new Form { Text = "Record Library", ClientSize = new Size(750, 450),
            AutoScaleMode = AutoScaleMode.None, StartPosition = FormStartPosition.CenterScreen };
        var split = new SplitContainer { Dock = DockStyle.Fill, Size = window.ClientSize, SplitterDistance = 198 };
        window.Controls.Add(split);

        Add(split.Panel1, new Label { Text = "Library" }, 10, 10, 175, 22);
        var tree = Add(split.Panel1, new TreeView(), 10, 34, 176, 115);
        var collection = tree.Nodes.Add("Collection");
        collection.Nodes.Add("Albums");
        var playlists = collection.Nodes.Add("Playlists");
        Add(split.Panel1, new Label { Text = "Formats" }, 10, 163, 175, 20);
        var formats = Add(split.Panel1, new CheckedListBox { CheckOnClick = true }, 10, 187, 176, 85);
        formats.Items.AddRange(new object[] { "CD", "Digital", "Vinyl" });
        formats.SetItemChecked(0, true);
        Add(split.Panel1, new Label { Text = "Volume" }, 10, 295, 175, 20);
        var volume = Add(split.Panel1, new TrackBar { Maximum = 10, Value = 4 }, 10, 318, 176, 43);
        var repeat = Add(split.Panel1, new CheckBox { Text = "Repeat playlist" }, 10, 374, 176, 25);

        Add(split.Panel2, new Label { Text = "Playlist name" }, 10, 10, 270, 20);
        var title = Add(split.Panel2, new TextBox { Text = "Evening mix" }, 10, 33, 268, 23);
        var apply = Add(split.Panel2, new Button { Text = "Apply" }, 290, 31, 88, 27);
        Add(split.Panel2, new Label { Text = "Albums" }, 10, 73, 200, 20);
        var albums = Add(split.Panel2, new ListView { View = View.Details, FullRowSelect = true }, 10, 96, 515, 105);
        albums.Columns.Add("Album", 280);
        albums.Columns.Add("Format", 110);
        albums.Columns.Add("Year", 85);
        albums.Items.Add(new ListViewItem(new[] { "Kind of Blue", "Vinyl", "1959" }));
        albums.Items.Add(new ListViewItem(new[] { "Blue Train", "CD", "1958" }));
        albums.Items.Add(new ListViewItem(new[] { "Speak No Evil", "Digital", "1966" }));

        Add(split.Panel2, new Label { Text = "Tracks" }, 10, 214, 200, 20);
        var grid = Add(split.Panel2, new DataGridView { AllowUserToAddRows = false, RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill }, 10, 237, 515, 114);
        var tracks = new BindingList<Track> { new() { Title = "So What", Rating = 4 }, new() { Title = "Blue Train", Rating = 5 } };
        using var source = new BindingSource { DataSource = tracks };
        grid.DataSource = source;
        var progress = Add(split.Panel2, new ProgressBar { Maximum = 10, Value = 4 }, 10, 372, 265, 19);
        var status = Add(split.Panel2, new Label { Text = "Ready" }, 10, 407, 515, 24);

        int clicked = 0;
        apply.Click += (_, _) => { clicked++; status.Text = title.Text; progress.Value = volume.Value; };
        using var timer = new System.Windows.Forms.Timer { Interval = 350 };
        int result = 0;
        bool captured = false;
        window.Shown += (_, _) =>
        {
            tree.ExpandAll();
            tree.SelectedNode = playlists;
            if (output != null) timer.Start();
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            try
            {
                if (captured) { window.Close(); return; }
                repeat.Checked = true;
                formats.SetItemChecked(2, true);
                volume.Value = 7;
                albums.Items[1].Selected = true;
                tracks.Add(new Track { Title = "Footprints", Rating = 3 });
                apply.PerformClick();
                var state = new {
                    Client = new[] { window.ClientSize.Width, window.ClientSize.Height }, Split = split.SplitterDistance,
                    Folder = tree.SelectedNode?.FullPath, Formats = formats.CheckedItems.Cast<object>().Select(o => o.ToString()).ToArray(),
                    Repeat = repeat.Checked, Volume = volume.Value, Progress = progress.Value,
                    Album = albums.SelectedItems.Count == 1 ? albums.SelectedItems[0].Text : null,
                    Tracks = grid.Rows.Count, LastTrack = grid.Rows.Count > 2 ? grid.Rows[2].Cells["Title"].Value?.ToString() : null,
                    Clicks = clicked, Status = status.Text,
                    Bounds = new[] { split.Panel1, (Control)split.Panel2, tree, formats, title, apply, albums, grid, progress, status }
                        .Select(c => new { Type = c.GetType().Name, c.Left, c.Top, c.Width, c.Height }).ToArray()
                };
                Directory.CreateDirectory(output!);
                File.WriteAllText(Path.Combine(output!, "state.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
                if (state.Folder != @"Collection\Playlists" || state.Formats.Length != 2 || !state.Repeat || state.Progress != 7 ||
                    state.Album != "Blue Train" || state.Tracks != 3 || state.LastTrack != "Footprints" || state.Clicks != 1 || state.Status != "Evening mix")
                    throw new InvalidOperationException("Gallery behavior differed from the expected state. See state.json.");
#if WINDOWS
                // Capture only this form's own client, including when it runs on a private desktop.
                using var image = new Bitmap(window.Width, window.Height);
                window.DrawToBitmap(image, new Rectangle(Point.Empty, window.Size));
                Point origin = window.PointToScreen(Point.Empty) - (Size)window.Location;
                using var client = image.Clone(new Rectangle(origin, window.ClientSize), image.PixelFormat);
                client.Save(Path.Combine(output!, "gallery.png"), System.Drawing.Imaging.ImageFormat.Png);
#endif
                captured = true;
                // Give the external window-only macOS capture time to run.
                timer.Interval = OperatingSystem.IsMacOS() ? 20000 : 100;
                timer.Start();
            }
            catch (Exception e) { Console.Error.WriteLine(e); result = 1; window.Close(); }
        };
        Application.Run(window);
        return result;
    }

    private static T Add<T>(Control parent, T control, int x, int y, int width, int height) where T : Control
    {
        control.Bounds = new Rectangle(x, y, width, height);
        parent.Controls.Add(control);
        return control;
    }
}
