using System;
using System.Collections.Generic;
using JetBrains.Annotations;

public class WorkflowResultContainer //todo: rename
{
    public RuntimeContainer RuntimeContainer { get; set; }
    public string Message { get; set; }
    public bool TagsWereFound { get; private set; }
    public List<Tag> Tags { get; private set; }
    [CanBeNull] public ImageContainer ImageContainer { get; private set; }

    public WorkflowResultContainer(List<Tag> tags, RuntimeContainer runtimeContainer, bool tagsWereFound, ImageContainer imageContainer)
    {
        RuntimeContainer = runtimeContainer;
        Message = GetRuntimeProfilingMessage(runtimeContainer);
        TagsWereFound = tagsWereFound;
        Tags = tags;
        ImageContainer = imageContainer;
    }

    public WorkflowResultContainer(string message, bool tagsWereFound)
    {
        RuntimeContainer = new RuntimeContainer(0, 0);
        Message = message;
        TagsWereFound = tagsWereFound;
        Tags = new List<Tag>();
        ImageContainer = null;
    }

    private string GetRuntimeProfilingMessage(RuntimeContainer runtimeContainer)
    {
        var customVisionDurationPercentage = $"{Math.Round(100 * runtimeContainer.CustomVision / runtimeContainer.Total)}%";

        var customVisionDurationMessage =
            $"\nVision Duration: {runtimeContainer.CustomVision}s ({customVisionDurationPercentage})\n";
        var totalDurationMessage = $"Total Duration: {runtimeContainer.Total}s\n\n";

        var runtimeProfilingMessage = customVisionDurationMessage + totalDurationMessage;
        return runtimeProfilingMessage;
    }
}