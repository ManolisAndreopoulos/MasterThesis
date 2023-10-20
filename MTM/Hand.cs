public abstract class Hand
{
    public string State { get; set; } = "empty"; // get or empty
    public string Object { get; set; }
}

public class RightHand : Hand
{
}

public class LeftHand : Hand
{
}