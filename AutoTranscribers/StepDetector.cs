using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class StepDetector : MonoBehaviour
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

    public TextMeshPro StepDetectorOutputText = null; //assigned in Unity
    public Camera HololensCamera = null; //assigned in Unity

    [Header("Table Manager")]
    public TableManager TableManager = null;

    //Algorithm Thresholds
    private double StartVerticalVelocityThreshold = 0.035;
    private const int HeadTwistAngleThreshold = 5; //todo: change this based on experiments
    private  double MinimumHorizontalDisplacementInMeters = 0.05;
    private  double MaximumVerticalDisplacementInMeters = 0.1;

    //Algorithm States
    private Vector3 _headPosition = Vector3.zero;
    private Vector3 _headOrthogonalPosition = Vector3.zero;
    private Vector3 _headEulerAngles = Vector3.zero;
    private Vector3 _headLinearVelocity = Vector3.zero;
    private Vector3 _headPositionAtStartOfStep = Vector3.zero;
    private Vector3 _headOrthogonalPositionAtStartOfStep = Vector3.zero;
    private string _stepSide = "Unknown"; //"right", "left" or "unknown"
    private bool _started;

    //Counters
    private int _stepCount = 0;
    private int _leftSideStepCount = 0;
    private int _rightSideStepCount = 0;
    private int _unknownSideStepCount = 0;

    //Step Cycle stopwatch
    private Stopwatch _stopwatch;
    private int StepCycleTimeThresholdInMilliseconds = 300; // Adds a small delay between Start and Stop of steps, to ensure not more than 1 step is transcribed each time

    private CameraToOrthogonalPosition _converter = new CameraToOrthogonalPosition();

    private bool _toDetect = false;

    public void Run()
    {
        _toDetect = true;
    }

    public void Stop()
    {
        _toDetect = false;
        TableManager.StoreMtmAction(new StepAction(_leftSideStepCount, _rightSideStepCount, _unknownSideStepCount));
        StepDetectorOutputText.text = $"Total: {_stepCount}\n"+
                                      $"Left: {_leftSideStepCount}\n"+
                                      $"Right: {_rightSideStepCount}\n"+
                                      $"Unknown: {_unknownSideStepCount}";
    }

    #region Voice Commands for Tuning Of Algorithm Parameters

    public void ResetStepDetection()
    {
        _stepCount = 0;
        _started = false;
    }


    private string GetDebuggingText()
    {
        return $"Steps: {_stepCount}\n\n" +
               //THRESHOLDS
               $"Vel Thres:{StartVerticalVelocityThreshold}\n" +
               $"Dist Thres:{MinimumHorizontalDisplacementInMeters}\n\n" +

               //$"Velocity:\n" +
               //$"x:{_headLinearVelocity.x:F2}, y:{_headLinearVelocity.y:F2}, z:{_headLinearVelocity.z:F2}\n" +
               //$"Rotation:\n" +
               //$"x:{_headEulerAngles.x:F1}, y:{_headEulerAngles.y:F1}, z:{_headEulerAngles.z:F1}\n" +
               $"Orthogonal Position:\n" +
               $"x:{_headOrthogonalPosition.x:F1}, y:{_headOrthogonalPosition.y:F1} , z: {_headOrthogonalPosition.z:F1}\n";
        //$"VertTravel Dist:{_verticalTravelDistanceForDebugging}\n";
        //$"Time Thres:{StepCycleTimeThresholdInMilliseconds}\n";
        //$"Vertic Thres:{MaximumVerticalDisplacementInMeters}\n";
    }

    //public void IncreaseMaximumVerticalDisplacement()
    //{
    //    MaximumVerticalDisplacementInMeters += 0.01;
    //}

    //public void DecreaseMaximumVerticalDisplacement()
    //{
    //    MaximumVerticalDisplacementInMeters -= 0.01;
    //}

    public void IncreaseMinimumHorizontalDisplacement()
    {
        MinimumHorizontalDisplacementInMeters += 0.01;
    }

    public void DecreaseMinimumHorizontalDisplacement()
    {
        MinimumHorizontalDisplacementInMeters -= 0.01;
    }

    public void IncreaseStepStartVelocity()
    {
        StartVerticalVelocityThreshold += 0.005;
    }

    public void DecreaseStepStartVelocity()
    {
        StartVerticalVelocityThreshold -= 0.005;
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
            StepDetectorOutputText.text = $"Steps: {_stepCount}";
            CheckForAction();
        }
    }

    private void CheckForAction()
    {
        if (!CheckTimerHasPassed()) return;

        // Determine step start
        if (!_started && Math.Abs(_headLinearVelocity.y) > StartVerticalVelocityThreshold)
        {
            _started = true;
            _headPositionAtStartOfStep = _headPosition;
            _headOrthogonalPositionAtStartOfStep = _headOrthogonalPosition;
        }

        // Determine step side
        //todo: not used yet
        //if (_started)
        //{
        //    if (_headEulerAngles.z > HeadTwistAngleThreshold)
        //    {
        //        _stepSide = "Left";
        //        _leftSideStepCount++;
        //    }
        //    else if (_headEulerAngles.z < 360 - HeadTwistAngleThreshold)
        //    {
        //        _stepSide = "Right";
        //        _rightSideStepCount++;
        //    }
        //    else
        //    {
        //        _stepSide = "Unknown";
        //        _unknownSideStepCount++;
        //    }
        //}

        if (_started && Math.Abs(_headLinearVelocity.y) < StartVerticalVelocityThreshold)
        {
            _started = false;

            // Checking vertical displacement
            if (_headOrthogonalPositionAtStartOfStep != Vector3.zero)
            {
                var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfStep.y;
                if (Math.Abs(verticalTravelDistance) > MaximumVerticalDisplacementInMeters)
                {
                    // In case of big vertical displacement, we will not consider it as a step (might be bend and arise instead)
                    _headPositionAtStartOfStep = Vector3.zero;
                    _headOrthogonalPositionAtStartOfStep = Vector3.zero;
                    return;
                }
            }

            // Checking horizontal displacement
            if (_headPositionAtStartOfStep != Vector3.zero)
            {
                //todo: maybe always use the orthogonal vector
                var horizontalPlaneTravelDistance = Vector2.zero;
                horizontalPlaneTravelDistance.x = _headPosition.x - _headPositionAtStartOfStep.x;
                horizontalPlaneTravelDistance.y = _headPosition.z - _headPositionAtStartOfStep.z;
                if (horizontalPlaneTravelDistance.magnitude > MinimumHorizontalDisplacementInMeters)
                {
                    //Transcribe step, otherwise discard
                    if (_headEulerAngles.z > HeadTwistAngleThreshold)
                    {
                        _stepSide = "Left";
                        _leftSideStepCount++;
                    }
                    else if (_headEulerAngles.z < 360 - HeadTwistAngleThreshold)
                    {
                        _stepSide = "Right";
                        _rightSideStepCount++;
                    }
                    else
                    {
                        _stepSide = "Unknown";
                        _unknownSideStepCount++;
                    }

                    _stepCount++;
                }
                ResetTimer();
            }

            _headPositionAtStartOfStep = Vector3.zero;
            _headOrthogonalPositionAtStartOfStep = Vector3.zero;
        }
    }

    #region Stopwatch Methods

    private bool CheckTimerHasPassed()
    {
        if (_stopwatch == null) return true;

        if (_stopwatch.ElapsedMilliseconds < StepCycleTimeThresholdInMilliseconds)
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

    private void UpdateCurrentRotationOfHead()
    {
        _headEulerAngles = HololensCamera.transform.eulerAngles;
    }

    private void UpdateCurrentPositionOfHead()
    {
        _headPosition = HololensCamera.transform.position;
        _headOrthogonalPosition = _converter.ConvertToOrthogonalPosition(_headPosition, _headEulerAngles);
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
}