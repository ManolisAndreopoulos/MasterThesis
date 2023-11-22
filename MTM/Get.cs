using System;
using Vector3 = UnityEngine.Vector3;

public class Get : MtmActionHand
{
    public Get(DistanceClass distanceClass, string imageTitle, string hand)
    {
        Name = "Get";
        TMU = 9;
        Depth = distanceClass.DistanceClassMtm;
        ImageTitle = imageTitle;
        WorldPosition = distanceClass.WorldPosition;
        Hand = hand;
    }
}