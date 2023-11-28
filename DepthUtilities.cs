using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;

public class DepthUtilities
{
    private const int Height = 512;
    private const int Width = 512;

    public void AugmentTagsWithFilteredDepth(List<Tag> tags)
    {
        foreach (var tag in tags)
        {
            var medianDepth = CalculatePixelDepthWithMedianDepth(tag.OtsuForegroundPixels);

            tag.HeuristicFilteredPixels = ApplyHeuristicFilteringTo(tag.OtsuForegroundPixels, medianDepth.Depth);

            var estimatedPixelDepth = EstimateObjectPixelDepth(tag.HeuristicFilteredPixels);

            tag.PixelTakenForDepth = estimatedPixelDepth;
            tag.Depth = estimatedPixelDepth.Depth;
        }
    }

    private PixelDepth? EstimateObjectPixelDepth(List<PixelDepth> tagHeuristicFilteredPixels)
    {
        return CalculatePixelDepthWithMedianDepth(tagHeuristicFilteredPixels);
    }

    private List<PixelDepth> ApplyHeuristicFilteringTo(List<PixelDepth> tagOtsuForegroundPixels, int medianDepth)
    {
        var filteredPixelDepths = new List<PixelDepth>();
        var depthOffsetFromMedianInMillimeters = 100;

        var maxDepth = medianDepth + depthOffsetFromMedianInMillimeters;
        var minDepth = medianDepth - depthOffsetFromMedianInMillimeters;

        foreach (var pixel in tagOtsuForegroundPixels)
        {
            if (pixel.Depth < maxDepth && pixel.Depth > minDepth)
            {
                filteredPixelDepths.Add(pixel);
            }
        }

        return filteredPixelDepths;
    }

    private PixelDepth CalculatePixelDepthWithMedianDepth(List<PixelDepth> pixelDepths)
    {
        if (pixelDepths.Count == 0)
        {
            throw new InvalidOperationException("The list is empty. Cannot calculate the median.");
        }

        var depths = new List<int>();
        foreach (var pixel in pixelDepths)
        {
            depths.Add(pixel.Depth);
        }

        // Sort the list in ascending order
        depths.Sort();
        var middle = depths.Count / 2;

        // If there are an odd number of elements, return the middle value
        if (depths.Count % 2 != 0)
        {
            var medianDepth = depths[middle];

            return pixelDepths.Find(p => p.Depth == medianDepth);
        }

        // If there are an even number of elements, take one of the two values (currently left, since this kind of granularity does not really matter)
        var leftMedianDepth = depths[middle - 1];

        return pixelDepths.Find(p => p.Depth == leftMedianDepth);
    }
}