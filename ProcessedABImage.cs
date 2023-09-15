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
            if (StoreOnly)
            {
                _analyzedImageInBytes = ImageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D);
            }

            return _analyzedImageInBytes;
        }
        private set
        {
            _analyzedImageInBytes = value;
        }
    }

    private byte[] _analyzedImageInBytes;

}

