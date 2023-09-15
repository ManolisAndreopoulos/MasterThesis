﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

public static class TagsManager
{
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

    public static List<Tag> GetTagsWithConfidenceHigherThan(float minimumConfidence, List<Tag> tagsToFilter)
    {
        return tagsToFilter.Where(t => t.Probability > minimumConfidence).ToList();
    }

    [CanBeNull]
    public static List<Tag> GetPredictedTagsFromResult(string result)
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
            var boundingBox = new BoundingBox(leftSides[i], topSides[i], widths[i], heights[i]);
            var tag = new Tag(name, probability, boundingBox);
            predictedTags.Add(tag);
        }
        return predictedTags;
    }

    private static bool VerifyAllPropertiesHaveEqualElements(int numberOfObjects, List<string> tagNames, List<float> leftSides, List<float> topSides, List<float> widths, List<float> heights)
    {
        return tagNames.Count == numberOfObjects && leftSides.Count == numberOfObjects && topSides.Count == numberOfObjects && widths.Count == numberOfObjects && heights.Count == numberOfObjects;
    }

    private static List<string> GetStringModelObjects(Regex mainRegex, string stringToAnalyze)
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

    private static List<float> GetFloatModelObjects(Regex mainRegex, string stringToAnalyze, Regex floatRegex, CultureInfo ci)
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

