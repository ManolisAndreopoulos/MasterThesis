using System;
using System.Collections.Generic;
using System.Linq;

public static class OtsuThresholding
{
    private static List<int> _histogramOfIntensities = new List<int>(); // todo: for debugging


    // Function to perform Otsu's thresholding on the image
    //public static byte[] OtsuThresholdImage(int[] pixels)
    //{
    //    var histogram = CalculateHistogram(pixels);
    //    var totalPixels = pixels.Length;
    //    var threshold = CalculateOtsuThreshold(histogram, totalPixels);

    //    // Create a new image with threshold applied
    //    byte[] thresholdedImage = new byte[totalPixels];
    //    for (int i = 0; i < totalPixels; i++)
    //    {
    //        thresholdedImage[i] = (pixels[i] > threshold) ? (byte)255 : (byte)0;
    //    }

    //    return thresholdedImage;
    //}

    // Function to get Otsu's threshold for the image
    public static int GetOtsuThreshold(int[] pixels)
    {
        var histogram = CalculateHistogram(pixels);
        _histogramOfIntensities = histogram.ToList();

        var totalPixels = pixels.Length;
        var threshold = CalculateOtsuThreshold(histogram, totalPixels);

        return threshold;
    }

    public static int GetMostFrequentDepthInBBox() //todo: for debugging
    {
        return _histogramOfIntensities.IndexOf(_histogramOfIntensities.Max());
    }

    public static int GetCountOfMostFrequentDepthInBBox() //todo: for debugging
    {
        return _histogramOfIntensities.Max();
    }

    // Function to calculate the histogram of the image
    private static int[] CalculateHistogram(int[] pixels)
    {
        var histogram = new int[256]; // Assuming 8-bit grayscale image
        foreach (var pixel in pixels)
        {
            histogram[pixel]++;
        }
        return histogram;
    }

    // Function to calculate Otsu's threshold
    private static int CalculateOtsuThreshold(int[] histogram, int totalPixels)
    {
        double weightedSumOfAllPixelIntensities = 0.0;
        for (int i = 0; i < 256; i++)
        {
            weightedSumOfAllPixelIntensities += i * histogram[i];
        }

        var weightedSumOfBackgroundPixelIntensities = 0.0;
        var countPixelsBackground = 0;
        var countPixelsForeground = 0;

        var maxBetweenClassVariance = 0.0;
        var threshold = 0;

        for (var i = 0; i < 256; i++)
        {
            countPixelsBackground += histogram[i];
            if (countPixelsBackground == 0)
                continue;

            countPixelsForeground = totalPixels - countPixelsBackground;
            if (countPixelsForeground == 0)
                break;

            weightedSumOfBackgroundPixelIntensities += i * histogram[i];

            var meanIntensityBackground = weightedSumOfBackgroundPixelIntensities / countPixelsBackground;
            var meanIntensityForeground = (weightedSumOfAllPixelIntensities - weightedSumOfBackgroundPixelIntensities) / countPixelsForeground;

            var betweenClassVariance = countPixelsBackground * countPixelsForeground * (meanIntensityBackground - meanIntensityForeground) * (meanIntensityBackground - meanIntensityForeground);

            if (betweenClassVariance > maxBetweenClassVariance)
            {
                maxBetweenClassVariance = betweenClassVariance;
                threshold = i;
            }
        }

        return threshold;
    }
}
