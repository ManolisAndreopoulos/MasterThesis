using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbImageSampler : MonoBehaviour
{
    [SerializeField] private DepthStreamProvider DepthStreamProvider = null;
    [SerializeField] private RawImage AbRawImage;
    public TextMeshPro DebuggingText = null;

    [Header("Blob Manager")]
    [SerializeField] private BlobManager BlobManager;

    private bool _startSampling;
    private float _timeLastSampledAnImage;
    private const float TimerForCapturingNewImageInSeconds = 0.5f;
    private readonly ImageUtilities _imageUtilities = new ImageUtilities();

    private int _counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        _timeLastSampledAnImage = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_startSampling) return; // Do not start queueing before the first Detect

        if (TimerRinging(TimerForCapturingNewImageInSeconds))
        {
            var time = DateTime.Now;

            var abImage = (AbRawImage.texture as Texture2D).EncodeToPNG();
            var abImageContainer = new ImageContainer(
                abImage,
                $"Image{_counter}.png",
                time
            );


            var depthImage = DepthStreamProvider.DepthFrameData != null ? _imageUtilities.ConvertDepthMapToPNG(DepthStreamProvider.DepthFrameData) : new byte[20];
            var depthImageContainer = new ImageContainer(
                depthImage,
                $"Depth{_counter}.png",
                time
            );

            StoreImagesAsync(abImageContainer, depthImageContainer);

            _counter++;
        }
    }

    public void StartSamplingAbImages()
    {
        _startSampling = true;
    }
    public void StopSamplingAbImages()
    {
        _startSampling = false;
        DebuggingText.text = "Processed Stopped.";
    }

    private ImageContainer GetCurrentAdjustedAbImage()
    {
        return _imageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D, DepthStreamProvider.DepthFrameData);
    }

    private bool TimerRinging(float timerValueInSeconds)
    {
        if (!(Time.time - _timeLastSampledAnImage > timerValueInSeconds)) return false;

        _timeLastSampledAnImage = Time.time;
        return true;
    }

    private async void StoreImagesAsync(ImageContainer abImageContainer, ImageContainer depthImageContainer)
    {
        DebuggingText.text = await BlobManager.StoreImagesForPostProcessing(abImageContainer, depthImageContainer);
    }
}

