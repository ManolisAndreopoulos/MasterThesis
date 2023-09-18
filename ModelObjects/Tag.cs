public class Tag
{
    public Name Name { get; private set; }
    public double Probability { get; private set; }
    public BoundingBox BoundingBox { get; private set; }

    public int? Depth { get; set; } = null;

    public Tag(Name name, double probability, BoundingBox boundingBox)
    {
        Name = name;
        Probability = probability;
        BoundingBox = boundingBox;
    }
}