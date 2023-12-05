public class Put : MtmActionHand
{
    public Put(DistanceClass distanceClass, string imageTitle, string hand)
    {
        Name = "Put";
        TMU = 9;
        Depth = distanceClass.DistanceClassMtm;
        ImageTitle = imageTitle;
        WorldPosition = distanceClass.WorldPosition;
        Hand = hand;
    }
}