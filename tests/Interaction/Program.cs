using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Interaction;

internal static class Program
{
    private sealed class Entry { public string Title { get; set; } = ""; }
    [DllImport("InteractionProbe")] static extern void start_board();
    [DllImport("InteractionProbe")] static extern void finish_board();
    [DllImport("InteractionProbe")] static extern int has_external_formats(int count,string text);
    [DllImport("InteractionProbe")] static extern int external_binary(int kind);
    [DllImport("InteractionProbe")] static extern void external_files(string first,string second);
    [DllImport("InteractionProbe")] static extern void send_key(int code,string text,int modifiers);
    [DllImport("InteractionProbe")] static extern int simulate_drop(int phase,int x,int y);
    [DllImport("InteractionProbe")] static extern int click_toolbar(string name);
    [DllImport("InteractionProbe")] static extern int move_thumb(int horizontal,double fraction);

    [STAThread]
    static int Main()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Console.WriteLine("This test drives the macOS AppKit integration.");
            return 0;
        }
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "Mac WinForms interaction probe", ClientSize = new(480,250), KeyPreview = true };
        var entry = new TextBox { Location = new(12,12) };
        var second = new TextBox { Location = new(140,12) };
        var drop = new Panel { Bounds = new(20,60,180,120), AllowDrop = true };
        var child = new Label { Text = "Drop target", Bounds = new(10,10,100,30) };
        drop.Controls.Add(child);
        drop.CreateControl(); // Create the enabled drop target before attaching its parent window.
        form.Controls.AddRange(new Control[]{entry,second,drop});
        var errors = new List<string>();
        void Check(bool condition,string message) { if (!condition) errors.Add(message); }
        form.Shown += async (_,_) =>
        {
            await Task.Delay(100);
            start_board();
            try
            {
                List<string> keys = new();
                form.KeyDown += (_,e) => keys.Add("form:"+e.KeyCode);
                entry.KeyDown += (_,e) => { keys.Add("input:"+e.KeyCode); if (e.KeyCode==Keys.D) e.SuppressKeyPress=true; };
                entry.KeyUp += (_,e) => keys.Add("up:"+e.KeyCode);
                entry.KeyPress += (_,e) => { keys.Add("char:"+e.KeyChar); if (e.KeyChar=='a') e.KeyChar='Z'; if (e.KeyChar=='b') e.Handled=true; };
                Check(entry.Focus() && entry.Focused && form.ContainsFocus,"native text focus");
                send_key(0,"a",0);
                send_key(11,"b",0);
                send_key(2,"d",0);
                Check(entry.Text=="Z","typing replacement and handled input, actual text: "+entry.Text);
                Check(keys.IndexOf("form:A") >= 0 && keys.IndexOf("form:A") < keys.IndexOf("input:A"),"preview order: "+string.Join(",",keys));
                Check(keys.Contains("up:A") && !keys.Contains("char:d") && keys.Contains("up:D"),"suppressed character and independent key-up: "+string.Join(",",keys));
                var shortcut = new ToolStripMenuItem("Do") { ShortcutKeys = Keys.Control | Keys.K };
                bool invoked = false;
                shortcut.Click += (_,_) => invoked = true;
                using var menu = new MenuStrip();
                menu.Items.Add(shortcut);
                form.MainMenuStrip = menu;
                send_key(40,"k",2);
                Check(invoked && !entry.Text.Contains('k'),"menu shortcut dispatch");
                invoked = false;
                send_key(40,"л",2);
                Check(invoked && !entry.Text.Contains('л'),"menu shortcut with a Cyrillic input source");

                bool valid = false;
                entry.Validating += (_,e) => e.Cancel = !valid;
                Check(!second.Focus() && entry.Focused && !second.Focused,"validation blocks native focus");
                valid = true;
                Check(second.Focus() && second.Focused && !entry.Focused,"focus changes after validation");

                const string alpha = "/private/tmp/Mac forms probe α.txt";
                const string beta = "/private/tmp/Mac forms probe β.txt";
                var paths = new StringCollection { alpha,beta };
                var data = new DataObject();
                data.SetText("Local 日本語");
                data.SetText("<b>rich text</b>",TextDataFormat.Html);
                data.SetFileDropList(paths);
                data.SetData("Test bytes",false,new byte[]{0,17,255});
                Clipboard.SetDataObject(data,true);
                Check(has_external_formats(2,"Local 日本語")!=0,"standard native file and string representations");
                Check(Clipboard.GetText()=="Local 日本語" && Clipboard.GetText(TextDataFormat.Html)=="<b>rich text</b>","rich text roundtrip");
                Check(Clipboard.GetData("Test bytes") is byte[] bytes && bytes.SequenceEqual(new byte[]{0,17,255}),"custom byte format");
                Check(Clipboard.GetFileDropList().Cast<string>().SequenceEqual(paths.Cast<string>()),"WinForms file list roundtrip");
                byte[] audio = {82,73,70,70,4,0,0,0,87,65,86,69};
                using var originalStream = new MemoryStream(audio);
                originalStream.Position = 2;
                Clipboard.SetAudio(originalStream);
                using var receivedAudio = Clipboard.GetAudioStream();
                Check(Clipboard.ContainsAudio() && receivedAudio?.ReadByte()==audio[0] && external_binary(1)==1 && originalStream.Position==2,"WAV data and stream position");
                using var png = new MemoryStream(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg=="));
                using var pixel = Image.FromStream(png);
                Clipboard.SetImage(pixel);
                using var receivedImage = Clipboard.GetImage();
                Check(receivedImage?.Size==new Size(1,1) && external_binary(0)==1,"PNG image roundtrip");
                external_files(alpha,beta);
                Check(Clipboard.ContainsFileDropList() && Clipboard.GetFileDropList().Count==2 && !Clipboard.ContainsText(),"native file URLs from another app");
                List<string> dragEvents = new();
                void TakeDrop(DragEventArgs e)
                {
                    var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
                    Check(files?.SequenceEqual(paths.Cast<string>())==true && e.AllowedEffect==DragDropEffects.Copy,"decoded native drag payload and effects");
                    e.Effect=DragDropEffects.Copy;
                }
                drop.DragEnter += (_,e) => { dragEvents.Add("enter"); TakeDrop(e); };
                drop.DragOver += (_,e) => { dragEvents.Add("over"); TakeDrop(e); };
                drop.DragLeave += (_,_) => dragEvents.Add("leave");
                drop.DragDrop += (_,e) => { dragEvents.Add("drop"); TakeDrop(e); };
                Check(simulate_drop(1,40,80)==1,"file drag enters through child");
                Check(simulate_drop(2,43,83)==1,"file drag over");
                simulate_drop(3,43,83);
                Check(simulate_drop(1,40,80)==1 && simulate_drop(4,43,83)==1,"file drop accepted");
                Check(dragEvents.SequenceEqual(new[]{"enter","over","leave","enter","drop"}),"drag event ordering: "+string.Join(",",dragEvents));
                drop.AllowDrop=false;
                Check(simulate_drop(1,40,80)==0,"disabled drop target");
                Clipboard.Clear();
                Check(Clipboard.GetDataObject()?.GetFormats().Length==0,"cleared private board");

                using var rows = new BindingSource(new BindingList<Entry>{new(),new(),new()},"");
                using var navigator = new BindingNavigator(rows) { Dock = DockStyle.Bottom };
                form.Controls.Add(navigator);
                Check(navigator.Items.Count==11 && navigator.PositionItem!.Text=="1" && navigator.CountItem!.Text=="of 3","navigator initial state");
                Check(click_toolbar("Move next")==1 && rows.Position==1,"native navigator button advances currency");
                Check(click_toolbar("Add new")==1 && rows.Count==4 && navigator.PositionItem!.Text=="4","native navigator add");
                Check(click_toolbar("Delete")==1 && rows.Count==3 && rows.Position==2,"native navigator delete");
                var number = ((ToolStripTextBox)navigator.PositionItem!).TextBox;
                Check(number.Focus(),"navigator position field focus");
                number.Text="2";
                send_key(36,"\r",0);
                Check(rows.Position==1 && number.Text=="2","native Return accepts position");
                number.Text="invalid";
                send_key(53,"\u001b",0);
                Check(number.Text=="2","native Escape restores position");
                using var horizontal = new HScrollBar { Bounds=new(220,65,125,17), Minimum=-10, Maximum=100, LargeChange=11, SmallChange=2 };
                using var vertical = new VScrollBar { Bounds=new(362,65,17,100), LargeChange=1 };
                form.Controls.AddRange(new Control[]{horizontal,vertical});
                horizontal.Cursor=Cursors.Hand;
                Check(horizontal.Cursor.Handle!=IntPtr.Zero && horizontal.Cursor.Size.Width>0 && horizontal.Cursor.HotSpot.X>=0,"AppKit system cursor");
                var scrolls = new List<ScrollEventType>();
                horizontal.Scroll += (_,e) => {
                    Check(e.ScrollOrientation==ScrollOrientation.HorizontalScroll,"horizontal orientation");
                    scrolls.Add(e.Type);
                    if (e.Type==ScrollEventType.ThumbTrack) e.NewValue+=2;
                };
                Check(move_thumb(1,0.5)==1 && horizontal.Value==42,"native horizontal thumb and mutable Scroll.NewValue");
                Check(move_thumb(0,0.33)==1 && vertical.Value==33,"native vertical thumb");
                Check(horizontal.Focus(),"scrollbar focus");
                send_key(124,"\uf703",0);
                Check(horizontal.Value==44 && scrolls.TakeLast(2).SequenceEqual(new[]{ScrollEventType.SmallIncrement,ScrollEventType.EndScroll}),"arrow key + end scroll");
                send_key(119,"\uf72b",0);
                Check(horizontal.Value==90,"user scroll respects last page");
                horizontal.Value=100;
                Check(horizontal.Value==100,"programmatic scrollbar maximum");
            }
            catch (Exception e) { errors.Add(e.ToString()); }
            finally { finish_board(); form.Close(); }
        };
        Application.Run(form);
        if (errors.Count > 0) { Console.Error.WriteLine(string.Join(Environment.NewLine,errors)); return 1; }
        Console.WriteLine("PASS: native keyboard, shortcuts, validation, pasteboard, drag/drop, binding navigator, scrollbars");
        return 0;
    }
}
