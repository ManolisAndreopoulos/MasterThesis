using Vector3 = UnityEngine.Vector3;

public class Get : MtmAction
{
    public Get(Vector3 worldPosition, string imageTitle)
    {
        Name = "Get";
        TMU = 9;
        Distance = 0;
        ImageTitle = imageTitle;
        WorldPosition = worldPosition;
    }
}