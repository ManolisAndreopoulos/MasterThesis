using UnityEngine;

public static class ImageUtilities
{
    private const float brightnessFactor = 5f;
    private const float contrastFactor = 0.95f;

    public static byte[] AdjustBrightnessContrastAndRotate(Texture2D alphaTexture)
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
                var rotatedAndFlippedIndex = (height - y - 1) * width + x ;

                var alpha = originalPixels[originalIndex].a;

                var newR = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newG = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newB = (((alpha * 255 - 128) * contrastFactor + 128) * brightnessFactor) / 255;
                var newA = 1.0f;

                modifiedPixels[rotatedAndFlippedIndex] = new Color(Mathf.Clamp01(newR), Mathf.Clamp01(newG), Mathf.Clamp01(newB), newA);
            }
        }

        var rgbaTexture = new Texture2D(alphaTexture.width, alphaTexture.height, TextureFormat.RGBA32, false);

        rgbaTexture.SetPixels(modifiedPixels);
        rgbaTexture.Apply();

        var image = rgbaTexture.EncodeToPNG();

        return image;
    }
}