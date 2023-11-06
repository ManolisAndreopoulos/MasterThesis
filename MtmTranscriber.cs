using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using JetBrains.Annotations;
using UnityEngine;

public class MtmTranscriber : MonoBehaviour
{
    public WorldPositionGenerator WorldPositionGenerator = null;

    private Hand leftHand = new LeftHand("empty");
    private Hand rightHand = new RightHand("empty");

    private Get lastLeftGet = null;
    private Get lastRightGet = null;

    public string DebugMessage = string.Empty; //todo: to be deleted

    public List<MtmAction> GetMTMActionsFromTags(List<WorkflowResultContainer> results)
    {
        DebugMessage = string.Empty;
        List<MtmAction> MTMActionsToTranscribe = new List<MtmAction>();

        // Aggregate all tags in the batch in a single list
        List<Tag> tags = new List<Tag>();
        foreach (var result in results)
        {
            if(!result.TagsWereFound) continue;

            tags.AddRange(result.Tags);
        }

        DebugMessage += $"{tags.Count} tags where added to list, which are:\n";
        foreach (var tag in tags)
        {
            DebugMessage += $"{tag.Name.Get()},";
        }

        DebugMessage += "\n";

        if(tags.Count == 0) { return new List<MtmAction>(); }

        // Getting the overall counts across all tags from the images in the batch
        var leftHandTags = tags.Where(t => SearchTagForWord(t, "left")).ToList();
        var rightHandTags = tags.Where(t => SearchTagForWord(t, "right")).ToList();

        var actionLeftHand = GetActionForHand(leftHandTags, leftHand);
        var actionRightHand = GetActionForHand(rightHandTags, rightHand);

        if (actionLeftHand != null)
        {
            //todo: does not go in here
            DebugMessage += $"RHA transcribed: {actionRightHand.Name}\n";
            lastLeftGet = actionLeftHand.Name.Contains("Get") ? (Get)actionLeftHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionLeftHand);
        }
        if (actionRightHand != null)
        {
            DebugMessage += $"LHA transcribed: {actionLeftHand.Name}\n";
            lastRightGet = actionRightHand.Name.Contains("Get") ? (Get)actionRightHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionRightHand);
        }

        return MTMActionsToTranscribe;
    }

    [CanBeNull]
    private MtmAction GetActionForHand(List<Tag> tagsForHand, Hand hand)
    {
        var getTags = tagsForHand.Where(t => SearchTagForWord(t, "get")).ToList();
        var emptyTags = tagsForHand.Where(t => SearchTagForWord(t, "empty")).ToList();

        // A single code is transcribed based on all batch images
        if (getTags.Count > emptyTags.Count)
        {
            if (hand.State.ToLower().Contains("get"))
            {
                DebugMessage += $"{hand.GetType()} Get already transcribed\n";
                return null;
            }

            hand.State = "get";
            var highestConfidenceTag = FindTagWithHighestConfidence(getTags);
            if (highestConfidenceTag == null) return null;
            var worldPosition = WorldPositionGenerator.GetWorldPositionFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var getAction = new Get(worldPosition, imageTitle, hand.GetType().ToString());
            return getAction;
        }

        if (getTags.Count < emptyTags.Count)
        {
            if (hand.State.ToLower().Contains("empty"))
            {
                DebugMessage += $"{hand.GetType()} Put already transcribed\n";
                return null;
            }

            hand.State = "empty";
            var highestConfidenceTag = FindTagWithHighestConfidence(emptyTags);
            if (highestConfidenceTag == null) return null;
            var worldPosition = WorldPositionGenerator.GetWorldPositionFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var putAction = new Put(worldPosition, imageTitle, hand.GetType().ToString());
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
