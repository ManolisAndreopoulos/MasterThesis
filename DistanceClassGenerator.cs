using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DistanceClassGenerator : MonoBehaviour
{
    /*
     * Coordinate System:
     * Linear:
     *  x -> Right Direction
     *  y -> Up Direction
     *  z -> Forward Direction
     * Rotation:
     *  x -> Rotation (head) to down direction
     *  y -> Rotation (body) to right direction
     *  z -> Rotation (neck) to left direction
     */


    public Camera HololensCamera = null;


    public DistanceClass GetDistanceClassFromPixel(PixelDepth pixelDepth)
    {
        // Convert the depth from cm to meters.
        var depthInMeters = pixelDepth.Depth / 100.0f;

        // Convert the pixel position into normalized viewport coordinates (range: 0 to 1).
        float viewportX = pixelDepth.PixelPositionX / 512;
        float viewportY = pixelDepth.PixelPositionY / 512;

        // Convert to a position where z is the depth from the camera.
        var viewportPosition = new Vector3(viewportX, viewportY, depthInMeters);

        // Convert the viewport position to world coordinates.
        var worldPosition = HololensCamera.ScreenToWorldPoint(viewportPosition);

        // Calculate the depth compared to the HoloLens Camera
        var distanceInCm = (int) Vector3.Distance(worldPosition, HololensCamera.transform.position) * 100;

        var distanceClass = new DistanceClass(worldPosition, distanceInCm);

        return distanceClass;
    }

    private string GetDebuggingMessageWithHoloLensTransform()
    {
        var position = HololensCamera.transform.position;
        var rotation = HololensCamera.transform.rotation;

        var message = $"Pos: {position.x:F3}, {position.y:F3}, {position.z:F3}\n"
                            + $"Rot: {rotation.x:F3}, {rotation.y:F3}, {rotation.z:F3}";

        return message;
    }
}

public class DistanceClass
{
    public Vector3 WorldPosition { get; private set; }
    public int DistanceInCm { get; private set; }
    public int DistanceClassMtm { get; private set; }

    private static List<int> mtmDistanceClasses = new List<int> {5, 15, 30, 45, 80};

    public DistanceClass(Vector3 worldPosition, int distanceInCm)
    {
        WorldPosition = worldPosition;
        DistanceInCm = distanceInCm;
        DistanceClassMtm = FindClosestMtmDistanceClass(DistanceInCm);
    }

    static int FindClosestMtmDistanceClass(int actualDistance)
    {
        // Find the closest value using LINQ
        var closestValue = mtmDistanceClasses.Aggregate((x, y) => Math.Abs(x - actualDistance) < Math.Abs(y - actualDistance) ? x : y);

        return closestValue;
    }


}