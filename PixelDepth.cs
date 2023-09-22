using System.Runtime.InteropServices;

public class PixelDepth
{
    public int Index { get; }
    public ushort Depth { get; }
    public PixelDepth(int transformedIndex, ushort depth)
    {
        Index = transformedIndex;
        Depth = depth;
    }
}