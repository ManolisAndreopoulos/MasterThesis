using TMPro;
using UnityEngine;

public class WorldPositionGenerator : MonoBehaviour
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


    public TextMeshPro DebuggingText = null;
    public Camera HololensCamera = null;

    void Update()
    {
        //DebuggingText.text = GetDebuggingMessageWithHoloLensTransform();
    }

    public Vector3 GetWorldPositionFromPixel(PixelDepth pixelDepth)
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

        return worldPosition;
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