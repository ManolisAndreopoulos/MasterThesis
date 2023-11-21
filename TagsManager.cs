using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

public class TagsManager
{
    // Global Image Parameters
    private const int Height = 512;
    private const int Width = 512;

    // Regex Patterns
    private static string floatPattern = @"(\d*\.\d+|\d+\.?\d*)";
    private static string anyCharacterApartFromCommaPattern = $"[^,]+";
    private static string probabilityPattern = $"\"probability\":" + floatPattern;
    private static string tagNamePattern = $"\"tagName\":" + anyCharacterApartFromCommaPattern;
    private static string leftSidePattern = $"\"left\":" + floatPattern;
    private static string topSidePattern = $"\"top\":" + floatPattern;
    private static string widthPattern = $"\"width\":" + floatPattern;
    private static string heightPattern = $"\"height\":" + floatPattern;

    // Regex
    private static Regex floatRegex = new Regex(floatPattern);
    private static Regex probabilityRegex = new Regex(probabilityPattern);
    private static Regex tagNameRegex = new Regex(tagNamePattern);
    private static Regex leftSideRegex = new Regex(leftSidePattern);
    private static Regex topSideRegex = new Regex(topSidePattern);
    private static Regex widthRegex = new Regex(widthPattern);
    private static Regex heightRegex = new Regex(heightPattern);

    // Culture Info for Floating Point
    private static CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();

    public void AugmentTagsWithImageTitle(List<Tag> tags, string adjustedImageImageTitle)
    {
        foreach (var tag in tags)
        {
            tag.ImageTitle = adjustedImageImageTitle;
        }
    }

    public void AugmentTagsWithForegroundIndices(List<Tag> tags, Color[] abImagePixels, ushort[] depthMap)
    {
        foreach (var tag in tags)
        {
            // boundaries
            var left = tag.BoundingBox.Left;
            var right = tag.BoundingBox.Right;
            var top = tag.BoundingBox.Top;
            var bottom = tag.BoundingBox.Bottom;

            var boundingBoxPixels = new List<int>(); //values 0 to 255
            for (var x = Math.Max(0, left); x <= Math.Min(Width, right); x++)
            {
                for (var y = Math.Max(0, top); y <= Math.Min(Height, bottom); y++)
                {
                    var index = (Height - y) * Width + x;
                    boundingBoxPixels.Add( (int) (abImagePixels[index].r * 255));
                }
            }

            var threshold = OtsuThresholding.GetOtsuThreshold(boundingBoxPixels.ToArray());

            // Find the indices of the foreground pixels in the adjusted image
            var foregroundPixels = new List<PixelDepth>();
            for (var x = Math.Max(0, left); x <= Math.Min(Width, right); x++)
            {
                for (var y = Math.Max(0, top); y <= Math.Min(Height, bottom); y++)
                {
                    var index = (Height - y) * Width + x;
                    var abIntensity = (int) (abImagePixels[index].r * 255);

                    if (abIntensity > threshold)
                    {
                        var pixelPositionX = x;
                        var pixelPositionY = Height - y;
                        var depth = depthMap[index];
                        var pixelDepth = new PixelDepth(index, depth, pixelPositionX, pixelPositionY);
                        foregroundPixels.Add(pixelDepth);
                    }
                }
            }

            tag.OtsuForegroundPixels = foregroundPixels;
        }
    }

    public List<Tag> GetTagsWithConfidenceHigherThan(float minimumConfidence, List<Tag> tagsToFilter)
    {
        return tagsToFilter.Where(t => t.Probability > minimumConfidence).ToList();
    }

    [CanBeNull]
    public List<Tag> GetPredictedTagsFromResult(string result)
    {
        ci.NumberFormat.CurrencyDecimalSeparator = ".";

        var lines = result.Split('\n');
        var predictionsLine = lines.First(l => l.Contains("predictions"));

        var probabilities = GetFloatModelObjects(probabilityRegex, predictionsLine, floatRegex, ci);
        var tagNames = GetStringModelObjects(tagNameRegex, predictionsLine);
        var leftSides = GetFloatModelObjects(leftSideRegex, predictionsLine, floatRegex, ci);
        var topSides = GetFloatModelObjects(topSideRegex, predictionsLine, floatRegex, ci);
        var widths = GetFloatModelObjects(widthRegex, predictionsLine, floatRegex, ci);
        var heights = GetFloatModelObjects(heightRegex, predictionsLine, floatRegex, ci);

        var numberOfObjects = probabilities.Count;

        var success = VerifyAllPropertiesHaveEqualElements(numberOfObjects, tagNames, leftSides, topSides, widths, heights);

        if (!success)
        {
            return null;
        }

        var predictedTags = new List<Tag>();

        for (var i = 0; i < numberOfObjects; i++)
        {
            var name = new Name(tagNames[i]);
            var probability = probabilities[i];
            var boundingBox = ConvertToBoundingBox(leftSides[i], topSides[i], widths[i], heights[i]);
            var tag = new Tag(name, probability, boundingBox);
            predictedTags.Add(tag);
        }
        return predictedTags;
    }

    private BoundingBox ConvertToBoundingBox(float leftSide, float topSide, float width, float height)
    {
        var left = (int) (leftSide * Width);
        var right =  left + (int) (width * Width);
        var top = (int) (topSide * Height);
        var bottom = top + (int) (height * Height);

        return new BoundingBox(left, right, top, bottom);
    }

    private bool VerifyAllPropertiesHaveEqualElements(int numberOfObjects, List<string> tagNames, List<float> leftSides, List<float> topSides, List<float> widths, List<float> heights)
    {
        return tagNames.Count == numberOfObjects && leftSides.Count == numberOfObjects && topSides.Count == numberOfObjects && widths.Count == numberOfObjects && heights.Count == numberOfObjects;
    }

    private List<string> GetStringModelObjects(Regex mainRegex, string stringToAnalyze)
    {
        var matches = mainRegex.Matches(stringToAnalyze);

        var stringList = new List<string>();
        for (int i = 0; i < matches.Count; i++)
        {
            var value = matches[i].Value;
            var splitVale = value.Split(':');
            var stringValue = splitVale[1].Trim('\"');
            stringList.Add(stringValue);
        }
        return stringList;
    }

    private List<float> GetFloatModelObjects(Regex mainRegex, string stringToAnalyze, Regex floatRegex, CultureInfo ci)
    {
        var matches = mainRegex.Matches(stringToAnalyze);

        var floatList = new List<float>();
        for (int i = 0; i < matches.Count; i++)
        {
            var value = matches[i].Value;
            var splitVale = value.Split(':');
            var floatValue = float.Parse(splitVale[1], NumberStyles.Any, ci);
            floatList.Add(floatValue);
        }
        return floatList;
    }
}

