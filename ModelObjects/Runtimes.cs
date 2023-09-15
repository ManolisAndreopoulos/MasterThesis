public class Runtimes
{
    public double CustomVision { get; }
    public double BlobStorage { get; }
    public double TableStorage { get; }
    public double Total { get; }

    public Runtimes(double customVision, double blobStorage, double tableStorage, double total)
    {
        CustomVision = customVision;
        BlobStorage = blobStorage;
        TableStorage = tableStorage;
        Total = total;
    }
}