public class StepAction : MtmAction
{
    public int LeftSideStepCount { get; }
    public int RightSideStepCount { get; }
    public int UnknownSideStepCount { get; }
    public int TotalStepCount { get; }

    public StepAction(int leftSideStepCount, int rightSideStepCount, int unknownSideStepCount)
    {
        Name = "Step";
        LeftSideStepCount = leftSideStepCount;
        RightSideStepCount = rightSideStepCount;
        UnknownSideStepCount = unknownSideStepCount;
        TotalStepCount = LeftSideStepCount + RightSideStepCount + UnknownSideStepCount;
        TMU = 18* TotalStepCount;
    }
}