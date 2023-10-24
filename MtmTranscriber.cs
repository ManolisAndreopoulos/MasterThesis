using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using JetBrains.Annotations;
using UnityEngine;

public class MtmTranscriber
{
    private Hand leftHand = new LeftHand();
    private Hand rightHand = new RightHand();

    private Get lastLeftGet = null;
    private Get lastRightGet = null;

    private WorldPositionGenerator worldPositionGenerator = new WorldPositionGenerator();

    public List<MtmAction> GetMTMActionsFromTags(List<WorkflowResultContainer> results)
    {
        List<MtmAction> MTMActionsToTranscribe = new List<MtmAction>();

        // Aggregate all tags in the batch in a single list
        List<Tag> tags = new List<Tag>();
        foreach (var result in results)
        {
            tags.AddRange(result.Tags);
        }

        // Getting the overall counts across all tags from the images in the batch
        var leftHandTags = tags.Where(t => SearchTagForWord(t, "left"));
        var rightHandTags = tags.Where(t => SearchTagForWord(t, "right"));

        var actionLeftHand = GetActionForHand(leftHandTags, leftHand);
        var actionRightHand = GetActionForHand(rightHandTags, rightHand);

        if (actionLeftHand != null)
        {
            lastLeftGet = actionLeftHand.GetType() == typeof(Get) ? (Get)actionLeftHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionLeftHand);
        }
        if (actionRightHand != null)
        {
            lastRightGet = actionRightHand.GetType() == typeof(Get) ? (Get)actionRightHand : null; //Store Get to use later for Put distance
            MTMActionsToTranscribe.Add(actionRightHand);
        }

        return MTMActionsToTranscribe;
    }

    [CanBeNull]
    private MtmAction GetActionForHand(IEnumerable<Tag> tagsForHand, Hand hand)
    {
        var getTags = tagsForHand.Where(t => SearchTagForWord(t, "get"));
        var emptyTags = tagsForHand.Where(t => SearchTagForWord(t, "empty"));

        // A single code is transcribed based on all batch images
        if (getTags.Count() > emptyTags.Count())
        {
            if (hand.State.ToLower().Contains("get")) return null;

            hand.State = "get";
            var highestConfidenceTag = FindTagWithHighestConfidence(getTags);
            var worldPosition = worldPositionGenerator.GetWorldPositionFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var getAction = new Get(worldPosition, imageTitle);
            return getAction;
        }
        else
        {
            if (hand.State.ToLower().Contains("empty")) return null;

            hand.State = "empty";
            var highestConfidenceTag = FindTagWithHighestConfidence(emptyTags);
            var worldPosition = worldPositionGenerator.GetWorldPositionFromPixel(highestConfidenceTag.PixelTakenForDepth);
            var imageTitle = highestConfidenceTag.ImageTitle;
            var putAction = new Put(worldPosition, imageTitle);
            return putAction;
        }
    }

    private Tag FindTagWithHighestConfidence(IEnumerable<Tag> handSpecificStateTags) // For now, we take the depth from the tag with the highest confidence
    {
        return handSpecificStateTags.OrderByDescending(t => t.Probability).First();
        //var highestConfidence = 0;
        //var highestConfidenceTag = 0;

        //foreach (var tag in leftHandGet)
        //{
        //    if (tag.Probability < highestConfidence) continue;

        //    if (tag.Depth != null) highestConfidenceDepth = (int) tag.Depth;
        //}

        //return highestConfidenceDepth;
    }

    private bool SearchTagForWord(Tag tag, string wordLowercase)
    {
        return tag.Name.Get().ToLower().Contains(wordLowercase);
    }
}
