public class BoundingBox
{
    public double Left { get; }
    public double Top { get; }
    public double Width { get; }
    public double Height { get; }

    public BoundingBox(double left, double top, double width, double height)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }
}