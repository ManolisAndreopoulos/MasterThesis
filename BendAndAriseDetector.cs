using System;
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

    private double StartVerticalVelocityThreshold = 0.06;
    private double VerticalDisplacementThresholdInMeters = 0.3; //todo: change this based on experiments
    private const int HeadTwistAngleThreshold = 30; //todo: change this based on experiments

    private Vector3 _headPosition = Vector3.zero;
    private Vector3 _headOrthogonalPosition = Vector3.zero;
    private Vector3 _headEulerAngles = Vector3.zero;
    private Vector3 _headLinearVelocity = Vector3.zero;

    private Vector3 _headOrthogonalPositionAtStartOfBend = Vector3.zero;

    private int _bendAndAriseCount = 0;

    private CameraToOrthogonalPosition _converter = new CameraToOrthogonalPosition();

    private float _verticalTravelDistanceForDebugging;

    public void ResetBendAndAriseDetection()
    {
        _bendAndAriseCount = 0;
        StartedBend = false;
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

    private string GetDebuggingText()
    {
        return $"Bends: {_bendAndAriseCount}\n\n" +
               $"Vert Displ Threshold:{VerticalDisplacementThresholdInMeters}\n"+
               $"Position:\n" +
               $"x:{_headPosition.x:F3}, y:{_headPosition.y:F3}, z:{_headPosition.z:F3}\n" +
               $"Rotation:\n" +
               $"x:{_headEulerAngles.x:F3}, y:{_headEulerAngles.y:F3}, z:{_headEulerAngles.z:F3}";
    }

    private void CheckForAction()
    {
        if (!StartedBend && CompletedPreviousBend && Math.Abs(_headLinearVelocity.y) > StartVerticalVelocityThreshold)
        {
            StartedBend = true;
            _headOrthogonalPositionAtStartOfBend = _headPosition;
        }

        if (StartedBend)
        {
            //if (_headOrthogonalPositionAtStartOfBend == Vector3.zero)
            //{
            //    StartedBend = false;
            //    return;
            //}
            
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            _verticalTravelDistanceForDebugging = verticalTravelDistance;
            if (verticalTravelDistance < -VerticalDisplacementThresholdInMeters) //Bend
            {
                CompletedPreviousBend = false;
            }
        }

        if (!CompletedPreviousBend)
        {
            var verticalTravelDistance = _headOrthogonalPosition.y - _headOrthogonalPositionAtStartOfBend.y;
            _verticalTravelDistanceForDebugging = verticalTravelDistance;
            if (verticalTravelDistance > VerticalDisplacementThresholdInMeters) //Arise
            {
                StartedBend = false;
                CompletedPreviousBend = true;
                _bendAndAriseCount++;
                _headOrthogonalPositionAtStartOfBend = Vector3.zero;
            }
        }
    }

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
}