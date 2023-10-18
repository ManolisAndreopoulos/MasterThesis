using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using TMPro;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ImuStreamProvider : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
#endif
    private float[] accelSampleData = null;
    private float[] gyroSampleData = null;

    public Vector3 AccelerationVector;
    public Vector3 GyroEulerAngle;

    public TextMeshPro ImuOutputText = null;

    private string _accelerationOutputTextBuffer = string.Empty;
    private string _gyroscopeOutputTextBuffer = string.Empty;


    //Timer
    private float _timeLastUpdateImuStream;
    private const float TimerForNewImuStreamInSeconds = 1f; // refresh rate for the output

    void Start()
    {
#if ENABLE_WINMD_SUPPORT
        researchMode = new HL2ResearchMode();
        researchMode.InitializeAccelSensor();
        researchMode.InitializeGyroSensor();
        researchMode.InitializeMagSensor();

        researchMode.StartAccelSensorLoop();
        researchMode.StartGyroSensorLoop();
        researchMode.StartMagSensorLoop();
#endif
        _timeLastUpdateImuStream = Time.time;
    }

    void Update()
    {
        ImuOutputText.text = _accelerationOutputTextBuffer + _gyroscopeOutputTextBuffer;
    }

    void LateUpdate()
    {
#if ENABLE_WINMD_SUPPORT
        // update Acceleration Sample
        if (researchMode.AccelSampleUpdated())
        {
            accelSampleData = researchMode.GetAccelSample();
        }

        // update Gyro Sample
        if (researchMode.GyroSampleUpdated())
        {
            gyroSampleData = researchMode.GetGyroSample();
        }

#endif
        // Convert to Vector3
        AccelerationVector = CreateAccelVector(accelSampleData);
        GyroEulerAngle = CreateGyroEulerAngle(gyroSampleData);

        //Create output strings
        _accelerationOutputTextBuffer = $"Accel: {AccelerationVector[0]:F3}, {AccelerationVector[1]:F3}, {AccelerationVector[2]:F3}\n";
        _gyroscopeOutputTextBuffer = $"Gyro  : {GyroEulerAngle[0]:F3}, {GyroEulerAngle[1]:F3}, {GyroEulerAngle[2]:F3}";

        // Visualize corrected values
        //RefImuVisualize.AccelVector = AccelerationVector * 0.1f;
        //RefImuVisualize.GyroEulorAngle = GyroEulerAngle * 30.0f;
    }

    private Vector3 CreateAccelVector(float[] accelSample)
    {
        Vector3 vector = Vector3.zero;
        if ((accelSample?.Length ?? 0) == 3)
        {
            // Positive directions
            //  accelSample[0] : Down direction
            //  accelSample[1] : Back direction
            //  accelSample[2] : Right direction
            vector = new Vector3(
                accelSample[2],
                -1.0f * accelSample[0],
                -1.0f * accelSample[1]
                ); //todo: use this vector
        }
        return vector;
    }

    private Vector3 CreateGyroEulerAngle(float[] gyroSample)
    {
        Vector3 vector = Vector3.zero;
        if ((gyroSample?.Length ?? 0) == 3)
        {
            // Axis of rotation
            //  gyroSample[0] : Unity Y axis(Plus)
            //  gyroSample[1] : Unity Z axis(Plus)
            //  gyroSample[2] : Unity X axis(Plus)
            vector = new Vector3(
                gyroSample[2],
                gyroSample[0],
                gyroSample[1]
                );
        }
        return vector;
    }

    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
        researchMode.StopAllSensorDevice();
#endif
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }

    private bool TimerRinging()
    {
        if (!(Time.time - _timeLastUpdateImuStream > TimerForNewImuStreamInSeconds)) return false;

        _timeLastUpdateImuStream = Time.time;
        return true;
    }

}