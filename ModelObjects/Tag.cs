using System.Collections.Generic;

public class Tag
{
    public Name Name { get; private set; }
    public double Probability { get; private set; }
    public BoundingBox BoundingBox { get; private set; }

    public int? Depth { get; set; } = null;

    public int? OtsuThreshold { get; set; } = null; // todo: for debugging
    public int? HistogramMaxByte { get; set; } = null; // todo: for debugging
    public int? HistogramMaxCount { get; set; } = null; // todo: for debugging

    public List<int>? ForegroundIndices { get; set; } = null;

    public Tag(Name name, double probability, BoundingBox boundingBox)
    {
        Name = name;
        Probability = probability;
        BoundingBox = boundingBox;
    }
}