using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class Program
{
    [DllImport("MacForms")] private static extern nint mf_widget_view(nint handle);
    [DllImport("CaptionProbe")] private static extern int CaptionHeight(nint view);
    [DllImport("CaptionProbe")] private static extern int Buttons(nint view);
    [DllImport("CaptionProbe")] private static extern int WidthMatches(nint view);
    [DllImport("CaptionProbe")] private static extern int HasTitle(nint view,string text);
    [DllImport("CaptionProbe")] private static extern void ClickCaption(nint view,int command);
    [DllImport("CaptionProbe")] private static extern int HasDottedTabFocus(nint view);
    private static void Check(bool value,string name)
    {
        if (!value) throw new InvalidOperationException(name);
    }
    private static void WaitForState(Form form,FormWindowState expected)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (form.WindowState != expected && elapsed.ElapsedMilliseconds < 3000)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Check(form.WindowState==expected,$"Expected {expected} after the window animation");
    }

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { ClientSize = new Size(500,220), Text = "Original" };
        var group = new Panel { Dock = DockStyle.Fill };
        var later = new TextBox { TabIndex = 3, Top = 80 };
        var tree = new TreeView { TabIndex = 2, Height = 50 };
        group.Controls.AddRange(new Control[] { later, new Button { TabIndex = 0, Enabled = false },
            new TextBox { TabIndex = 1, Visible = false }, tree });
        form.Controls.Add(group);
        form.Show();
        Application.DoEvents();
        Check(tree.Focused && form.ActiveControl == tree,"Initial focus should follow TabIndex inside the panel");
        if (OperatingSystem.IsMacOS())
        {
            nint view = mf_widget_view(form.Handle);
            Check(CaptionHeight(view) is >=28 and <=32 && Buttons(view)==7,$"Caption size {CaptionHeight(view)} and native button mask {Buttons(view)}");
            form.Text = "Changed";
            Check(HasTitle(view,form.Text)==1,"Updated window title");
            form.ClientSize = new Size(320,180);
            Application.DoEvents();
            Check(WidthMatches(view)==1 && Buttons(view)==7,"Buttons follow resized window");
            ClickCaption(view,1);
            Check(form.WindowState==FormWindowState.Maximized,"Maximize click");
            ClickCaption(view,1);
            Check(form.WindowState==FormWindowState.Normal && form.ClientSize==new Size(320,180),"Restore click and dimensions");
            ClickCaption(view,0);
            WaitForState(form,FormWindowState.Minimized);
            form.WindowState = FormWindowState.Normal;
            WaitForState(form,FormWindowState.Normal);
            form.MinimizeBox = false;
            Check(Buttons(view)==6,"Disabled minimize");
            form.MaximizeBox = false;
            Check(Buttons(view)==4,"Close-only caption");
            form.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Check(CaptionHeight(view) is >=24 and <=28 && Buttons(view)==4,"Tool window caption");
            form.ControlBox = false;
            Check(Buttons(view)==0,"ControlBox=false");
            form.FormBorderStyle = FormBorderStyle.None;
            Check(CaptionHeight(view)==0 && form.ClientSize==new Size(320,180),"Borderless client size");
            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.ControlBox = true;
            Check(CaptionHeight(view) is >=28 and <=32 && Buttons(view)==4,"Restored caption");
            bool veto = true;
            form.FormClosing += (_,e) => e.Cancel = veto;
            ClickCaption(view,2);
            Check(!form.IsDisposed && form.Visible,"Close cancellation");
            veto = false;
            ClickCaption(view,2);
            Check(form.IsDisposed,"Caption close raises FormClosing");
        }
        using var tabsForm = new Form();
        var tabs = new TabControl();
        var firstPage = new TabPage("First");
        firstPage.Controls.Add(new TextBox());
        tabs.TabPages.Add(firstPage);
        tabsForm.Controls.Add(tabs);
        tabsForm.Show();
        Application.DoEvents();
        Check(tabs.Focused,"The initial tab stop is the selected tab header");
        if (OperatingSystem.IsMacOS())
            Check(HasDottedTabFocus(mf_widget_view(tabs.Handle))==1,"Visible alternating keyboard focus dots");
        using var dialog = new Form();
        var requested = new TextBox { TabIndex = 1 };
        dialog.Controls.AddRange(new Control[] { new Button { TabIndex = 0 }, requested });
        dialog.ActiveControl = requested;
        dialog.Show();
        Application.DoEvents();
        Check(requested.Focused,"Explicit ActiveControl before Show");
        using var spinnerForm = new Form();
        var number = new NumericUpDown { TabIndex = 0 };
        spinnerForm.Controls.AddRange(new Control[] { new TextBox { TabIndex = 1, Top = 40 }, number });
        spinnerForm.Show();
        Application.DoEvents();
        Check(number.ContainsFocus,"The initial tab stop can be a composite spinner");
        Console.WriteLine("PASS window captions, native button actions, client dimensions and initial keyboard focus");
    }
}
