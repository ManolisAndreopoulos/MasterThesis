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
            var medianDepth = CalculateMedianDepth(tag.OtsuForegroundPixels);

            tag.HeuristicFilteredPixels = ApplyHeuristicFilteringTo(tag.OtsuForegroundPixels, medianDepth);

            var estimatedDepth = EstimateObjectDepth(tag.HeuristicFilteredPixels); 

            if (estimatedDepth == null)
            {
                tag.Depth = 0;
                return;
            }

            tag.Depth = estimatedDepth;
        }
    }

    private int? EstimateObjectDepth(List<PixelDepth> tagHeuristicFilteredPixels)
    {
        return CalculateMedianDepth(tagHeuristicFilteredPixels);
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

    private ushort[] FindBBoxEnclosedDepths(ushort[] depthMap, int left, int right, int top, int bottom)
    {
        var enclosedDepths = new List<ushort>();
        for (var x = Math.Max(0, left); x <= Math.Min(Width, right); x++)
        {
            for (var y = Math.Max(0, top); y <= Math.Min(Height, bottom); y++)
            {
                // Check if (x, y) is within the individual boundary regions
                if ((x >= left && x <= right) || (y >= top && y <= bottom))
                {
                    var index = (Height - y) * Width + x;
                    enclosedDepths.Add(depthMap[index]);
                }
            }
        }
        return enclosedDepths.ToArray();
    }

    private int CalculateMedianDepth(List<PixelDepth> pixelDepths)
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
        if (depths.Count % 2 != 0) return depths[middle];

        // If there are an even number of elements, average the two middle values
        var leftMiddleValue = depths[middle - 1];
        var rightMiddleValue = depths[middle];
        return (leftMiddleValue + rightMiddleValue) / 2;
    }
}