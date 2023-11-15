using Vector3 = UnityEngine.Vector3;

public class Get : MtmActionHand
{
    public Get(Vector3 worldPosition, string imageTitle, string hand)
    {
        Name = "Get";
        TMU = 9;
        Depth = 0;
        ImageTitle = imageTitle;
        WorldPosition = worldPosition;
        Hand = hand;
    }
}