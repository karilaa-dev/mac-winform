using System.Runtime.InteropServices;

namespace Widgets;

internal static class Program
{
    [DllImport("WidgetProbe")] private static extern int ProbeList(IntPtr handle,int stage);
    [DllImport("WidgetProbe")] private static extern int ProbeSpinnerLink(IntPtr handle,int stage);
    [DllImport("WidgetProbe")] private static extern int ProbeTree(IntPtr handle);
    [DllImport("WidgetProbe")] private static extern int ProbeMask(IntPtr handle);
    [DllImport("WidgetProbe")] private static extern int ProbeSplit(IntPtr handle,int start,int finish);
    private static void Require(bool ok,string condition)
    {
        if (!ok) throw new InvalidOperationException(condition);
    }

    [STAThread]
    static int Main()
    {
        bool probe = Environment.GetEnvironmentVariable("MAC_WINFORM_WIDGET_PROBE") is {} path && File.Exists(path);
        if (probe) NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (name,_,_) =>
            name == "WidgetProbe" ? NativeLibrary.Load(Environment.GetEnvironmentVariable("MAC_WINFORM_WIDGET_PROBE")!) : IntPtr.Zero);
        ApplicationConfiguration.Initialize();
        using var window = new Form { Text="WinForms list widgets",ClientSize=new(640,400) };
        var split = new SplitContainer { Size=window.ClientSize,Dock=DockStyle.Fill,SplitterDistance=200 };
        var checks = new CheckedListBox { Dock=DockStyle.Fill, CheckOnClick=true };
        var list = new ListView { Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,GridLines=true,CheckBoxes=true };
        var a = new ListViewItem(new[]{"First","A"}); var b = new ListViewItem(new[]{"Second","B"});
        list.Columns.Add("Name",190); list.Columns.Add("Letter",90);
        list.Items.AddRange(new[]{a,b});
        checks.Items.Add("First",true);
        checks.Items.Add("Second");
        checks.Items.Add("Maybe",CheckState.Indeterminate);
        split.Panel1.Controls.Add(checks); split.Panel2.Controls.Add(list); window.Controls.Add(split);
        int result=1;
        window.Shown += (_,_) => window.BeginInvoke((Action)(() =>
        {
            try
            {
                Require(checks.CheckedItems.Count == 2 && checks.CheckedIndices[1] == 2,"tri-state initialization");
                bool before = false;
                checks.ItemCheck += (_,e) => { before=checks.GetItemCheckState(e.Index) == e.CurrentValue; if (e.Index == 0) e.NewValue=e.CurrentValue; };
                checks.SetItemChecked(0,false);
                Require(before && checks.GetItemChecked(0),"cancel check in ItemCheck");
                checks.Items.RemoveAt(0);
                Require(checks.CheckedIndices.Count == 1 && checks.CheckedIndices[0] == 1,"indices after removal");
                if (probe)
                {
                    Require(ProbeList(checks.Handle,1) != 0,"row count, tri-state and native checkbox action");
                    Require(before && !checks.GetItemChecked(0),"native checkbox cancellation");
                    Require(ProbeList(list.Handle,2) != 0,"native detail columns, selection and checkbox action");
                    Require(b.Checked && b.Selected,"native list selection/check callback");
                    Require(ProbeSplit(split.Handle,202,244) != 0 && split.SplitterDistance == 242,"native splitter drag");
                }
                using var rich = new RichTextBox { Rtf=@"{\rtf1\ansi Plain {\b bold} text}" };
                split.Panel2.Controls.Add(rich);
                Require(rich.Text.Contains("Plain bold text"),"RTF decoding");
                rich.Select(6,4); Require(rich.SelectedText=="bold" && rich.SelectionFont!.Bold,"RTF bold selection");
                rich.SelectionColor=Color.Red;
                string fragment=rich.SelectedRtf;
                Require(fragment.Contains(@"\rtf") && fragment.Contains("bold"),"selected RTF");
                using var stream=new MemoryStream();
                rich.SaveFile(stream,RichTextBoxStreamType.RichText); stream.Position=0;
                rich.Clear(); rich.LoadFile(stream,RichTextBoxStreamType.RichText); rich.Select(6,4);
                Require(rich.SelectionColor.ToArgb()==Color.Red.ToArgb() && rich.SelectionFont!.Bold,"RTF stream formatting round trip");
                rich.SelectedText="new";
                Require(rich.Text.Contains("Plain new text"),"native selection replacement");
                using var masked=new MaskedTextBox("00-00") {TextMaskFormat=MaskFormat.ExcludePromptAndLiterals};
                split.Panel2.Controls.Add(masked);
                int rejected=0; masked.MaskInputRejected+=(_,_)=>rejected++;
                masked.Text="4512"; Require(masked.MaskFull && masked.Text=="4512","mask provider accepted valid data");
                masked.Select(0,2); masked.SelectedText="36"; Require(masked.Text=="3612","masked range replacement");
                masked.Clear(); Require(!masked.MaskCompleted,"required mask positions");
                if (probe)
                {
                    Require(ProbeMask(masked.Handle)!=0,"native masked text view");
                    Require(rejected==1 && masked.MaskCompleted && masked.Text=="1234","native masked input and rejection");
                }
                using var plain=new TextBox {Text="alpha beta"};
                split.Panel2.Controls.Add(plain); plain.Select(6,4); plain.SelectedText="test";
                Require(plain.Text=="alpha test","single-line field replacement");
                using var numeric=new NumericUpDown { DecimalPlaces=2,Minimum=-10,Increment=.25m,Value=1.5m };
                using var domain=new DomainUpDown { Sorted=true,Wrap=true };
                split.Panel2.Controls.Add(numeric); split.Panel2.Controls.Add(domain);
                domain.Items.AddRange(new[]{"Zulu","Alpha","Beta"}); domain.SelectedItem="Alpha";
                if (probe)
                {
                    Require(ProbeSpinnerLink(numeric.Handle,1)!=0 && numeric.Value==1.75m,"native numeric increment");
                    Require(ProbeSpinnerLink(domain.Handle,1)!=0 && domain.SelectedItem?.ToString()=="Zulu","native domain wrap");
                }
                numeric.Text="-2.50"; numeric.DownButton(); Require(numeric.Value==-2.75m,"numeric typed decimal");
                domain.SelectedItem="Alpha"; domain.DownButton();
                Require(domain.SelectedIndex==1 && domain.Text=="Beta","sorted domain step");
                using var link=new LinkLabel { Text="Open album or artist" };
                link.Links.Add(5,5,"album"); link.Links.Add(14,6,"artist"); split.Panel2.Controls.Add(link);
                string? activated=null;
                link.LinkClicked+=(_,e)=>activated=e.Link?.LinkData as string;
                ((IButtonControl)link).PerformClick(); Require(activated=="album","default link");
                if(probe)
                {
                    Require(ProbeSpinnerLink(link.Handle,2)!=0 && activated=="artist","second native attributed link");
                    activated=null;
                    Require(ProbeSpinnerLink(link.Handle,3)!=0 && activated=="artist","keyboard link activation");
                }
                using var legacyHost=new Panel { Size=new(300,100) };
                using var legacyPanel=new Panel { Dock=DockStyle.Left,Width=80 };
                using var legacy=new Splitter { Dock=DockStyle.Left,Width=5 };
                legacyHost.Controls.Add(legacy); legacyHost.Controls.Add(legacyPanel);
                split.Panel2.Controls.Add(legacyHost); legacyHost.PerformLayout();
                legacy.SplitPosition=110;
                Require(legacyPanel.Width==110 && legacy.Left==110,"classic splitter adjacent panel");
                if(probe) Require(ProbeSplit(legacy.Handle,1,31)!=0 && legacyPanel.Width==140,"classic native splitter drag");
                a.Selected=true; b.Selected=true;
                Require(list.SelectedIndices.Count==2 && list.SelectedItems[1]==b,"multiselect");
                list.MultiSelect=false;
                Require(list.SelectedIndices.Count==1,"single select");
                b.Selected=true;
                b.SubItems[1].Text="Edited";
                foreach (var mode in new[]{View.LargeIcon,View.SmallIcon,View.List,View.Tile})
                {
                    list.View=mode;
                    if (probe) Require(ProbeList(list.Handle,3)!=0,"native collection mode "+mode);
                    Require(list.SelectedItems[0]==b,"mode switch retains selection");
                }
                list.View=View.Details; list.Sorting=SortOrder.Descending;
                Require(list.Items[0]==b && list.SelectedIndices[0]==0,"selection after sorting");
                list.Items.Remove(b); Require(list.SelectedItems.Count==0 && b.ListView==null,"remove selected");
                split.Panel1Collapsed=true;
                Require(!split.Panel1.Visible && split.Panel2.Width==split.Width,"collapse");
                split.Panel1Collapsed=false; split.Orientation=Orientation.Horizontal; split.SplitterDistance=123;
                Require(split.Panel1.Height==123 && split.Panel2.Top==123+split.SplitterWidth,"horizontal layout");
                using var tree = new TreeView { Size = new(180,100) };
                split.Panel1.Controls.Add(tree);
                var root = tree.Nodes.Add("Collection");
                root.Nodes.Add("Albums");
                var playlists = root.Nodes.Add("Playlists");
                tree.ExpandAll();
                if (probe)
                {
                    Require(tree.Focus() && ProbeTree(tree.Handle) != 0,"native disclosure and keyboard navigation");
                    Require(tree.SelectedNode == playlists,"tree selection after Down arrow");
                }
                Console.WriteLine("PASS: lists, trees, splitters, RTF, masked editing, spinners and links"+(probe ? " with native AppKit input" : ""));
                result=0;
            }
            catch(Exception e) { Console.Error.WriteLine(e); }
            finally { window.Close(); }
        }));
        Application.Run(window);
        return result;
    }
}
