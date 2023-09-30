using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ImageUtilities
{
    private const float brightnessFactor = 5f;
    private const float contrastFactor = 0.95f;

    private const int Height = 512;
    private const int Width = 512;

    public byte[] ConvertToPNG(ushort[] originalStream)
    {
        var valuesCount = originalStream.Length;
        var maxvalue = originalStream.Max();

        var pixels = new Color[valuesCount];

        for (var i = 0; i < valuesCount; i++)
        {
            var normalizedValue = originalStream[i] / (float) maxvalue;

            pixels[i] = new Color(normalizedValue, normalizedValue, normalizedValue, 1);
        }

        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

        texture.SetPixels(pixels);
        texture.Apply();

        var image = texture.EncodeToPNG();

        return image;
    }

    public AdjustedImage AdjustBrightnessContrastAndRotate(Texture2D alphaTexture, ushort[] depthFrameData)
    {
        var originalPixels = alphaTexture.GetPixels();
        var modifiedPixels = new Color[originalPixels.Length];

        var height = alphaTexture.height;
        var width = alphaTexture.width;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                //Rotate and flip
                var originalIndex = y * width + x;
                var rotatedAndFlippedIndex = (height - y - 1) * width + x;

                var alpha = originalPixels[originalIndex].a;

                var newR = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newG = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newB = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newA = 1.0f;

                modifiedPixels[rotatedAndFlippedIndex] =
                    new Color(Mathf.Clamp01(newR), Mathf.Clamp01(newG), Mathf.Clamp01(newB), newA);
            }
        }

        var adjustedImageTexture = new Texture2D(alphaTexture.width, alphaTexture.height, TextureFormat.RGBA32, false);

        adjustedImageTexture.SetPixels(modifiedPixels);
        adjustedImageTexture.Apply();

        var image = adjustedImageTexture.EncodeToPNG();

        return new AdjustedImage(image, adjustedImageTexture, depthFrameData);
    }

    public byte[] AugmentImageWithBoundingBoxesAndDepth(List<Tag> tags, Texture2D adjustedImageTexture)
    {
        var originalPixels = adjustedImageTexture.GetPixels();
        var augmentedPixels = originalPixels;
        var boundaryThicknessInPixels = 2;

        var height = adjustedImageTexture.height;
        var width = adjustedImageTexture.width;


        foreach (var tag in tags)
        {
            // boundaries
            var left = tag.BoundingBox.Left;
            var right = tag.BoundingBox.Right;
            var top = tag.BoundingBox.Top;
            var bottom = tag.BoundingBox.Bottom;

            // left boundary
            for (var x = Math.Max(0, left - (boundaryThicknessInPixels - 1)); x <= Math.Min(width, left + (boundaryThicknessInPixels - 1)); x++)
            {
                for (var y = top; y <= bottom; y++)
                {

                    var index = (height - y) * width + x;
                    augmentedPixels[index] = new Color(1, 0, 0, 1);
                }
            }

            // right boundary
            for (var x = Math.Max(0, right - (boundaryThicknessInPixels - 1)); x <= Math.Min(width, right + (boundaryThicknessInPixels - 1)); x++)
            {
                for (var y = top; y <= bottom; y++)
                {

                    var index = (height - y) * width + x;
                    augmentedPixels[index] = new Color(1, 0, 0, 1);
                }
            }

            // top boundary
            for (var x = left; x <= right; x++)
            {
                for (var y = Math.Max(0, top - (boundaryThicknessInPixels - 1)); y <= Math.Min(height, top + (boundaryThicknessInPixels - 1)); y++)
                {

                    var index = (height - y) * width + x;
                    augmentedPixels[index] = new Color(1, 0, 0, 1);
                }
            }

            // bottom boundary
            for (var x = left; x <= right; x++)
            {
                for (var y = Math.Max(0, bottom - (boundaryThicknessInPixels - 1)); y <= Math.Min(height, bottom + (boundaryThicknessInPixels - 1)); y++)
                {

                    var index = (height - y) * width + x;
                    augmentedPixels[index] = new Color(1, 0, 0, 1);
                }
            }

            // Otsu Foreground points in red
            if (tag.OtsuForegroundPixels == null) continue;
            
            foreach (var pixel in tag.OtsuForegroundPixels)
            {
                augmentedPixels[pixel.Index] = new Color(1, 0, 0, 1);
            }
            
            // Heuristic filtering points in blue
            if (tag.HeuristicFilteredPixels == null) continue;
            
            foreach (var pixel in tag.HeuristicFilteredPixels)
            {
                augmentedPixels[pixel.Index] = new Color(0, 0, 1, 1);
            }
        }

        var rgbaTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        rgbaTexture.SetPixels(augmentedPixels);
        rgbaTexture.Apply();

        var augmentedImage = rgbaTexture.EncodeToPNG();

        return augmentedImage;
    }
}