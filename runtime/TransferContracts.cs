// Adapted from dotnet/winforms v8.0.20 (MIT). See notices/dotnet-winforms.txt.
namespace System.Windows.Forms;


public interface IDataObject
{
    object? GetData(string format, bool autoConvert);

    object? GetData(string format);

    object? GetData(Type format);

    void SetData(string format, bool autoConvert, object? data);

    void SetData(string format, object? data);

    void SetData(Type format, object? data);

    void SetData(object? data);

    bool GetDataPresent(string format, bool autoConvert);

    bool GetDataPresent(string format);

    bool GetDataPresent(Type format);

    string[] GetFormats(bool autoConvert);

    string[] GetFormats();
}


[Flags]
public enum DragDropEffects
{
    None = 0x00000000,

    Copy = 0x00000001,

    Move = 0x00000002,

    Link = 0x00000004,

    Scroll = unchecked((int)0x80000000),

    All = Copy | Move | Scroll,
}


public enum DropImageType
{
    Invalid = -1,

    None = 0,

    Copy = DragDropEffects.Copy,

    Move = DragDropEffects.Move,

    Link = DragDropEffects.Link,

    Label = 6,

    Warning = 7,

    NoImage = 8
}


public class DragEventArgs : EventArgs
{
    public DragEventArgs(
        IDataObject? data,
        int keyState,
        int x,
        int y,
        DragDropEffects allowedEffect,
        DragDropEffects effect)
        : this(data, keyState, x, y, allowedEffect, effect, dropImageType: DropImageType.Invalid, message: string.Empty, messageReplacementToken: string.Empty)
    {
    }

    public DragEventArgs(
        IDataObject? data,
        int keyState,
        int x,
        int y,
        DragDropEffects allowedEffect,
        DragDropEffects effect,
        DropImageType dropImageType,
        string? message,
        string? messageReplacementToken)
    {
        Data = data;
        KeyState = keyState;
        X = x;
        Y = y;
        AllowedEffect = allowedEffect;
        Effect = effect;
        DropImageType = dropImageType;
        Message = message;
        MessageReplacementToken = messageReplacementToken;
    }

    public IDataObject? Data { get; }

    public int KeyState { get; }

    public int X { get; }

    public int Y { get; }

    public DragDropEffects AllowedEffect { get; }

    public DragDropEffects Effect { get; set; }

    public DropImageType DropImageType { get; set; }

    public string? Message { get; set; }

    public string? MessageReplacementToken { get; set; }

    internal DragEventArgs Clone()
    {
        return (DragEventArgs)MemberwiseClone();
    }

    internal bool Equals(DragEventArgs? dragEventArgs)
    {
        if (dragEventArgs == this)
        {
            return true;
        }

        return dragEventArgs is not null
            && dragEventArgs.Data is not null
            && dragEventArgs.Data.Equals(Data)
            && dragEventArgs.KeyState == KeyState
            && dragEventArgs.X == X
            && dragEventArgs.Y == Y
            && dragEventArgs.AllowedEffect == AllowedEffect
            && dragEventArgs.Effect == Effect
            && dragEventArgs.DropImageType == DropImageType
            && dragEventArgs.Message == Message
            && dragEventArgs.MessageReplacementToken == MessageReplacementToken;
    }
}


public delegate void DragEventHandler(object? sender, DragEventArgs e);
