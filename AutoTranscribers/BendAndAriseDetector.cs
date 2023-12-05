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

    public TextMeshPro BendAndAriseDetectorOutputText = null; //assigned in Unity
    public Camera HololensCamera = null; //assigned in Unity
    [Header("Table Manager")]
    public TableManager TableManager = null;

    //Algorithm Thresholds
    private double StartVerticalVelocityThreshold = 0.35;
    private double VerticalDisplacementThresholdInMeters = 0.2; //todo: change this based on experiments

    //Algorithm States
    private Vector3 _headPosition = Vector3.zero;
    private Vector3 _headOrthogonalPosition = Vector3.zero;
    private Vector3 _headEulerAngles = Vector3.zero;
    private Vector3 _headLinearVelocity = Vector3.zero;
    private Vector3 _headOrthogonalPositionAtStartOfBend = Vector3.zero;
    private bool _startedBend;
    private bool _completedPreviousBend = true;

    //Counter
    private int _bendAndAriseCount;

    //Bend and Arise Cycle stopwatch
    private Stopwatch _stopwatch;
    private const int BendTimeThresholdInMilliseconds = 2000; // Max Bend duration before discarding

    private readonly CameraToOrthogonalPosition _converter = new CameraToOrthogonalPosition();

    private bool _toDetect = false;

    public void Run()
    {
        _toDetect = true;
    }

    public void Stop()
    {
        _toDetect = false;
        TableManager.StoreMtmAction(new BendAndAriseAction(_bendAndAriseCount));
        BendAndAriseDetectorOutputText.text = $"Total: {_bendAndAriseCount}";
    }

    #region Voice Commands for Tuning Of Algorithm Parameters

    public void ResetBendAndAriseDetection()
    {
        _bendAndAriseCount = 0;
        _startedBend = false;
        _completedPreviousBend = true;
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

    #endregion

    #region Debugging Message Generation

    private string GetDebuggingText()
    {
        return $"Bends: {_bendAndAriseCount}\n\n" +
               $"Vert Vel Thres:{StartVerticalVelocityThreshold}\n" +
               $"Vert Displ Thre:{VerticalDisplacementThresholdInMeters}\n\n" +
               $"Velocity:\n" +
               $"x:{_headLinearVelocity.x:F3}, y:{_headLinearVelocity.y:F3}, z:{_headLinearVelocity.z:F3}\n";
    }

    private string GetStatesOfFlowchart()
    {
        return $"StartedBend: {_startedBend}\n CompletedPrevBend: {_completedPreviousBend}\n";
    }

    #endregion

    void FixedUpdate()
    {
        UpdateLinearVelocityOfHead();
        UpdateCurrentPositionOfHead();
        UpdateCurrentRotationOfHead();
        UpdateCurrentOrthogonalPositionOfHead();
    }

    void Update()
    {
        if (_toDetect)
        {
            BendAndAriseDetectorOutputText.text = $"Bends: {_bendAndAriseCount}";
            CheckForAction();
        }
    }

    private void CheckForAction()
    {
        // Start of Bend
        if (!_startedBend && _completedPreviousBend && _headLinearVelocity.y < -StartVerticalVelocityThreshold)
        {
            _startedBend = true;
            _completedPreviousBend = true;
            _headOrthogonalPositionAtStartOfBend = _headOrthogonalPosition;

            ResetTimer();
        }

        //End of Bend
        if (_startedBend)
        {
            if (CheckTimerHasPassed())
            {
                //Return back to "Start of Bend" detection
                _startedBend = false;
                _completedPreviousBend = true;
            }
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            if (verticalTravelDistance < -VerticalDisplacementThresholdInMeters) //Bend
            {
                _startedBend = false; // so that the code does not enter this if statement again for this Bend&Arise Cycle
                _completedPreviousBend = false;
            }
        }

        // End of Arise
        if (!_completedPreviousBend)
        {
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            if (Math.Abs(verticalTravelDistance) < VerticalDisplacementThresholdInMeters) //Arise
            {
                _startedBend = false;
                _completedPreviousBend = true;
                _bendAndAriseCount++;
                _headOrthogonalPositionAtStartOfBend = Vector3.zero;
            }
        }
    }

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