using System.Runtime.InteropServices;

public class PixelDepth
{
    public int Index { get; }
    /// <summary>
    /// Origin for x-axis is the left edge of the image
    /// </summary>
    public int PixelPositionX { get; }
    /// <summary>
    /// Origin for y-axis is the bottom edge of the image
    /// </summary>
    public int PixelPositionY { get; }
    public ushort Depth { get; }

    public PixelDepth(int transformedIndex, ushort depth, int pixelPositionX, int pixelPositionY)
    {
        Index = transformedIndex;
        Depth = depth;
        PixelPositionX = pixelPositionX;
        PixelPositionY = pixelPositionY;
    }
}