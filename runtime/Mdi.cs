using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace System.Windows.Forms;

public enum MdiLayout { Cascade, TileHorizontal, TileVertical, ArrangeIcons }

public sealed class MdiClient : Control
{
    public MdiClient() { Dock = DockStyle.Fill; BackColor = SystemColors.AppWorkspace; }
}

public partial class Form
{
    [DllImport("MacForms")] private static extern void mf_mdi_activate(IntPtr h);
    [DllImport("MacForms")] private static extern void mf_mdi_location(IntPtr h,out int x,out int y);
    private MdiClient? client;
    public bool IsMdiContainer
    {
        get => client != null;
        set
        {
            if (value == IsMdiContainer) return;
            if (value)
            {
                if (IsMdiChild) throw new InvalidOperationException("An MDI child cannot be an MDI parent.");
                client = new();
                Controls.Insert(0,client);
            }
            else { client!.Dispose(); client = null; }
        }
    }
    public Form[] MdiChildren => client?.Controls.OfType<Form>().ToArray() ?? Array.Empty<Form>();
    public Form? ActiveMdiChild { get; private set; }
    public event EventHandler? MdiChildActivate;
    protected virtual void OnMdiChildActivate(EventArgs e) => MdiChildActivate?.Invoke(this,e);
    internal void SetActiveChild(Form? child)
    {
        if (ActiveMdiChild == child) return;
        ActiveMdiChild = child;
        OnMdiChildActivate(EventArgs.Empty);
    }
    public bool IsMdiChild => MdiParent != null;
    public Form? MdiParent
    {
        get => (Parent as MdiClient)?.Parent as Form;
        set
        {
            var old = MdiParent;
            if (old == value) return;
            if (value != null && (!value.IsMdiContainer || IsMdiContainer || value == this))
                throw new ArgumentException("Assign an MDI container as the parent.",nameof(value));
            if (IsHandleCreated)
            {
                // Create the new kind of peer before attaching it to its parent.
                Parent = value?.client;
                RecreateWidget();
                if (loaded) Show();
            }
            else Parent = value?.client;
            if (old?.ActiveMdiChild == this) old.SetActiveChild(old.MdiChildren.LastOrDefault(c=>c.loaded));
        }
    }
    public void LayoutMdi(MdiLayout value)
    {
        if (client == null) return;
        if (value == MdiLayout.ArrangeIcons)
        {
            int x = 0, y = client.Height - 24;
            foreach (var f in MdiChildren.Where(c=>c.WindowState == FormWindowState.Minimized))
            {
                if (x != 0 && x+f.Width > client.Width) { x = 0; y -= 24; }
                f.Location = new(x,y);
                x += f.Width;
            }
            return;
        }
        var children = MdiChildren.Where(c=>c.loaded && c.Visible && c.WindowState != FormWindowState.Minimized).ToArray();
        int n = children.Length;
        for (int i=0;i<n;i++)
        {
            int width=client.Width, height=client.Height;
            children[i].Bounds = value switch
            {
                MdiLayout.TileHorizontal => Rectangle.FromLTRB(0,i*height/n,width,(i+1)*height/n),
                MdiLayout.TileVertical => Rectangle.FromLTRB(i*width/n,0,(i+1)*width/n,height),
                _ => new Rectangle(i*24%(Math.Max(1,width/2)),i*24%(Math.Max(1,height/2)),
                    Math.Max(1,width*3/4),Math.Max(1,height*3/4))
            };
        }
    }
}
