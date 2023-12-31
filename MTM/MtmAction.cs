using System.Numerics;
using Vector3 = UnityEngine.Vector3;

public abstract class MtmAction
{
    public string Name { get; set; }
    public int TMU { get; set; }
}

public abstract class MtmActionHand : MtmAction
{
    public string ImageTitle { get; set; } = string.Empty;
    public int? Depth { get; set; } = null;
    public Vector3 WorldPosition { get; set; } = new Vector3();
    public string Hand { get; set; } //only for Get and Put
}