using UnityEngine;
using UnityEngine.UI;

public class ProcessedABImage : MonoBehaviour
{
    [SerializeField] private RawImage AbRawImage;

    public bool StoreOnly = false;

    /// <summary>
    /// Use this parameter to get the most recent AB Image
    /// </summary>
    public byte[] CurrentImageInBytes
    {
        get
        {
            AnalyzedImageInBytes = ImageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D);
            AnalyzedImageTexture = ImageUtilities.AdjustedImageTexture;
            return AnalyzedImageInBytes;
        }
    }

    /// <summary>
    /// Use this parameter to get the already Analyzed AB Image
    /// </summary>
    public byte[] AnalyzedImageInBytes
    {
        get
        {
            // in the case that CurrentImageInBytes parameter was not previously retrieved, we set the necessary parameters before returning
            if (StoreOnly)
            {
                _analyzedImageInBytes = ImageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D);
                AnalyzedImageTexture = ImageUtilities.AdjustedImageTexture;
            }

            return _analyzedImageInBytes;
        }
        private set => _analyzedImageInBytes = value;
    }

    private byte[] _analyzedImageInBytes;

    /// <summary>
    /// Use this parameter to get the already Analyzed AB Image
    /// </summary>
    public Texture2D AnalyzedImageTexture { get; private set; }
}

