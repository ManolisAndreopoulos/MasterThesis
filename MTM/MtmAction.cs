public abstract class MtmAction
{
    public string Name { get; set; }
    public int TMU { get; set; }
    public int? Distance { get; set; } = null;
    public string ImageTitle { get; set; } = string.Empty;
}