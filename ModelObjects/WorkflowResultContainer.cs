using System;

public class WorkflowResultContainer //todo: rename
{
    public RuntimeContainer RuntimeContainer { get; set; }
    public string Message { get; set; }
    public bool TagsWereFound { get; private set; }

    public WorkflowResultContainer(RuntimeContainer runtimeContainer, bool tagsWereFound)
    {
        RuntimeContainer = runtimeContainer;
        Message = GetRuntimeProfilingMessage(runtimeContainer);
        TagsWereFound = tagsWereFound;
    }

    public WorkflowResultContainer(string message, bool tagsWereFound)
    {
        RuntimeContainer = new RuntimeContainer(0, 0, 0, 0);
        Message = message;
        TagsWereFound = tagsWereFound;
    }

    private string GetRuntimeProfilingMessage(RuntimeContainer runtimeContainer)
    {
        var customVisionDurationPercentage = $"{Math.Round(100 * runtimeContainer.CustomVision / runtimeContainer.Total)}%";
        var blobDurationPercentage = $"{Math.Round(100 * runtimeContainer.BlobStorage / runtimeContainer.Total)}%";
        var tableDurationPercentage = $"{Math.Round(100 * runtimeContainer.TableStorage / runtimeContainer.Total)}%";

        var customVisionDurationMessage =
            $"\nVision Duration: {runtimeContainer.CustomVision}s ({customVisionDurationPercentage})\n";
        var blobStoreDurationMessage = $"Blob Duration: {runtimeContainer.BlobStorage}s ({blobDurationPercentage})\n";
        var tableStoreDurationMessage = $"Table Duration: {runtimeContainer.TableStorage}s ({tableDurationPercentage})\n";
        var totalDurationMessage = $"Total Duration: {runtimeContainer.Total}s\n\n";

        var runtimeProfilingMessage = customVisionDurationMessage + blobStoreDurationMessage +
                                      tableStoreDurationMessage + totalDurationMessage;
        return runtimeProfilingMessage;
    }
}