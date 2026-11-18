using System.Drawing;

namespace System.Windows.Forms;

public class ToolStripContentPanel : Panel { }

public class ToolStripPanel : ContainerControl
{
    private Orientation orientation;
    private readonly Dictionary<Control,int> rows = new();
    public ToolStripPanel()
    {
        Size = Size.Empty;
        ControlAdded += (_,_) => Parent?.PerformLayout();
        ControlRemoved += (_,e) => { if (e.Control != null) rows.Remove(e.Control); Parent?.PerformLayout(); };
    }
    public Orientation Orientation
    {
        get => orientation;
        set { orientation = value; Parent?.PerformLayout(); PerformLayout(); }
    }
    public void Join(ToolStrip toolStrip) => Join(toolStrip,0);
    public void Join(ToolStrip toolStrip,int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        rows[toolStrip] = row;
        Controls.Add(toolStrip);
        Parent?.PerformLayout();
        PerformLayout();
    }
    public void Join(ToolStrip toolStrip,Point location)
    {
        var neighbour = Controls.FirstOrDefault(c => c.Bounds.Contains(location));
        Join(toolStrip,neighbour != null ? rows.GetValueOrDefault(neighbour) : rows.Count);
    }
    public void Join(ToolStrip toolStrip,int x,int y) => Join(toolStrip,new Point(x,y));
    private bool Vertical => orientation == Orientation.Vertical;
    private IEnumerable<IGrouping<int,Control>> Groups => Controls.GroupBy(c=>rows.GetValueOrDefault(c)).OrderBy(g=>g.Key);
    internal int Thickness => Groups.Sum(g=>g.Max(c=> Vertical ? c.Width : c.Height));
    internal override void Arrange(Size delta)
    {
        int offset = 0;
        foreach (var group in Groups)
        {
            int position = 0, depth = group.Max(c=>Vertical ? c.Width : c.Height);
            foreach (var control in group)
            {
                control.Location = Vertical ? new(offset,position) : new(position,offset);
                position += Vertical ? control.Height : control.Width;
            }
            offset += depth;
        }
    }
}

public class ToolStripContainer : ContainerControl
{
    public ToolStripPanel TopToolStripPanel { get; } = new() { Dock = DockStyle.Top };
    public ToolStripPanel BottomToolStripPanel { get; } = new() { Dock = DockStyle.Bottom };
    public ToolStripPanel LeftToolStripPanel { get; } = new() { Dock = DockStyle.Left, Orientation = Orientation.Vertical };
    public ToolStripPanel RightToolStripPanel { get; } = new() { Dock = DockStyle.Right, Orientation = Orientation.Vertical };
    public ToolStripContentPanel ContentPanel { get; } = new() { Dock = DockStyle.Fill };
    public ToolStripContainer()
    {
        Size = new(150,175);
        Controls.AddRange(new Control[] { ContentPanel,LeftToolStripPanel,RightToolStripPanel,TopToolStripPanel,BottomToolStripPanel });
    }
    public bool TopToolStripPanelVisible { get => TopToolStripPanel.Visible; set { TopToolStripPanel.Visible = value; PerformLayout(); } }
    public bool BottomToolStripPanelVisible { get => BottomToolStripPanel.Visible; set { BottomToolStripPanel.Visible = value; PerformLayout(); } }
    public bool LeftToolStripPanelVisible { get => LeftToolStripPanel.Visible; set { LeftToolStripPanel.Visible = value; PerformLayout(); } }
    public bool RightToolStripPanelVisible { get => RightToolStripPanel.Visible; set { RightToolStripPanel.Visible = value; PerformLayout(); } }
    internal override void Arrange(Size delta)
    {
        int top = TopToolStripPanelVisible ? TopToolStripPanel.Thickness : 0;
        int bottom = BottomToolStripPanelVisible ? BottomToolStripPanel.Thickness : 0;
        int left = LeftToolStripPanelVisible ? LeftToolStripPanel.Thickness : 0;
        int right = RightToolStripPanelVisible ? RightToolStripPanel.Thickness : 0;
        TopToolStripPanel.Bounds = new(0,0,Width,top);
        BottomToolStripPanel.Bounds = new(0,Height-bottom,Width,bottom);
        int middle = Math.Max(0,Height-top-bottom);
        LeftToolStripPanel.Bounds = new(0,top,left,middle);
        RightToolStripPanel.Bounds = new(Width-right,top,right,middle);
        ContentPanel.Bounds = new(left,top,Math.Max(0,Width-left-right),middle);
    }
}
