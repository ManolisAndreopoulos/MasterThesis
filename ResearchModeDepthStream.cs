using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ResearchModeDepthStream : MonoBehaviour, ISensorStreamProvider
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    [SerializeField] bool enablePointCloud = true;
    private bool startRealtimePreview = true;

    // depth texture parameters
    public GameObject depthPreviewPlane = null;
    private Material depthMediaMaterial = null;
    private Texture2D depthMediaTexture = null;
    private byte[] depthTextureFrameData = null;
    
    // depth map parameter
    private ushort[] depthFrameData = null;
    public ushort[] DepthFrameData => depthFrameData;

    #region ISensorStreamProvider

    public GameObject SensorStream
    {
        get => depthPreviewPlane;
        set => depthPreviewPlane = value;
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
        if (depthPreviewPlane != null)
        {
            depthMediaMaterial = depthPreviewPlane.GetComponent<MeshRenderer>().material;
            depthMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
            depthMediaMaterial.mainTexture = depthMediaTexture;
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
        if (startRealtimePreview && depthPreviewPlane != null && researchMode.DepthMapTextureUpdated())
        {
            // update depth map texture
            byte[] frameTexture = researchMode.GetDepthMapTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (depthTextureFrameData == null)
                {
                    depthTextureFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, depthTextureFrameData, 0, depthTextureFrameData.Length);
                }

                depthMediaTexture.LoadRawTextureData(depthTextureFrameData);
                depthMediaTexture.Apply();
            }

            // update depth map
            depthFrameData = researchMode.GetDepthMapBuffer();
        }
#endif
    }
}