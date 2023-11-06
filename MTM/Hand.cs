public abstract class Hand
{
    public string State { get; set; } // get or empty
}

public class RightHand : Hand
{
    public RightHand(string initialState)
    {
        State = initialState;
    }
}

public class LeftHand : Hand
{
    public LeftHand(string initialState)
    {
        State = initialState;
    }
}