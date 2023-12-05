public class BendAndAriseAction : MtmAction
{
    public int Total { get; }
    public BendAndAriseAction(int total)
    {
        Name = "BendAndArise";
        Total = total;
        TMU = 61* Total;
    }
}