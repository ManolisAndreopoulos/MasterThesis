using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DepthUtilities
{
    private const int Height = 512;
    private const int Width = 512;

    public static void AugmentTagsWithDepth(List<Tag> tags, ushort[] depthMap)
    {
        foreach (var tag in tags)
        {
            // boundaries
            var left = tag.BoundingBox.Left;
            var right = tag.BoundingBox.Right;
            var top = tag.BoundingBox.Top;
            var bottom = tag.BoundingBox.Bottom;

            var enclosedDepths = FindBBoxEnclosedDepths(depthMap, left, right, top, bottom);

            var estimatedDepth = EstimateObjectDepth(enclosedDepths.ToList());

            tag.Depth = estimatedDepth;
        }
    }

    private static ushort[] FindBBoxEnclosedDepths(ushort[] depthMap, int left, int right, int top, int bottom)
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

    private static int EstimateObjectDepth(List<ushort> enclosedDepths)
    {
        //var mean = enclosedDepths.Average();
        var median = CalculateMedian(enclosedDepths);

        return median;
    }

    private static int CalculateMedian(List<ushort> values)
    {
        if (values.Count == 0)
        {
            throw new InvalidOperationException("The list is empty. Cannot calculate the median.");
        }

        // Sort the list in ascending order
        values.Sort();

        var middle = values.Count / 2;

        // If there are an odd number of elements, return the middle value
        if (values.Count % 2 != 0) return values[middle];

        // If there are an even number of elements, average the two middle values
        var leftMiddleValue = values[middle - 1];
        var rightMiddleValue = values[middle];
        return (leftMiddleValue + rightMiddleValue) / 2;
    }
}