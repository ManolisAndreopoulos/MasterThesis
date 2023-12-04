using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbImageSampler : MonoBehaviour
{
    [SerializeField] private DepthStreamProvider DepthStreamProvider = null;
    [SerializeField] private ActiveBrightnessStreamProvider ActiveBrightnessStreamProvider = null;
    [SerializeField] private RawImage AbRawImage;
    public TextMeshPro DebuggingText = null;

    [Header("Blob Manager")]
    [SerializeField] private BlobManager BlobManager;

    [Header("TCP Client")]
    [SerializeField] private TCPClient TcpClient;

    private bool _startSampling;
    private float _timeLastSampledAnImage;
    private const float TimerForCapturingNewImageInSeconds = 0.25f;
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
            //For Database Storing
            //----------------------------------
            //var time = DateTime.Now;

            //var abImage = (AbRawImage.texture as Texture2D).EncodeToPNG();
            //var abImageContainer = new ImageContainer(
            //    abImage,
            //    $"Image{_counter}.png",
            //    time
            //);


            //var depthImage = DepthStreamProvider.DepthFrameData != null ? _imageUtilities.ConvertDepthMapToPNG(DepthStreamProvider.DepthFrameData) : new byte[20];
            //var depthImageContainer = new ImageContainer(
            //    depthImage,
            //    $"Depth{_counter}.png",
            //    time
            //);

            //StoreImagePngTcpAsync(abImage, depthImage);

            //StoreImagesAsync(abImageContainer, depthImageContainer);

            //_counter++;
            //----------------------------------

            var abImageBuffer = ActiveBrightnessStreamProvider.ShortAbImageBuffer;
            var depthMapBuffer = DepthStreamProvider.DepthFrameDataOriginal;

            StoreViaTcp(abImageBuffer, depthMapBuffer);
        }
    }

    private async Task StoreViaTcp(ushort[] abImageBuffer, ushort[] depthMapBuffer)
    {
        await StoreAbImageBufferTcpAsync(abImageBuffer);
        await Task.Delay(50);
        await StoreDepthMapBufferTcpAsync(depthMapBuffer);
        await Task.Delay(50);
    }

    private async Task StoreAbImageBufferTcpAsync(ushort[] abImage)
    {
#if WINDOWS_UWP
            if(!TcpClient.Connected) return;
            TcpClient.SendAbImageBufferAsync(abImage);
#endif
    }

    private async Task StoreDepthMapBufferTcpAsync(ushort[] depthMap)
    {
#if WINDOWS_UWP
            if(!TcpClient.Connected) return;
            TcpClient.SendDepthMapBufferAsync(depthMap);
#endif
    }

    private async void StoreImageRawTcpAsync(ushort[] abImage, ushort[] depthImage)
    {
#if WINDOWS_UWP
            if(!TcpClient.Connected) return;
            TcpClient.SendUINT16Async(abImage, depthImage);
#endif
    }

    private async void StoreImagePngTcpAsync(byte[] abImage, byte[] depthImage)
    {
#if WINDOWS_UWP
            if(!TcpClient.Connected) return;
            TcpClient.SendImageAsync(abImage, depthImage);
#endif
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

