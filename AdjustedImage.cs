using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class AdjustedImage
{
    public byte[] InBytes { get; }
    [CanBeNull] public Texture2D Texture { get; }
    public ushort[] DepthMap { get; }
    public DateTime TimeImageWasCaptured { get; }
    public string ImageTitle { get; set; }

    public AdjustedImage(byte[] inBytes, Texture2D texture, ushort[] depthMap)
    {
        InBytes = inBytes;
        Texture = texture;
        DepthMap = depthMap;
        TimeImageWasCaptured = DateTime.Now;
        ImageTitle = "Image" + TimeImageWasCaptured.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss':'f") + ".png";
    }

    public AdjustedImage(byte[] inBytes, string imageTitle, DateTime dateTime)
    {
        InBytes = inBytes;
        DepthMap = null;
        Texture = null;
        TimeImageWasCaptured = dateTime;
        ImageTitle = imageTitle;
    }
}