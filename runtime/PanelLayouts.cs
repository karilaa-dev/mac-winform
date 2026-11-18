using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms;

public abstract class TableLayoutStyle
{
    private SizeType type;
    internal Action? Layout;
    public SizeType SizeType { get=>type; set { type=value; Layout?.Invoke(); } }
    protected void Resize(ref float field,float value)
    {
        if(value<0) throw new ArgumentOutOfRangeException(nameof(value));
        field=value; Layout?.Invoke();
    }
}
public class ColumnStyle : TableLayoutStyle
{
    private float width;
    public ColumnStyle() {}
    public ColumnStyle(SizeType sizeType) { SizeType=sizeType; }
    public ColumnStyle(SizeType sizeType,float width) : this(sizeType) { Width=width; }
    public float Width { get=>width; set=>Resize(ref width,value); }
}
public class RowStyle : TableLayoutStyle
{
    private float height;
    public RowStyle() {}
    public RowStyle(SizeType sizeType) { SizeType=sizeType; }
    public RowStyle(SizeType sizeType,float height) : this(sizeType) { Height=height; }
    public float Height { get=>height; set=>Resize(ref height,value); }
}
public abstract class TableLayoutStyleCollection : IList
{
    private readonly TableLayoutPanel owner;
    private readonly Type elementType;
    private readonly List<TableLayoutStyle> items=new();
    internal TableLayoutStyleCollection(TableLayoutPanel owner,Type elementType)
    {
        this.owner=owner; this.elementType=elementType;
    }
    private TableLayoutStyle Accept(object? value)
    {
        if (value is not TableLayoutStyle style || !elementType.IsInstanceOfType(style))
            throw new ArgumentException("Choose a style matching this table dimension.");
        if (style.Layout!=null) throw new ArgumentException("The style already belongs to a table.");
        return style;
    }
    protected TableLayoutStyle this[int index]
    {
        get=>items[index];
        set
        {
            if(items[index]==value) return;
            var style=Accept(value); items[index].Layout=null;
            items[index]=style; style.Layout=owner.PerformLayout; owner.PerformLayout();
        }
    }
    public int Count=>items.Count;
    public int Add(TableLayoutStyle value) { Insert(Count,value); return Count-1; }
    protected void Insert(int index,TableLayoutStyle value)
    {
        var style=Accept(value); items.Insert(index,style); style.Layout=owner.PerformLayout; owner.PerformLayout();
    }
    protected int IndexOf(TableLayoutStyle value)=>items.IndexOf(value);
    public void RemoveAt(int index) { items[index].Layout=null; items.RemoveAt(index); owner.PerformLayout(); }
    public void Clear() { foreach(var item in items) item.Layout=null; items.Clear(); owner.PerformLayout(); }
    bool IList.IsReadOnly=>false;
    bool IList.IsFixedSize=>false;
    int IList.Add(object? value)=>Add(Accept(value));
    void IList.Insert(int index,object? value)=>Insert(index,Accept(value));
    bool IList.Contains(object? value)=>value is TableLayoutStyle style && items.Contains(style);
    int IList.IndexOf(object? value)=>value is TableLayoutStyle style ? items.IndexOf(style):-1;
    void IList.Remove(object? value) { if(value is TableLayoutStyle style && items.IndexOf(style) is int i && i>=0) RemoveAt(i); }
    object? IList.this[int index] { get=>items[index]; set=>this[index]=Accept(value); }
    bool ICollection.IsSynchronized=>false;
    object ICollection.SyncRoot=>this;
    void ICollection.CopyTo(Array array,int index)=>((ICollection)items).CopyTo(array,index);
    IEnumerator IEnumerable.GetEnumerator()=>items.GetEnumerator();
}
public class TableLayoutColumnStyleCollection : TableLayoutStyleCollection
{
    internal TableLayoutColumnStyleCollection(TableLayoutPanel owner):base(owner,typeof(ColumnStyle)) {}
    public new ColumnStyle this[int i] { get=>(ColumnStyle)base[i]; set=>base[i]=value; }
    public int Add(ColumnStyle style)=>base.Add(style);
    public void Insert(int index,ColumnStyle style)=>base.Insert(index,style);
    public int IndexOf(ColumnStyle style)=>base.IndexOf(style);
    public bool Contains(ColumnStyle style)=>IndexOf(style)>=0;
    public void Remove(ColumnStyle style) { int i=IndexOf(style); if(i>=0) RemoveAt(i); }
}
public class TableLayoutRowStyleCollection : TableLayoutStyleCollection
{
    internal TableLayoutRowStyleCollection(TableLayoutPanel owner):base(owner,typeof(RowStyle)) {}
    public new RowStyle this[int i] { get=>(RowStyle)base[i]; set=>base[i]=value; }
    public int Add(RowStyle style)=>base.Add(style);
    public void Insert(int index,RowStyle style)=>base.Insert(index,style);
    public int IndexOf(RowStyle style)=>base.IndexOf(style);
    public bool Contains(RowStyle style)=>IndexOf(style)>=0;
    public void Remove(RowStyle style) { int i=IndexOf(style); if(i>=0) RemoveAt(i); }
}
public struct TableLayoutPanelCellPosition : IEquatable<TableLayoutPanelCellPosition>
{
    private int column,row;
    public TableLayoutPanelCellPosition(int column,int row) { this=default; Column=column; Row=row; }
    public int Column { readonly get=>column; set { if(value< -1) throw new ArgumentOutOfRangeException(nameof(value)); column=value; } }
    public int Row { readonly get=>row; set { if(value< -1) throw new ArgumentOutOfRangeException(nameof(value)); row=value; } }
    public readonly bool Equals(TableLayoutPanelCellPosition other)=>Column==other.Column && Row==other.Row;
    public override readonly bool Equals(object? other)=>other is TableLayoutPanelCellPosition cell && Equals(cell);
    public override readonly int GetHashCode()=>HashCode.Combine(Column,Row);
    public static bool operator ==(TableLayoutPanelCellPosition a,TableLayoutPanelCellPosition b)=>a.Equals(b);
    public static bool operator !=(TableLayoutPanelCellPosition a,TableLayoutPanelCellPosition b)=>!a.Equals(b);
    public override readonly string ToString()=>$"{Column},{Row}";
}
public class TableLayoutControlCollection : Control.ControlCollection
{
    public TableLayoutControlCollection(TableLayoutPanel container):base(container) { Container=container; }
    public TableLayoutPanel Container { get; }
}
public class TableLayoutPanel : Panel,IExtenderProvider
{
    private int columns,rows;
    private int[] widths=[],heights=[];
    private TableLayoutPanelGrowStyle growth=TableLayoutPanelGrowStyle.AddRows;
    private readonly Dictionary<Control,Size> spans=new();
    private Dictionary<Control,(int column,int row)> placed=new();
    internal Dictionary<Control,(int column,int row)> Positions { get; }=new();
    public TableLayoutPanel() { ColumnStyles=new(this); RowStyles=new(this); }
    protected override ControlCollection CreateControlsInstance()=>new TableLayoutControlCollection(this);
    public new TableLayoutControlCollection Controls=>(TableLayoutControlCollection)base.Controls;
    public TableLayoutColumnStyleCollection ColumnStyles { get; }
    public TableLayoutRowStyleCollection RowStyles { get; }
    public int ColumnCount { get=>columns; set { ArgumentOutOfRangeException.ThrowIfNegative(value); columns=value; PerformLayout(); } }
    public int RowCount { get=>rows; set { ArgumentOutOfRangeException.ThrowIfNegative(value); rows=value; PerformLayout(); } }
    public TableLayoutPanelGrowStyle GrowStyle { get=>growth; set { growth=value; PerformLayout(); } }
    bool IExtenderProvider.CanExtend(object extendee)=>extendee is Control c && c.Parent==this;
    public int GetColumn(Control c)=>Positions.GetValueOrDefault(c,(-1,-1)).Item1;
    public int GetRow(Control c)=>Positions.GetValueOrDefault(c,(-1,-1)).Item2;
    public TableLayoutPanelCellPosition GetCellPosition(Control c)=>new(GetColumn(c),GetRow(c));
    public void SetColumn(Control c,int column)=>SetCellPosition(c,new(column,GetRow(c)));
    public void SetRow(Control c,int row)=>SetCellPosition(c,new(GetColumn(c),row));
    public void SetCellPosition(Control c,TableLayoutPanelCellPosition position) { Positions[c]=(position.Column,position.Row); PerformLayout(); }
    public int GetColumnSpan(Control c)=>spans.GetValueOrDefault(c,new Size(1,1)).Width;
    public int GetRowSpan(Control c)=>spans.GetValueOrDefault(c,new Size(1,1)).Height;
    public void SetColumnSpan(Control c,int value) { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); spans[c]=new(value,GetRowSpan(c)); PerformLayout(); }
    public void SetRowSpan(Control c,int value) { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); spans[c]=new(GetColumnSpan(c),value); PerformLayout(); }
    internal void ForgetCell(Control c) { Positions.Remove(c); spans.Remove(c); placed.Remove(c); }
    public int[] GetColumnWidths()=>(int[])widths.Clone();
    public int[] GetRowHeights()=>(int[])heights.Clone();
    public TableLayoutPanelCellPosition GetPositionFromControl(Control c)
    {
        var cell=placed.GetValueOrDefault(c,(-1,-1)); return new(cell.Item1,cell.Item2);
    }
    public Control? GetControlFromPosition(int column,int row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column); ArgumentOutOfRangeException.ThrowIfNegative(row);
        foreach(var (c,(x,y)) in placed)
            if(column>=x && row>=y && column<x+GetColumnSpan(c) && row<y+GetRowSpan(c)) return c;
        return null;
    }
    private (int columns,int rows) Place()
    {
        placed=new();
        var visible=Controls.Where(c=>c.LayoutVisible).ToArray();
        int width=Math.Max(1,Math.Max(ColumnCount,visible.Select(c=>Math.Max(0,GetColumn(c))+GetColumnSpan(c)).DefaultIfEmpty(0).Max()));
        int height=Math.Max(1,Math.Max(RowCount,visible.Select(c=>Math.Max(0,GetRow(c))+GetRowSpan(c)).DefaultIfEmpty(0).Max()));
        var occupied=new HashSet<(int,int)>();
        bool Fits(int x,int y,int w,int h)
        {
            for(int cy=y;cy<y+h;cy++) for(int cx=x;cx<x+w;cx++) if(occupied.Contains((cx,cy))) return false;
            return true;
        }
        // Fixed cells reserve their entire rectangles before automatic placement.
        foreach(var c in visible.OrderBy(c=>GetColumn(c)<0 || GetRow(c)<0))
        {
            int w=GetColumnSpan(c), h=GetRowSpan(c);
            int fixedX=GetColumn(c), fixedY=GetRow(c);
            int x=Math.Max(0,fixedX),y=Math.Max(0,fixedY);
            bool vertical=growth==TableLayoutPanelGrowStyle.AddColumns;
            while(!Fits(x,y,w,h))
            {
                if(fixedX>=0 && fixedY<0) y++;
                else if(fixedY>=0 && fixedX<0) x++;
                else if(vertical) { y++; if(y+h>height) { y=0; x++; } }
                else { x++; if(x+w>width) { x=0; y++; } }
            }
            if(growth==TableLayoutPanelGrowStyle.FixedSize && (x+w>width || y+h>height))
                throw new ArgumentException("The fixed table does not have space for this control.");
            placed[c]=(x,y);
            for(int cy=y;cy<y+h;cy++) for(int cx=x;cx<x+w;cx++) occupied.Add((cx,cy));
            width=Math.Max(width,x+w); height=Math.Max(height,y+h);
        }
        return(width,height);
    }
    private static int[] SizeTracks(int available,int count,(SizeType type,float value)[] styles,IEnumerable<(int start,int span,int desired)> requests)
    {
        var sizes=new int[count]; var weights=new double[count];
        SizeType Mode(int i)=>i<styles.Length ? styles[i].type:SizeType.AutoSize;
        for(int i=0;i<count;i++)
        {
            if(Mode(i)==SizeType.Absolute) sizes[i]=Math.Max(0,(int)Math.Round(styles[i].value));
            if(Mode(i)==SizeType.Percent) weights[i]=Math.Max(0,styles[i].value);
        }
        foreach(var (start,span,desired) in requests.OrderBy(r=>r.span))
        {
            int deficit=desired;
            int lastAuto=-1;
            for(int i=start;i<start+span;i++) { deficit-=sizes[i]; if(Mode(i)==SizeType.AutoSize) lastAuto=i; }
            if(deficit>0 && lastAuto>=0) sizes[lastAuto]+=deficit;
        }
        int remaining=Math.Max(0,available-sizes.Sum());
        double sum=weights.Sum(),fraction=0;
        if(sum==0) sizes[^1]+=remaining;
        else for(int i=0;i<count;i++)
        {
            int previous=(int)Math.Round(fraction); fraction+=remaining*weights[i]/sum;
            sizes[i]+=(int)Math.Round(fraction)-previous;
        }
        return sizes;
    }
    internal override void Arrange(Size delta)
    {
        var (col,row)=Place();
        var desired=placed.Select(pair=>
        {
            var c=pair.Key; var size=c is FlowLayoutPanel flow && flow.AutoSize ? flow.MeasureContents():c.Size;
            return (c,pair.Value.column,pair.Value.row,width:size.Width+c.Margin.Horizontal(),height:size.Height+c.Margin.Vertical());
        }).ToArray();
        widths=SizeTracks(Width-Padding.Horizontal(),col,ColumnStyles.Cast<ColumnStyle>().Select(s=>(s.SizeType,s.Width)).ToArray(),
            desired.Select(r=>(r.column,GetColumnSpan(r.c),r.width)));
        heights=SizeTracks(Height-Padding.Vertical(),row,RowStyles.Cast<RowStyle>().Select(s=>(s.SizeType,s.Height)).ToArray(),
            desired.Select(r=>(r.row,GetRowSpan(r.c),r.height)));
        static int[] Boundaries(int origin,int[] tracks)
        {
            int[] edges=new int[tracks.Length+1]; edges[0]=origin;
            for(int i=0;i<tracks.Length;i++) edges[i+1]=edges[i]+tracks[i];
            return edges;
        }
        int[] xs=Boundaries(Padding.Left,widths), ys=Boundaries(Padding.Top,heights);
        foreach(var (c,(x,y)) in placed)
        {
            var m=c.Margin; int left=xs[x]+m.Left, top=ys[y]+m.Top;
            int w=Math.Max(0,xs[x+GetColumnSpan(c)]-xs[x]-m.Horizontal());
            int h=Math.Max(0,ys[y+GetRowSpan(c)]-ys[y]-m.Vertical());
            var anchor=c.Dock switch {
                DockStyle.Fill=>AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom,
                DockStyle.Top=>AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,
                DockStyle.Bottom=>AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right,
                DockStyle.Left=>AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom,
                DockStyle.Right=>AnchorStyles.Right|AnchorStyles.Top|AnchorStyles.Bottom,_=>c.Anchor };
            var bx=Fit(left,w,c.Width,anchor.HasFlag(AnchorStyles.Left),anchor.HasFlag(AnchorStyles.Right));
            var by=Fit(top,h,c.Height,anchor.HasFlag(AnchorStyles.Top),anchor.HasFlag(AnchorStyles.Bottom));
            c.Bounds=new(bx.start,by.start,bx.size,by.size);
        }
    }
    internal static (int start,int size) Fit(int start,int available,int desired,bool near,bool far)
    {
        int size=near && far ? available:Math.Min(desired,available);
        return(start+(near ? 0:far ? available-size:(available-size)/2),size);
    }
}
public class FlowLayoutPanel : Panel,IExtenderProvider
{
    private readonly HashSet<Control> breaks=new();
    private FlowDirection direction;
    private bool wrap=true;
    public bool WrapContents { get=>wrap; set { wrap=value; PerformLayout(); } }
    public FlowDirection FlowDirection { get=>direction; set { direction=value; PerformLayout(); } }
    public bool GetFlowBreak(Control control)=>breaks.Contains(control);
    public void SetFlowBreak(Control control,bool value)
    {
        if(value) breaks.Add(control); else breaks.Remove(control); PerformLayout();
    }
    bool IExtenderProvider.CanExtend(object obj)=>obj is Control c && c.Parent==this;
    protected override void OnControlRemoved(ControlEventArgs e) { if(e.Control!=null) breaks.Remove(e.Control); base.OnControlRemoved(e); }
    internal Size MeasureContents()=>Flow(false);
    internal override void Arrange(Size delta)=>Flow(true);
    private Size Flow(bool arrange)
    {
        bool vertical=direction is FlowDirection.TopDown or FlowDirection.BottomUp;
        bool reverse=direction is FlowDirection.RightToLeft or FlowDirection.BottomUp;
        int limit=vertical ? Height-Padding.Vertical():Width-Padding.Horizontal();
        int line=0,offset=0,thickness=0,extent=0;
        var group=new List<(Control c,int edge)>();
        void FinishLine()
        {
            if(arrange) foreach(var (child,edge) in group)
            {
                var m=child.Margin;
                int total=vertical ? child.Height+m.Vertical():child.Width+m.Horizontal();
                int along=reverse ? limit-edge-total:edge;
                var a=child.Anchor; bool stretch=child.Dock==DockStyle.Fill;
                if(vertical)
                {
                    var fit=TableLayoutPanel.Fit(Padding.Left+offset+m.Left,Math.Max(0,thickness-m.Horizontal()),child.Width,
                        stretch || child.Dock==DockStyle.Left || a.HasFlag(AnchorStyles.Left),stretch || child.Dock==DockStyle.Right || a.HasFlag(AnchorStyles.Right));
                    child.Bounds=new(fit.start,Padding.Top+along+m.Top,fit.size,child.Height);
                }
                else
                {
                    var fit=TableLayoutPanel.Fit(Padding.Top+offset+m.Top,Math.Max(0,thickness-m.Vertical()),child.Height,
                        stretch || child.Dock==DockStyle.Top || a.HasFlag(AnchorStyles.Top),stretch || child.Dock==DockStyle.Bottom || a.HasFlag(AnchorStyles.Bottom));
                    child.Bounds=new(Padding.Left+along+m.Left,fit.start,child.Width,fit.size);
                }
            }
            extent=Math.Max(extent,line); offset+=thickness; thickness=line=0; group.Clear();
        }
        foreach(var c in Controls.Where(c=>c.LayoutVisible))
        {
            int w=c.Width+c.Margin.Horizontal(),h=c.Height+c.Margin.Vertical(),along=vertical ? h:w;
            if(wrap && group.Count>0 && line+along>limit) FinishLine();
            group.Add((c,line)); line+=along; thickness=Math.Max(thickness,vertical ? w:h);
            if(wrap && breaks.Contains(c)) FinishLine();
        }
        FinishLine();
        return new((vertical ? offset:extent)+Padding.Horizontal(),(vertical ? extent:offset)+Padding.Vertical());
    }
}
