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

    public TextMeshPro DebuggingText = null; //assigned in Unity
    public Camera HololensCamera = null; //assigned in Unity
    public string StepSide { get; private set; } = "unknown"; //"right", "left" or "unknown"
    public bool Started { get; private set; }

    private double StartVerticalVelocityThreshold = 0.035;
    private const int HeadTwistAngleThreshold = 5; //todo: change this based on experiments
    private  double MinimumHorizontalDisplacementInMeters = 0.05;
    private  double MaximumVerticalDisplacementInMeters = 0.1;

    private Vector3 _headPosition = Vector3.zero;
    private Vector3 _headOrthogonalPosition = Vector3.zero;
    private Vector3 _headEulerAngles = Vector3.zero;
    private Vector3 _headLinearVelocity = Vector3.zero;

    private Vector3 _headPositionAtStartOfStep = Vector3.zero;
    private Vector3 _headOrthogonalPositionAtStartOfStep = Vector3.zero;

    private int _stepCount = 0;
    private float _horizontalPlaneTravelDistanceForDebugging;
    private float _verticalTravelDistanceForDebugging;

    //Step Cycle stopwatch
    private Stopwatch _stopwatch;
    private int StepCycleTimeThresholdInMilliseconds = 300; // Adds a small delay between Start and Stop of steps, to ensure not more than 1 step is transcribed each time

    private CameraToOrthogonalPosition _converter = new CameraToOrthogonalPosition();

    public void ResetStepDetection()
    {
        _stepCount = 0;
        Started = false;
        _horizontalPlaneTravelDistanceForDebugging = 0;
        _verticalTravelDistanceForDebugging = 0;
    }

    //public void Increase()
    //{
    //    //MinimumHorizontalDisplacementInMeters += 0.01;
    //    //StepCycleTimeThresholdInMilliseconds += 50;
    //    MaximumVerticalDisplacementInMeters += 0.02;
    //}

    //public void Decrease()
    //{
    //    //MinimumHorizontalDisplacementInMeters -= 0.01;
    //    //StepCycleTimeThresholdInMilliseconds -= 50;
    //    MaximumVerticalDisplacementInMeters -= 0.02;
    //}

    //public void IncreaseStepStartVelocity()
    //{
    //    StartVerticalVelocityThreshold += 0.005;
    //}

    //public void DecreaseStepStartVelocity()
    //{
    //    StartVerticalVelocityThreshold -= 0.005;
    //}

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

    private string GetDebuggingText()
    {
        return $"Steps: {_stepCount}\n\n" +
               //$"Velocity:\n" +
               //$"x:{_headLinearVelocity.x:F2}, y:{_headLinearVelocity.y:F2}, z:{_headLinearVelocity.z:F2}\n" +
               //$"Rotation:\n" +
               //$"x:{_headEulerAngles.x:F1}, y:{_headEulerAngles.y:F1}, z:{_headEulerAngles.z:F1}\n" +
               $"Orthogonal Position:\n" +
               $"x:{_headOrthogonalPosition.x:F1}, y:{_headOrthogonalPosition.y:F1} , z: {_headOrthogonalPosition.z:F1}\n" +
               $"Vertical Travel Dist:{_verticalTravelDistanceForDebugging}\n" +
               //$"Travel Distance:{_horizontalPlaneTravelDistanceForDebugging}\n" +

               //THRESHOLDS
               $"Vel Thres:{StartVerticalVelocityThreshold}\n" +
               //$"Dist Thres:{MinimumHorizontalDisplacementInMeters}\n" +
               //$"Time Thres:{StepCycleTimeThresholdInMilliseconds}\n";
               $"Vertic Thres:{MaximumVerticalDisplacementInMeters}\n";
    }

    private void CheckForAction()
    {
        if (!CheckTimerHasPassed()) return;

        // Determine step start
        if (!Started && Math.Abs(_headLinearVelocity.y) > StartVerticalVelocityThreshold)
        {
            Started = true;
            _headPositionAtStartOfStep = _headPosition;
            _headOrthogonalPositionAtStartOfStep = _headOrthogonalPosition;
        }

        // Determine step side
        if (Started && StepSide == "unknown")
        {
            if (_headEulerAngles.z > HeadTwistAngleThreshold)
                StepSide = "left";
            else if (_headEulerAngles.z < 360-HeadTwistAngleThreshold)
                StepSide = "right";
            else
                StepSide = "unknown";
        }

        if (Started && Math.Abs(_headLinearVelocity.y) < StartVerticalVelocityThreshold)
        {
            Started = false;

            // Checking vertical displacement
            if (_headOrthogonalPositionAtStartOfStep != Vector3.zero)
            {
                var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfStep.y;
                _verticalTravelDistanceForDebugging = verticalTravelDistance;
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
                _horizontalPlaneTravelDistanceForDebugging = horizontalPlaneTravelDistance.magnitude;
                if (horizontalPlaneTravelDistance.magnitude > MinimumHorizontalDisplacementInMeters)
                {
                    //todo: Transcribe step, otherwise discard
                    _stepCount++;
                }
                ResetTimer();
            }

            _headPositionAtStartOfStep = Vector3.zero;
            _headOrthogonalPositionAtStartOfStep = Vector3.zero;
        }
    }

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