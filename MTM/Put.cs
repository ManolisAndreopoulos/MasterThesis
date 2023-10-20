public class Put : MtmAction
{
    public Put(int distance, string imageTitle)
    {
        Name = "Put";
        TMU = 9;
        Distance = distance;
        ImageTitle = imageTitle;
    }
}