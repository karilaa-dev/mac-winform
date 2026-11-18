using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StatusTest;
internal static class Program
{
    [DllImport("StatusProbe")] static extern int observe_item(IntPtr handle,string text);
    [DllImport("StatusProbe")] static extern int item_removed();
    [DllImport("StatusProbe")] static extern int item_mouse(int right,int clicks);
    [DllImport("StatusProbe")] static extern void select_own_menu(IntPtr menu);
    [DllImport("StatusProbe")] static extern int balloon_state(IntPtr handle,int click);
    [DllImport("MacForms")] static extern IntPtr mf_native_menu(IntPtr handle);
    static IntPtr Peer(NotifyIcon item) => (IntPtr)typeof(NotifyIcon).GetField("peer",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(item)!;
    static void Check(bool pass,string detail) { if (!pass) throw new InvalidOperationException(detail); }
    [STAThread]
    static int Main()
    {
        if (!OperatingSystem.IsMacOS()) return 0;
        ApplicationConfiguration.Initialize();
        using var form=new Form { Text="Isolated status item probe", ClientSize=new(220,100) };
        using var container=new Container();
        using var menu=new ContextMenuStrip();
        using var iconInput=Assembly.GetExecutingAssembly().GetManifestResourceStream("test.ico")!;
        using var icon=new Icon(iconInput);
        var item=new NotifyIcon(container) { Text="Mac WinForms status probe", Icon=icon, ContextMenuStrip=menu };
        int click=0,doubled=0,downs=0,ups=0,menuOpened=0,selected=0,shown=0,closed=0,balloonClick=0;
        item.Click+=(_,_)=>click++; item.DoubleClick+=(_,_)=>doubled++;
        item.MouseDown+=(_,_)=>downs++; item.MouseUp+=(_,_)=>ups++;
        menu.Opening+=(_,_)=>menuOpened++;
        menu.Items.Add("Choose this item",null,(_,_)=>selected++);
        item.BalloonTipShown+=(_,_)=>shown++; item.BalloonTipClosed+=(_,_)=>closed++; item.BalloonTipClicked+=(_,_)=>balloonClick++;
        Exception? error=null;
        form.Shown+=async (_,_)=>{
            try
            {
                item.Visible=true;
                Check(observe_item(Peer(item),item.Text)==31,"native status image and tooltip");
                item.Text="Status probe updated";
                Check(observe_item(Peer(item),item.Text)==31,"updated native tooltip");
                Check(item_mouse(0,1)!=0 && item_mouse(0,2)!=0 && click==1 && doubled==1 && downs==2 && ups==2,"single and double click routing");
                select_own_menu(mf_native_menu(menu.Handle));
                Check(item_mouse(1,1)!=0 && menuOpened==1 && selected==1,"native status context menu action");
                item.ShowBalloonTip(2000,"Status probe","Click this balloon",ToolTipIcon.Info);
                Check(balloon_state(Peer(item),1)!=0 && shown==1 && closed==1 && balloonClick==1,"native balloon click and close");
                item.BalloonTipTitle="Timed probe"; item.BalloonTipText="Closes by timer"; item.BalloonTipIcon=ToolTipIcon.Warning;
                item.ShowBalloonTip(100);
                await Task.Delay(400);
                Check(shown==2 && closed==2 && balloon_state(Peer(item),0)==0,"timer dismisses native balloon");
                item.Visible=false;
                Check(item_removed()!=0 && Peer(item)==0,"hide removes the status item");
                item.Visible=true;
                int fields=observe_item(Peer(item),item.Text);
                Check(fields==31,"show recreates the item, native field mask "+fields);
                container.Dispose();
                Check(item_removed()!=0 && Peer(item)==0,"owning container disposes the status item");
            }
            catch(Exception ex) { error=ex; }
            finally { item.Dispose(); form.Close(); }
        };
        Application.Run(form);
        if (error!=null) { Console.Error.WriteLine(error); return 1; }
        Console.WriteLine("PASS: AppKit menu-bar icon, tooltip, clicks, context menu, transient balloons, hide/show and disposal.");
        return 0;
    }
}
