public abstract class Hand
{
    public string CurrentState { get; set; } // get or empty
}

public class RightHand : Hand
{
    public RightHand(string initialState)
    {
        CurrentState = initialState;
    }
}

public class LeftHand : Hand
{
    public LeftHand(string initialState)
    {
        CurrentState = initialState;
    }
}