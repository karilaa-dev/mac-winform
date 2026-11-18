using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Forms;

namespace Buttons;
internal static class Program
{
    private sealed class TestCommand : ICommand
    {
        public bool Allowed { get; set; } = true;
        public object? LastArgument { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter)=>Allowed;
        public void Execute(object? parameter)=>LastArgument=parameter;
        public void Notify()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
    }
    [DllImport("MacForms")] private static extern nint mf_widget_view(nint peer);
    [DllImport("ButtonProbe")] private static extern void SendOption(nint view,string code,int key);
    [DllImport("ButtonProbe")] private static extern void Press(nint view);
    [DllImport("ButtonProbe")] private static extern void DragSlider(nint view,int x1,int y1,int x2,int y2);
    [DllImport("ButtonProbe")] private static extern void ArrowRight(nint view);
    [DllImport("ButtonProbe")] private static extern int HasChoiceAccent(nint view);
    [DllImport("ButtonProbe")] private static extern int PixelReport(nint view,int after);
    [DllImport("ButtonProbe")] private static extern int TitleMatches(nint view,string text);
    private static void Check(bool value,string message) { if (!value) throw new InvalidOperationException(message); }

    [STAThread] private static int Main()
    {
        ApplicationConfiguration.Initialize();
        using var form=new Form { ClientSize=new Size(340,240),Text="Button and mnemonic test" };
        var label=new Label { Text="&Name",TabIndex=0 };
        var input=new TextBox { Top=28,TabIndex=1 };
        using var icon=new Bitmap(8,8);
        using (var g=Graphics.FromImage(icon)) g.Clear(Color.Red);
        using var images=new ImageList();
        images.ImageSize=new Size(8,8);
        images.Images.Add("red",icon);
        var save=new Button { Text="&Save",Top=60,Width=130,Height=38,TabIndex=2,FlatStyle=FlatStyle.Flat,
            BackColor=Color.Lime,ImageList=images,ImageKey="red",TextImageRelation=TextImageRelation.ImageBeforeText };
        save.FlatAppearance.BorderColor=Color.Blue;
        save.FlatAppearance.BorderSize=2;
        var box=new CheckBox { Text="&Choice",Top=104,ThreeState=true,AutoCheck=true,TabIndex=3 };
        var r1=new RadioButton { Text="A",Top=138,TabIndex=4,Checked=true };
        var r2=new RadioButton { Text="B",Top=168,TabIndex=5 };
        var slider=new TrackBar { Left=130,Top=60,Size=new Size(176,43),Value=7,TabIndex=6 };
        var vertical=new TrackBar { Left=260,Top=110,Orientation=Orientation.Vertical,Size=new Size(43,120),TabIndex=7 };
        form.Controls.AddRange(new Control[] { label,input,save,box,r1,r2,slider,vertical });
        var buttonTypes=new ButtonBase[] { save,box,r1 };
        int count=0,changes=0;
        save.Click+=(_,_)=>count++;
        box.CheckStateChanged+=(_,_)=>changes++;
        Exception? failure=null;
        form.Shown+=async (_,_)=>{
            try
            {
                await Task.Delay(80);
                Check(buttonTypes.Length==3 && typeof(RadioButton).BaseType==typeof(ButtonBase),"stock ButtonBase hierarchy");
                Check(save.Image!=null && save.ImageIndex==-1 && save.ImageKey=="red","keyed image list");
                Check(Control.IsMnemonic('x',"E&xit") && !Control.IsMnemonic('x',"E&&xit"),"escaped mnemonic");
                if (OperatingSystem.IsMacOS())
                {
                    nint button=mf_widget_view(save.Handle), checkbox=mf_widget_view(box.Handle);
                    Check(PixelReport(button,0)==1,"flat fill, border and image before text in AppKit capture");
                    save.TextImageRelation=TextImageRelation.TextBeforeImage;
                    Check(PixelReport(button,1)==1,"image after caption");
                    input.Focus();
                    SendOption(button,"s",1);
                    Check(count==1,$"Option-S from the native window (clicks {count}, focus {form.ActiveControl?.Text})");
                    save.UseMnemonic=false;
                    Check(TitleMatches(button,"&Save")==1,"literal caption");
                    SendOption(button,"s",1);
                    Check(count==1,"mnemonic disabled");
                    save.Focus(); SendOption(button,"n",45);
                    Check(input.Focused,"Option-N label focuses next input");
                    SendOption(button,"c",8);
                    Check(box.CheckState==CheckState.Checked && changes==1,"checkbox mnemonic");
                    Check(HasChoiceAccent(checkbox)==1,"checked Windows checkbox glyph");
                    Press(checkbox); Check(box.CheckState==CheckState.Indeterminate && changes==2,"native three-state click");
                    Check(HasChoiceAccent(checkbox)==1,"mixed Windows checkbox glyph");
                    box.AutoCheck=false; Press(checkbox); Check(box.CheckState==CheckState.Indeterminate && changes==2,"manual checkbox");
                    box.Enabled=false;
                    Check(HasChoiceAccent(checkbox)==0,"disabled checkbox removes accent");
                    r2.PerformClick(); Check(r2.Checked && !r1.Checked,"exclusive group");
                    Check(HasChoiceAccent(mf_widget_view(r2.Handle))==1 && HasChoiceAccent(mf_widget_view(r1.Handle))==0,
                        "exclusive radio glyphs");
                    nint level=mf_widget_view(slider.Handle);
                    DragSlider(level,117,10,13,10); Check(slider.Value==0,"horizontal thumb to minimum");
                    DragSlider(level,13,10,117,10); Check(slider.Value==7,"horizontal thumb to seventh tick");
                    ArrowRight(level); Check(slider.Value==8,"arrow increments slider");
                    nint verticalLevel=mf_widget_view(vertical.Handle);
                    DragSlider(verticalLevel,10,107,10,14); Check(vertical.Value==10,"vertical thumb to maximum");
                }
                save.ImageIndex=0;
                Check(save.ImageKey=="" && save.Image!=null,"index image selection");
                save.Image=icon;
                Check(ReferenceEquals(save.Image,icon) && save.ImageList==null && save.ImageIndex==-1,"direct image overrides list");
                using var commandButton=new Button { CommandParameter="test" };
                var command=new TestCommand();
                commandButton.Command=command;
                commandButton.PerformClick();
                Check(Equals(command.LastArgument,"test"),"command invocation");
                command.Allowed=false;
                command.Notify();
                Check(!commandButton.Enabled,"command availability");
                commandButton.Command=null;
                Check(commandButton.Enabled,"restore enabled when command removed");
                save.AutoSizeMode=AutoSizeMode.GrowAndShrink;
                save.AutoSize=true;
                save.Text="A considerably longer button caption";
                Check(save.Width>130,"grow to fit text and image");
                save.Text="Go";
                Check(save.Width<130,"shrink to fit text and image");
            } catch(Exception e) {failure=e;}
            finally {form.Close();}
        };
        Application.Run(form);
        if (failure!=null) {Console.Error.WriteLine(failure);return 1;}
        Console.WriteLine("PASS: ButtonBase hierarchy, AppKit button bitmap, image source precedence, native mnemonics, checkbox and radio behavior.");
        return 0;
    }
}
