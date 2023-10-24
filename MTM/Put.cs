using Vector3 = UnityEngine.Vector3;

public class Put : MtmAction
{
    public Put(Vector3 worldPosition, string imageTitle)
    {
        Name = "Put";
        TMU = 9;
        Distance = 0; //todo: change that based on the previous get world position
        ImageTitle = imageTitle;
        WorldPosition = worldPosition;
    }
}