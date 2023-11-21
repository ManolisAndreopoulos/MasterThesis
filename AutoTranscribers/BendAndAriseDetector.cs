using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class BendAndAriseDetector : MonoBehaviour
{
    /*
     * Coordinate System:
     * Linear:
     *  x -> Right Direction
     *  y -> Up Direction
     *  z -> Forward Direction
     * Rotation (Quaternion):
     *  x -> Rotation (head) to down direction
     *  y -> Rotation (body) to right direction
     *  z -> Rotation (neck) to left direction
     */

    public TextMeshPro DebuggingText = null; //assigned in Unity
    public Camera HololensCamera = null; //assigned in Unity
  
    public bool StartedBend { get; private set; }
    public bool CompletedPreviousBend { get; private set; } = true;

    private double StartVerticalVelocityThreshold = 0.35;
    private double VerticalDisplacementThresholdInMeters = 0.2; //todo: change this based on experiments

    private Vector3 _headPosition = Vector3.zero;
    private Vector3 _headOrthogonalPosition = Vector3.zero;
    private Vector3 _headEulerAngles = Vector3.zero;
    private Vector3 _headLinearVelocity = Vector3.zero;

    private Vector3 _headOrthogonalPositionAtStartOfBend = Vector3.zero;

    // Action count
    private int _bendAndAriseCount = 0;

    private readonly CameraToOrthogonalPosition _converter = new CameraToOrthogonalPosition();

    private float _verticalTravelDistanceForDebugging;

    // Max Bend duration before discarding
    private Stopwatch _stopwatch;
    private const int BendTimeThresholdInMilliseconds = 2000;

    public void ResetBendAndAriseDetection()
    {
        _bendAndAriseCount = 0;
        StartedBend = false;
        CompletedPreviousBend = true;
        _verticalTravelDistanceForDebugging = 0;
    }

    public void IncreaseStartVelocity()
    {
        StartVerticalVelocityThreshold += 0.01;
    }

    public void DecreaseStartVelocity()
    {
        StartVerticalVelocityThreshold -= 0.01;
    }

    public void IncreaseVerticalDisplacementThreshold()
    {
        VerticalDisplacementThresholdInMeters += 0.1;
    }

    public void DecreaseVerticalDisplacementThreshold()
    {
        VerticalDisplacementThresholdInMeters -= 0.1;
    }

    void FixedUpdate()
    {
        UpdateLinearVelocityOfHead();
        UpdateCurrentPositionOfHead();
        UpdateCurrentRotationOfHead();
        UpdateCurrentOrthogonalPositionOfHead();
    }

    void Update()
    {
        CheckForAction();
        DebuggingText.text = GetDebuggingText();
    }

    private void CheckForAction()
    {
        // Start of Bend
        if (!StartedBend && CompletedPreviousBend && _headLinearVelocity.y < -StartVerticalVelocityThreshold)
        {
            StartedBend = true;
            CompletedPreviousBend = true;
            _headOrthogonalPositionAtStartOfBend = _headOrthogonalPosition;

            ResetTimer();
        }

        //End of Bend
        if (StartedBend)
        {
            if (CheckTimerHasPassed())
            {
                //Return back to "Start of Bend" detection
                StartedBend = false;
                CompletedPreviousBend = true;
            }
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            _verticalTravelDistanceForDebugging = verticalTravelDistance; //todo: Delete
            if (verticalTravelDistance < -VerticalDisplacementThresholdInMeters) //Bend
            {
                StartedBend = false; // so that the code does not enter this if statement again for this Bend&Arise Cycle
                CompletedPreviousBend = false;
            }
        }

        // End of Arise
        if (!CompletedPreviousBend)
        {
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            _verticalTravelDistanceForDebugging = verticalTravelDistance; //todo: Delete
            if (Math.Abs(verticalTravelDistance) < VerticalDisplacementThresholdInMeters) //Arise
            {
                StartedBend = false;
                CompletedPreviousBend = true;
                _bendAndAriseCount++;
                _headOrthogonalPositionAtStartOfBend = Vector3.zero;
            }
        }
    }

    #region Debugging Message Generation

    private string GetDebuggingText()
    {
        return $"Bends: {_bendAndAriseCount}\n\n" +
               $"Vert Vel Thres:{StartVerticalVelocityThreshold}\n" +
               $"Vert Displ Thre:{VerticalDisplacementThresholdInMeters}\n\n" +
               $"Velocity:\n" +
               $"x:{_headLinearVelocity.x:F3}, y:{_headLinearVelocity.y:F3}, z:{_headLinearVelocity.z:F3}\n" +
               $"VertTravel Dist:{_verticalTravelDistanceForDebugging}\n";
    }

    private string GetStatesOfFlowchart()
    {
        return $"StartedBend: {StartedBend}\n CompletedPrevBend: {CompletedPreviousBend}\n";
    }

    #endregion

    #region Stopwatch Methods

    private bool CheckTimerHasPassed()
    {
        if (_stopwatch == null) return true;

        if (_stopwatch.ElapsedMilliseconds < BendTimeThresholdInMilliseconds)
        {
            return false;
        }

        _stopwatch = null;
        return true;
    }

    private void ResetTimer()
    {
        _stopwatch = Stopwatch.StartNew();
    }

    #endregion

    #region Update Methods

    private void UpdateCurrentRotationOfHead()
    {
        _headEulerAngles = HololensCamera.transform.eulerAngles;
    }

    private void UpdateCurrentPositionOfHead()
    {
        _headPosition = HololensCamera.transform.position;
    }

    private void UpdateCurrentOrthogonalPositionOfHead()
    {
        _headOrthogonalPosition = _converter.ConvertToOrthogonalPosition(_headPosition, _headEulerAngles);
    }

    private void UpdateLinearVelocityOfHead()
    {
        _headLinearVelocity.x = (HololensCamera.transform.position.x - _headPosition.x) / Time.fixedDeltaTime;
        _headLinearVelocity.y = (HololensCamera.transform.position.y - _headPosition.y) / Time.fixedDeltaTime;
        _headLinearVelocity.z = (HololensCamera.transform.position.z - _headPosition.z) / Time.fixedDeltaTime;
    }

    #endregion
}