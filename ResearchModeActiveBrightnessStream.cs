using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UI;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ResearchModeActiveBrightnessStream : MonoBehaviour, ISensorStreamProvider
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    public GameObject shortAbImagePreviewPlane = null;
    public RawImage AbImage = null;
    //public Texture2D AbImageTexture = null;

    [SerializeField] bool enablePointCloud = true;

    private bool startRealtimePreview = true;
    private Material shortAbImageMediaMaterial = null;
    private byte[] shortAbImageFrameData = null;

    //[SerializeField] public CameraUtilities camera = null;
    

    #region ISensorStreamProvider

    public GameObject SensorStream
    {
        get => shortAbImagePreviewPlane;
        set => shortAbImagePreviewPlane = value;
    }

    #endregion

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
        IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
        //unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;
#else
        IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#endif
#endif
    }

    // Start is called before the first frame update
    void Start()
    {
        if (shortAbImagePreviewPlane != null)
        {
            shortAbImageMediaMaterial = shortAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
            AbImage.texture = new Texture2D(512, 512, TextureFormat.Alpha8, false); //For AB image
            //AbImage.texture = new Texture2D(512, 512, TextureFormat.RGB24, false); // For main camera
            //AbImageTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
            shortAbImageMediaMaterial.mainTexture = AbImage.texture;
            //shortAbImageMediaMaterial.mainTexture = AbImageTexture;
        }

#if ENABLE_WINMD_SUPPORT
        researchMode = new HL2ResearchMode();

        // Depth sensor should be initialized in only one mode
        researchMode.InitializeDepthSensor();

        researchMode.InitializeSpatialCamerasFront();
        researchMode.SetReferenceCoordinateSystem(unityWorldOrigin);
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor should be initialized in only one mode
        researchMode.StartDepthSensorLoop(enablePointCloud);
        // researchMode.StartSpatialCamerasFrontLoop();

#endif
    }

    // Update is called once per frame
    void LateUpdate()
    {
#if ENABLE_WINMD_SUPPORT
        // update short-throw AbRawImage texture
        if (startRealtimePreview && shortAbImagePreviewPlane != null && researchMode.ShortAbImageTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetShortAbImageTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (shortAbImageFrameData == null)
                {
                    shortAbImageFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, shortAbImageFrameData, 0, shortAbImageFrameData.Length);
                }

                (AbImage.texture as Texture2D).LoadRawTextureData(shortAbImageFrameData);
                //AbImageTexture.LoadRawTextureData(shortAbImageFrameData);
                (AbImage.texture as Texture2D).Apply();
                //AbImageTexture.Apply();
            }
        }

#endif
    }

    /*
    private byte[] _frameTexture;
    private int count;

    //todo: Try to connect the main camera instead to see if the analysis is proper. The reason is to see if the problem is in the interconnection or in the black-white images with low brightness
    void Update()
    {
        if (count == 1000) //to avoid the frame rate being higher than the camera can handle
        {
            count = 0;
            if (startRealtimePreview && shortAbImagePreviewPlane != null)
            {
                CapturePhoto();

                if (_frameTexture.Length > 0)
                {
                    if (shortAbImageFrameData == null)
                    {
                        shortAbImageFrameData = _frameTexture;
                    }
                    else
                    {

                        System.Buffer.BlockCopy(_frameTexture, 0, shortAbImageFrameData, 0, shortAbImageFrameData.Length);
                    }
                    (AbImage.texture as Texture2D).LoadRawTextureData(shortAbImageFrameData);
                    //AbImageTexture.LoadRawTextureData(shortAbImageFrameData);
                    (AbImage.texture as Texture2D).Apply();
                    //AbImageTexture.Apply();
                }
            }
            camera.StopCamera();
        }
        count++;
    }

    private async void CapturePhoto()
    {
        camera.StartCamera();
        _frameTexture = await camera.TakePhoto();
    }
    */
}
