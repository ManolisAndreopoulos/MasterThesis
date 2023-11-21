﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class AdjustedImageProvider : MonoBehaviour
{
    [SerializeField] private DepthStreamProvider DepthStreamProvider = null;
    [SerializeField] private RawImage AbRawImage;

    public List<AdjustedImage> NewAdjustedAbImageBatch { get; private set; } = new List<AdjustedImage>();
    public bool DetectWorkflowIsTriggered { get; set; } = false;

    public bool EnoughImagesAreCaptured => _adjustedAbImageBuffer.Count >= BatchSize;


    private const int BatchSize = 2; // 4;
    private const float TimerForCapturingNewImageInSeconds = 0.5f; // 0.25f;
    private const int MaxElementsInBuffer = 40;

    private ImageUtilities _imageUtilities = new ImageUtilities();
    private static Queue<AdjustedImage> _adjustedAbImageBuffer = new Queue<AdjustedImage>();
    private float _timeLastUpdatedProcessedImages;
    private readonly object _lock = new object();

    // Start is called before the first frame update
    void Start()
    {
        DetectWorkflowIsTriggered = false;
        _timeLastUpdatedProcessedImages = Time.time;
    }

    #region Capturing AB Images

    // Update is called once per frame
    void Update()
    {
        if(!DetectWorkflowIsTriggered) return; // Do not start queueing before the first Detect

        if (TimerRinging(TimerForCapturingNewImageInSeconds))
        {
            //EnqueueNewAbImageAsync();
            EnqueueNewAbImage();
        }
    }

    private async void EnqueueNewAbImageAsync()
    {
        await Task.Run(EnqueueNewAbImageAsyncInternal);
    }

    private async Task EnqueueNewAbImageAsyncInternal() //todo: delete if synchronous method operates well
    {
        lock (_lock)
        {
            if (_adjustedAbImageBuffer.Count >= MaxElementsInBuffer) // Dequeue the oldest batch from the queue if the max capacity has been exceeded
            {
                for (var i = 0; i < BatchSize; i++)
                {
                    _adjustedAbImageBuffer.Dequeue();
                }
            }

            _adjustedAbImageBuffer.Enqueue(GetCurrentAdjustedABImage());
        }
    }

    private void EnqueueNewAbImage()
    {
        lock (_lock)
        {
            if (_adjustedAbImageBuffer.Count >= MaxElementsInBuffer) // Dequeue the oldest batch from the queue if the max capacity has been exceeded
            {
                for (var i = 0; i < BatchSize; i++)
                {
                    _adjustedAbImageBuffer.Dequeue();
                }
            }

            _adjustedAbImageBuffer.Enqueue(GetCurrentAdjustedABImage());
        }
    }


    public AdjustedImage GetCurrentAdjustedABImage()
    {
        return _imageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D, DepthStreamProvider.DepthFrameData);
    }

    #endregion

    #region Populating Buffer

    public bool PopulateBatchWithNewImages()
    {
        if (_adjustedAbImageBuffer.Count < BatchSize) return false;

        lock (_lock)
        {

            NewAdjustedAbImageBatch = new List<AdjustedImage>();

            for (var i = 0; i < BatchSize; i++)
            {
                NewAdjustedAbImageBatch.Add(_adjustedAbImageBuffer.Dequeue());
            }

            return true;
        }
    }

    #endregion

    private bool TimerRinging(float timerValueInSeconds)
    {
        if (!(Time.time - _timeLastUpdatedProcessedImages > timerValueInSeconds)) return false;
        
        _timeLastUpdatedProcessedImages = Time.time;
        return true;
    }
}

