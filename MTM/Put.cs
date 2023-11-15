using UnityEngine.XR;
using Vector3 = UnityEngine.Vector3;

public class Put : MtmActionHand
{
    public Put(Vector3 worldPosition, string imageTitle, string hand)
    {
        Name = "Put";
        TMU = 9;
        Depth = 0; //todo: change that based on the previous get world position
        ImageTitle = imageTitle;
        WorldPosition = worldPosition;
        Hand = hand;
    }
}