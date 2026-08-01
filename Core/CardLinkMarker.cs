namespace VYgo.Core;

//箭头
[Flags]
public enum CardLinkMarker {
    None = 0,
    BottomLeft = 0x01,
    Bottom = 0x02,
    BottomRight = 0x04,
    
    Left = 0x08,

    Right = 0x20,
    TopLeft = 0x40,
    Top = 0x80,
    TopRight = 0x100
}
