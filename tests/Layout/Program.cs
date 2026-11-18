using System.Runtime.InteropServices;
namespace LayoutFixture;
internal static class Program
{
    [DllImport("LayoutProbe")] private static extern int ClickTab(IntPtr h,int index,int selected);
    private static void Check(bool ok,string what) { if(!ok) throw new InvalidOperationException(what); }
    [STAThread]
    private static int Main()
    {
        string? path=Environment.GetEnvironmentVariable("MAC_WINFORM_LAYOUT_PROBE");
        bool probe=path!=null && File.Exists(path);
        if(probe) NativeLibrary.SetDllImportResolver(typeof(Program).Assembly,(name,_,_)=>name=="LayoutProbe"?NativeLibrary.Load(path!):0);
        ApplicationConfiguration.Initialize();
        using var form=new Form { Text="Layout reference",ClientSize=new(560,360),AutoScaleMode=AutoScaleMode.None };
        using var tabs=new TabControl { Bounds=new(0,0,550,120) };
        tabs.TabPages.Add("one","First"); tabs.TabPages.Add("two","Second"); tabs.TabPages.Add("three","Third");
        using var flow=new FlowLayoutPanel { Bounds=new(12,128,130,90),Padding=new Padding(0) };
        var kids=Enumerable.Range(0,3).Select(_=>new Panel{Size=new(40,18),Margin=new Padding(0)}).ToArray();
        flow.Controls.AddRange(kids);
        using var table=new TableLayoutPanel { Bounds=new(190,128,300,150),ColumnCount=3,RowCount=3,Padding=new Padding(0) };
        foreach(int w in new[]{70,90,140}) table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,w));
        foreach(int h in new[]{45,55,50}) table.RowStyles.Add(new RowStyle(SizeType.Absolute,h));
        var span=new Panel{Dock=DockStyle.Fill,Margin=new(0)};
        var first=new Panel{Dock=DockStyle.Fill,Margin=new(0)};
        var second=new Panel{Dock=DockStyle.Fill,Margin=new(0)};
        table.Controls.Add(span,0,0); table.SetColumnSpan(span,2); table.SetRowSpan(span,2);
        table.Controls.Add(first); table.Controls.Add(second);
        form.Controls.AddRange([tabs,flow,table]);
        int result=1;
        form.Shown+=(_,_)=>form.BeginInvoke((Action)(()=>
        {
            try
            {
                Check(tabs.SelectedIndex==0 && tabs.TabPages.ContainsKey("ONE"),"first selection and key");
                var sequence=new List<string>(); bool cancelLeaving=false,cancelEntering=false;
                tabs.Deselecting+=(_,e)=> { sequence.Add(e.Action.ToString()); e.Cancel=cancelLeaving; };
                tabs.Deselected+=(_,e)=>sequence.Add(e.Action.ToString());
                tabs.Selecting+=(_,e)=> { sequence.Add(e.Action.ToString()); e.Cancel=cancelEntering; };
                tabs.Selected+=(_,e)=>sequence.Add(e.Action.ToString());
                tabs.SelectedIndexChanged+=(_,_)=>sequence.Add("changed");
                tabs.SelectTab("two");
                Check(tabs.SelectedIndex==1 && string.Join(",",sequence)=="Deselecting,Deselected,Selecting,Selected,changed","selection event order");
                cancelLeaving=true; sequence.Clear(); tabs.SelectTab(2);
                Check(tabs.SelectedIndex==1 && string.Join(",",sequence)=="Deselecting","canceled departure");
                cancelLeaving=false; cancelEntering=true; sequence.Clear();
                if(probe) Check(ClickTab(tabs.Handle,2,1)!=0,"native selection canceled"); else tabs.SelectTab(2);
                Check(tabs.SelectedIndex==1 && string.Join(",",sequence)=="Deselecting,Deselected,Selecting","canceled arrival");
                cancelEntering=false; sequence.Clear(); if(probe) Check(ClickTab(tabs.Handle,2,2)!=0,"native header selected"); else tabs.SelectTab(2);
                Check(tabs.SelectedTab==tabs.TabPages["three"] && tabs.GetTabRect(2).Width>0,"named and native selected tab");
                tabs.TabPages.Insert(1,"middle","Inserted"); Check(tabs.TabPages.IndexOfKey("middle")==1,"insert");
                tabs.SelectTab("middle"); tabs.TabPages.RemoveByKey("one"); Check(tabs.TabCount==3 && tabs.TabPages[0].Name=="middle","remove by key");
                flow.FlowDirection=FlowDirection.LeftToRight; flow.SetFlowBreak(kids[0],true); flow.PerformLayout();
                Check(kids[1].Location==new Point(0,18) && kids[2].Location==new Point(40,18),"horizontal break");
                flow.FlowDirection=FlowDirection.RightToLeft; flow.PerformLayout();
                Check(kids[0].Left==90 && kids[1].Location==new Point(90,18) && kids[2].Left==50,"right to left");
                flow.FlowDirection=FlowDirection.TopDown; flow.PerformLayout();
                Check(kids[1].Location==new Point(40,0) && kids[2].Location==new Point(40,18),"vertical break");
                flow.FlowDirection=FlowDirection.BottomUp; flow.PerformLayout();
                Check(kids[0].Location==new Point(0,72) && kids[1].Location==new Point(40,72) && kids[2].Top==54,"bottom up");
                flow.WrapContents=false; flow.PerformLayout(); Check(kids[1].Left==0,"no wrap");
                table.PerformLayout();
                Check(span.Bounds==new Rectangle(0,0,160,100),"spanning fill bounds");
                Check(table.GetControlFromPosition(1,1)==span && table.GetColumnSpan(span)==2,"cell coverage");
                Check(first.Location==new Point(160,0) && second.Location==new Point(160,45),"automatic span avoidance");
                Check(table.GetPositionFromControl(second)==new TableLayoutPanelCellPosition(2,1),"resolved cell");
                Check(table.GetColumnWidths().SequenceEqual(new[]{70,90,140}) && table.GetRowHeights().SequenceEqual(new[]{45,55,50}),"track sizes");
                Console.WriteLine("PASS: tab events, cancellation, all flow directions, breaks, and spanning cells");
                result=0;
            }
            catch(Exception e) { Console.Error.WriteLine(e); }
            finally { form.Close(); }
        }));
        Application.Run(form); return result;
    }
}
