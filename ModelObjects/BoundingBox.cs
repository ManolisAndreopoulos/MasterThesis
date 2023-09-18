public class BoundingBox
{
    public int Left { get; }
    public int Right { get; }
    public int Top { get; }
    public int Bottom { get; }

    public BoundingBox(int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }
}