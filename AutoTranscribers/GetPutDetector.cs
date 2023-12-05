using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GetPutDetector : MonoBehaviour
{
    public DistanceClassGenerator DistanceClassGenerator = null;

    private Hand leftHand = new LeftHand("empty");
    private Hand rightHand = new RightHand("empty");

    private Get lastLeftGet = null;
    private Get lastRightGet = null;

    public TextMeshPro DebuggingTextInternal = null; //todo: to be deleted
    private string _debuggingTextInternal = string.Empty; //todo: to be deleted
    public TextMeshPro DebuggingTextActions = null; //todo: to be deleted
    private string _debuggingTextActions = string.Empty; //todo: to be deleted
    private readonly object _lock = new object(); //todo: to be deleted

    private int _getCountRight;
    private int _getCountLeft;
    private int _putCountRight;
    private int _putCountLeft;

    void Update()
    {
        UpdateDebuggingMessages();
    }

    public void UpdateDebuggingMessages()
    {
        DebuggingTextInternal.text = _debuggingTextInternal;
        DebuggingTextActions.text = _debuggingTextActions;
    }

    public List<MtmActionHand> GetMTMActionsFromTags(List<WorkflowResultContainer> results)
    {
        var debuggingTextInternal = string.Empty;
        List<MtmActionHand> MTMActionsToTranscribe = new List<MtmActionHand>();

        // Aggregate all tags in the batch in a single list
        List<Tag> tags = new List<Tag>();
        foreach (var result in results)
        {
            if(!result.TagsWereFound) continue;

            tags.AddRange(result.Tags);
        }

        debuggingTextInternal += $"{tags.Count} tags where added to list, which are:\n";
        foreach (var tag in tags)
        {
            debuggingTextInternal += $"{tag.Name.Get()},";
        }

        debuggingTextInternal += "\n";

        if (tags.Count == 0)
        {
            lock (_lock)
            {
                _debuggingTextInternal = debuggingTextInternal;
                _debuggingTextActions = $"Left Gets: {_getCountLeft}\n" +
                                        $"Left Puts: {_putCountLeft}\n\n" +
                                        $"Right Gets: {_getCountRight}\n" +
                                        $"Right Puts: {_putCountRight}";
            }
            return new List<MtmActionHand>();
        } //todo: move up

        // Getting the overall counts across all tags from the images in the batch
        var leftHandTags = tags.Where(t => SearchTagForWord(t, "left")).ToList();
        var rightHandTags = tags.Where(t => SearchTagForWord(t, "right")).ToList();

        var actionLeftHand = GetActionForHand(leftHandTags, leftHand, debuggingTextInternal);
        var actionRightHand = GetActionForHand(rightHandTags, rightHand, debuggingTextInternal);

        if (actionLeftHand != null)
        {
            if (actionLeftHand.Name == "Get")
            {
                _getCountLeft++;
            }
            else if (actionLeftHand.Name == "Put")
            {
                _putCountLeft++;
            }
            
            lastLeftGet = actionLeftHand.Name == "Get" ? (Get)actionLeftHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionLeftHand);
        }
        if (actionRightHand != null)
        {
            if (actionRightHand.Name == "Get")
            {
                _getCountRight++;
            }
            else if (actionRightHand.Name == "Put")
            {
                _putCountRight++;
            }

            lastRightGet = actionRightHand.Name.Contains("Get") ? (Get)actionRightHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionRightHand);
        }

        lock (_lock)
        {
            _debuggingTextInternal = debuggingTextInternal;
            _debuggingTextActions = $"Left Gets: {_getCountLeft}\n" +
                                    $"Left Puts: {_putCountLeft}\n\n" +
                                    $"Right Gets: {_getCountRight}\n" +
                                    $"Right Puts: {_putCountRight}";
        }

        return MTMActionsToTranscribe;
    }

    [CanBeNull]
    private MtmActionHand GetActionForHand(List<Tag> tagsForHand, Hand hand, string debuggingTextInternal) //todo: delete the string debugging parameter 
    {
        var getTags = tagsForHand.Where(t => SearchTagForWord(t, "get")).ToList();
        var emptyTags = tagsForHand.Where(t => SearchTagForWord(t, "empty")).ToList();

        // A single code is transcribed based on all batch images
        if (getTags.Count > emptyTags.Count)
        {
            if (hand.CurrentState == "get")
            {
                debuggingTextInternal += $"{hand.GetType()} Get already transcribed\n";
                return null;
            }

            hand.CurrentState = "get";
            var highestConfidenceTag = FindTagWithHighestConfidence(getTags);
            if (highestConfidenceTag == null) return null;
            var distanceClass = DistanceClassGenerator.GetDistanceClassFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var getAction = new Get(distanceClass, imageTitle, hand.GetType().ToString());
            return getAction;
        }

        if (getTags.Count < emptyTags.Count)
        {
            if (hand.CurrentState == "empty")
            {
                debuggingTextInternal += $"{hand.GetType()} is empty\n";
                return null;
            }
            
            hand.CurrentState = "empty";
            var highestConfidenceTag = FindTagWithHighestConfidence(emptyTags);
            if (highestConfidenceTag == null) return null;
            var distanceClass = DistanceClassGenerator.GetDistanceClassFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var putAction = new Put(distanceClass, imageTitle, hand.GetType().ToString());
            return putAction;
        }

        return null;
    }

    [CanBeNull]
    private Tag FindTagWithHighestConfidence(List<Tag> handSpecificStateTags) // For now, we take the depth from the tag with the highest confidence
    {
        //return handSpecificStateTags.OrderByDescending(t => t.Probability).First();
        var highestConfidence = 0;
        Tag highestConfidenceTag = null;

        foreach (var tag in handSpecificStateTags)
        {
            if (tag.Probability < highestConfidence) continue;

            if (tag.Depth != null) highestConfidenceTag = tag;
        }

        return highestConfidenceTag;
    }

    private bool SearchTagForWord(Tag tag, string wordLowercase)
    {
        return tag.Name.Get().ToLower().Contains(wordLowercase);
    }
}
