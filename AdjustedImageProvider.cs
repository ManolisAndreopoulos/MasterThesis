﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdjustedImageProvider : MonoBehaviour
{
    [SerializeField] private DepthStreamProvider DepthStreamProvider = null;
    [SerializeField] private RawImage AbRawImage;


    //public event Action ReadyForAnalysis;
    public List<AdjustedImage> NewAdjustedAbImageBatch { get; private set; } = new List<AdjustedImage>();

    private const int BatchSize = 4;
    private const float TimerForCapturingNewImageInSeconds = 0.25f;
    private const int MaxElementsInBuffer = 40;


    private Queue<AdjustedImage> _adjustedAbImageBuffer;
    private float _timeLastUpdatedProcessedImages;
    private readonly object _lock = new object();

    // Start is called before the first frame update
    void Start()
    {
        _timeLastUpdatedProcessedImages = Time.time;
        _adjustedAbImageBuffer = new Queue<AdjustedImage>();
    }

    // Update is called once per frame
    void Update()
    {
        if (TimerRinging(TimerForCapturingNewImageInSeconds))
        {
            if (_adjustedAbImageBuffer.Count == MaxElementsInBuffer)
            {
                for (var i = 0; i < BatchSize; i++)
                {
                    _adjustedAbImageBuffer.Dequeue();
                }
            }
            _adjustedAbImageBuffer.Enqueue(GetCurrentAdjustedABImage());
        }
    }

    public void PopulateBatchWithNewImages()
    {
        if (_adjustedAbImageBuffer.Count < BatchSize) return;

        lock (_lock)
        {

            NewAdjustedAbImageBatch = new List<AdjustedImage>();

            for (var i = 0; i < BatchSize; i++)
            {
                NewAdjustedAbImageBatch.Add(_adjustedAbImageBuffer.Dequeue());
            }
        }
        //ReadyForAnalysis.Invoke();
    }

    private bool TimerRinging(float timerValueInSeconds)
    {
        if (!(Time.time - _timeLastUpdatedProcessedImages > timerValueInSeconds)) return false;
        
        _timeLastUpdatedProcessedImages = Time.time;
        return true;
    }

    private AdjustedImage GetCurrentAdjustedABImage()
    {
        var imageUtilities = new ImageUtilities();
        return imageUtilities.AdjustBrightnessContrastAndRotate(AbRawImage.texture as Texture2D, DepthStreamProvider.DepthFrameData);
    }
}

