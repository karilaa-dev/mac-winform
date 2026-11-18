using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Providers;
internal static class Program
{
    [DllImport("ProviderProbe")] private static extern void PrepareDialog(IntPtr owner,int kind,int confirm);
    [DllImport("ProviderProbe")] private static extern int PanelChecked();
    [DllImport("ProviderProbe")] private static extern int ProbeHint(IntPtr c,int stage);
    [DllImport("ProviderProbe")] private static extern int ProbeError(IntPtr c,string text,int dx,int dy);
    private static void Check(bool condition,string description)
    {
        if(!condition) throw new InvalidOperationException(description);
    }
    private sealed class Row(string error) : IDataErrorInfo
    {
        public string Name { get; set; }="Test";
        public string Error => error;
        public string this[string columnName] => columnName==nameof(Name) ? Error:"";
    }
    [STAThread]
    private static int Main()
    {
        string? path=Environment.GetEnvironmentVariable("MAC_WINFORM_PROVIDER_PROBE");
        bool probe=path!=null && File.Exists(path);
        if(probe) NativeLibrary.SetDllImportResolver(typeof(Program).Assembly,(name,_,_)=>name=="ProviderProbe"?NativeLibrary.Load(path!):0);
        ApplicationConfiguration.Initialize();
        using var form=new Form { Text="Error and tooltip providers",ClientSize=new(360,180) };
        using var box=new TextBox { Location=new(40,50),Size=new(160,23) };
        using var hints=new ToolTip { ShowAlways=true,InitialDelay=30,AutoPopDelay=1000,ToolTipTitle="Hint" };
        using var errors=new ErrorProvider(form) { BlinkStyle=ErrorBlinkStyle.NeverBlink };
        form.Controls.Add(box);
        hints.SetToolTip(box,"Type here");
        errors.SetIconPadding(box,4);
        errors.SetError(box,"A name is required");
        int result=1;
        form.Shown+=(_,_)=>form.BeginInvoke((Action)(()=>
        {
            try
            {
                using var ico=new MemoryStream(); errors.Icon.Save(ico); ico.Position=0;
                using var restored=new Icon(ico); errors.Icon=restored;
                Check(restored.Width==16 && restored.Height==16,"ICO stream roundtrip");
                Check(errors.HasErrors && errors.GetError(box)=="A name is required","validation error");
                if(probe)
                {
                    Check(ProbeError(box.Handle,"A name is required",box.Width+4,(box.Height-16)/2)!=0,"native error glyph and help");
                }
                errors.SetIconAlignment(box,ErrorIconAlignment.TopLeft);
                box.Left+=12;
                if(probe) Check(ProbeError(box.Handle,"A name is required",-20,0)!=0,"native glyph follows layout");
                box.Visible=false;
                if(probe) Check(ProbeError(box.Handle,"A name is required",-20,0)!=0,"hidden glyph");
                box.Visible=true;
                using var moved=new Panel { Bounds=new Rectangle(20,25,245,110) };
                form.Controls.Add(moved); moved.Controls.Add(box);
                if(probe) Check(ProbeError(box.Handle,"A name is required",-20,0)!=0,"glyph reattaches after reparent");
                form.Controls.Add(box); errors.Clear();
                if(probe) Check(ProbeError(box.Handle,"",0,0)!=0,"removed native glyph");
                using var rows=new BindingSource { DataSource=new BindingList<Row>{new("Missing"),new("")} };
                box.DataBindings.Add("Text",rows,"Name");
                errors.DataSource=rows;
                Check(errors.GetError(box)=="Missing","bound validation");
                rows.Position=1;
                Check(errors.GetError(box)=="","bound validation updates with currency");
                bool cancel=false; int popups=0;
                hints.Popup+=(_,e)=> { popups++; e.Cancel=cancel; e.ToolTipSize=new(160,45); };
                if(probe)
                {
                    Check(ProbeHint(box.Handle,0)!=0,"mouse enter tracking area");
                    var deadline=DateTime.UtcNow.AddSeconds(2);
                    while(popups==0 && DateTime.UtcNow<deadline) Application.DoEvents();
                    Check(popups==1 && ProbeHint(box.Handle,3)!=0,"hover displays a sized AppKit window");
                    Check(ProbeHint(box.Handle,1)!=0 && ProbeHint(box.Handle,2)!=0,"mouse exit hides hint");
                    cancel=true; hints.Show("cancel",box);
                    Check(popups==2 && ProbeHint(box.Handle,2)!=0,"cancellable popup");
                    cancel=false; hints.Show("Help",box,new Point(3,3)); Check(ProbeHint(box.Handle,3)!=0,"explicit hint");
                    hints.Hide(box); hints.RemoveAll(); Check(ProbeHint(box.Handle,0)==0,"unregistered tracking area");
                }
                using var colors=new ColorDialog { Color=Color.Blue,ShowHelp=true };
                using var fonts=new FontDialog { Font=new Font("Menlo",12),ShowHelp=true,ShowApply=true,ShowEffects=true };
                int help=0,applied=0;
                colors.HelpRequest+=(_,_)=>help++;
                fonts.HelpRequest+=(_,_)=>help++; fonts.Apply+=(_,_)=>applied++;
                if(probe)
                {
                    using var file=new OpenFileDialog();
                    PrepareDialog(form.Handle,2,0);
                    Check(file.ShowDialog(form)==DialogResult.Cancel && PanelChecked()==1,"owned file picker sheet");
                    PrepareDialog(form.Handle,0,1);
                    Check(colors.ShowDialog(form)==DialogResult.OK && PanelChecked()==1,"accepted owner-attached color panel");
                    Check(colors.Color.ToArgb()==Color.FromArgb(0x12,0x34,0x56).ToArgb() && colors.CustomColors[0]==0x563412,"picked color and palette");
                    colors.Color=Color.Aquamarine; PrepareDialog(IntPtr.Zero,0,0);
                    Check(colors.ShowDialog()==DialogResult.Cancel && PanelChecked()==1 && colors.Color==Color.Aquamarine,"cancel modal color panel");
                    PrepareDialog(form.Handle,1,1);
                    Check(fonts.ShowDialog(form)==DialogResult.OK && PanelChecked()==1,"accepted owner-attached font panel");
                    Check(fonts.Font.SizeInPoints==19 && fonts.Font.Name=="Menlo" && fonts.Font.Bold && fonts.Font.Underline,"native font face and effects");
                    Check(applied==1 && help==2,"native Apply and Help actions");
                    var old=fonts.Font; PrepareDialog(IntPtr.Zero,1,0);
                    Check(fonts.ShowDialog()==DialogResult.Cancel && ReferenceEquals(old,fonts.Font),"cancel modal font panel");
                }
                Console.WriteLine("PASS: native font/color dialogs, hover tips, bound error indicators, and ICO streams");
                result=0;
            }
            catch(Exception e) { Console.Error.WriteLine(e); }
            finally { form.Close(); }
        }));
        Application.Run(form); return result;
    }
}
