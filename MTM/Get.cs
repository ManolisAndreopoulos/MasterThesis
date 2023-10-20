public class Get : MtmAction
{
    public Get(int distance, string imageTitle)
    {
        Name = "Get";
        TMU = 9;
        Distance = distance;
        ImageTitle = imageTitle;
    }
}