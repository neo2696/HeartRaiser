using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathCreation;
using UnityEditor.UIElements;

[System.Serializable]
public class AxleInfo
{
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor; // is this wheel attached to motor?
    public bool steering; // does this wheel apply steer angle?
    public bool doesBreak; // does this wheel break?
}

public class CarController : MonoBehaviour
{
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public List<AxleInfo> axleInfos; // the information about each individual axle
    public float maxMotorTorque; // maximum torque the motor can apply to wheel (should change with pulse)
    public uint maxHeartRate;
    public float minMotorTorque;
    public uint minHeartRate;
    public float maxBreakTorque; // maximum break torque
    public float maxSteeringAngle; // maximum steer angle the wheel can have
    public Transform centerOfMass;

    public PathCreator path; // the path the car will follow
    public PathCreator road; // the road so we can put the car back on it and so
    public float samplingStep = .2f; // amount by which we increase the param along the path on each sample

    public int player; // player number, used to identify the input for the break as well as by the camera

    public TrailRenderer trailLeft; // left trail for the wheels drifting
    public TrailRenderer trailRight; // right trail for the wheels drifting

    private Material _breaksMaterial; // material for the breaks to light them up when breaking, saved on start
    private Rigidbody _rigid; // rigid body, saved on start

    private float _aimingAt; // current param (along the path) the car will try to aim at
    private Vector3 _aimedPoint; // point corresponding to the currently-aimed param
    private Vector3 _aimedDir; // direction in which to find the currently-aimed point
    private float _achieved; // last param (along the road) achieved by the car
    private bool _goingBack; // true if we are out of the road and waiting to be teleported back to it

    private Vector3 _lastStuckCheckPos = Vector3.zero; // position of the transform in the last stuck-check
    private float _nextStuckCheck = 0f; // time until next stuck-check

    private uint _heartRate = 60;

    private RigidbodyConstraints _savedRigidbodyConstraints; // Saved when locking
    private bool _locked = false;

    private int _onlyRoadLayer;

    public float AchievedDistanceAlongRoadPath => _achieved;

    public void FreezeCar() {
        if (_locked) return;
        _savedRigidbodyConstraints = _rigid.constraints;
        _rigid.constraints = RigidbodyConstraints.FreezePosition;
        _locked = true;
    }

    public void UnfreezeCar() {
        if (!_locked) return;
        _rigid.constraints = _savedRigidbodyConstraints;
        _locked = false;
    }

    public void Start() {
        _onlyRoadLayer = ~LayerMask.NameToLayer("Road");
        var bodyRenderer = transform.Find("carDisplay/bodymesh").GetComponent<MeshRenderer>();
        _breaksMaterial = bodyRenderer.materials[bodyRenderer.materials.Length - 1]; // Take last material as the breaks
        if (centerOfMass) {
            GetComponent<Rigidbody>().centerOfMass = centerOfMass.localPosition;
        }

        _rigid = GetComponent<Rigidbody>();
        var firstPoint = path.path.GetPoint(0);
        var closest = road.path.GetClosestPointOnPath(firstPoint);
        transform.position = new Vector3(firstPoint.x, closest.y + .5f, firstPoint.z);
        transform.rotation = path.path.GetRotation(0);
        _achieved = _aimingAt = 0;
        _goingBack = false;
        CalculateNextPointToAimAt();
    }

    public void Update() {
        var u = transform.rotation * Vector3.forward;
        var breakAmount = PortReading.IsBreaking(player) ? 1 : 0;
        // Drifting if velocity not aligned with the car / breaking and not stopped
        var drifting = Vector2.Dot(
                           new Vector2(_rigid.velocity.x, _rigid.velocity.z).normalized,
                           new Vector2(u.x, u.z)) < .9f ||
                       (_rigid.velocity.sqrMagnitude > 0.1f && breakAmount > .5f);
        trailLeft.emitting = drifting && axleInfos[1].leftWheel.isGrounded;
        trailRight.emitting = drifting && axleInfos[1].rightWheel.isGrounded;

        // Breaking light
        _breaksMaterial.SetColor(EmissionColor, Color.Lerp(Color.black, new Color(.5f, 0, 0), breakAmount));

        if (!_goingBack && _rigid.constraints != RigidbodyConstraints.FreezePosition) {
            if (Time.time >= _nextStuckCheck) {
                StuckCheck(); // Prevent getting stuck
            }

            // If not on the road, start coroutine to put back in the track
            if (!Physics.Raycast(transform.position, Vector3.down, 1, _onlyRoadLayer)) {
                StartCoroutine(BackToTrack());
                return;
            }

            // Last achieved point on the road (to get back if falling out)
            var closest = road.path.GetClosestDistanceAlongPath(transform.position);
            if ((road.path.GetPointAtDistance(closest) - transform.position).y <= 0) {
                _achieved = closest;
            }

            // Debug for those points
            Debug.DrawLine(transform.position, _aimedPoint, Color.red);
            Debug.DrawLine(transform.position, road.path.GetPointAtDistance(_achieved), Color.blue);
            Debug.DrawRay(transform.position, _rigid.velocity, Color.green);
        }

        _heartRate = PortReading.HeartRate(player);
    }

    /**
     * Periodic checks to check the car is not stuck in something
     */
    public void StuckCheck() {
        if ((_lastStuckCheckPos - transform.position).sqrMagnitude < .5f) {
            StartCoroutine(BackToTrack());
            return;
        }

        _lastStuckCheckPos = transform.position;
        _nextStuckCheck = Time.time + 3.0f;
    }

    /**
     * Waits some seconds and then it puts the car back where it was before leaving the track
     */
    public IEnumerator BackToTrack() {
        _goingBack = true; // Stop everything else, basically
        // Remove steering and motor
        foreach (var axleInfo in axleInfos) {
            if (axleInfo.steering) {
                axleInfo.leftWheel.steerAngle = 0;
                axleInfo.rightWheel.steerAngle = 0;
            }

            if (axleInfo.motor) {
                axleInfo.leftWheel.motorTorque = 0;
                axleInfo.rightWheel.motorTorque = 0;
            }
        }

        yield return new WaitForSeconds(2.0f); // 2 seconds so it can fall and bounce or whatever

        trailLeft.emitting = false;
        trailRight.emitting = false;
        transform.position = road.path.GetPointAtDistance(_achieved) + Vector3.up * .5f;
        transform.rotation = road.path.GetRotationAtDistance(_achieved);

        // reset _aimingAt back to the point
        _aimingAt = path.path.GetClosestDistanceAlongPath(transform.position);
        _rigid.isKinematic = true; // may help with the wheels having forces and shit

        yield return new WaitForSeconds(.5f);
        _rigid.isKinematic = false;
        _rigid.velocity = Vector3.zero;
        _rigid.angularVelocity = Vector3.zero;
        _goingBack = false;
    }

    public void FixedUpdate() {
        // Avoid if we are going to go back to track
        if (_goingBack || _rigid.constraints == RigidbodyConstraints.FreezePosition)
            return;

        var motorFactor = (Mathf.Clamp(_heartRate, minHeartRate, maxHeartRate) - minHeartRate)
                          / (maxHeartRate - minHeartRate); // Factor in of the heart rate from 0 = min to 1 = max
        var motor = motorFactor * (maxMotorTorque - minMotorTorque) + minMotorTorque; // factor applied to torque

        var breakAmount = PortReading.IsBreaking(player) ? 1f : 0f;
        var breakTorque = maxBreakTorque * breakAmount;
        var u = transform.rotation * Vector3.forward;

        CalculateNextPointToAimAt(); // Calculates the next point to aim at by the car
        var dir = _aimedPoint - transform.position;
        var targetAngle = Vector3.SignedAngle(u, dir, Vector3.up);
        // Tried with smoothed steering angles but the results seem to be worse, to be honest
        var steerAngle = Mathf.Clamp(targetAngle, -maxSteeringAngle, maxSteeringAngle);

        foreach (AxleInfo axleInfo in axleInfos) {
            if (axleInfo.steering) {
                axleInfo.leftWheel.steerAngle = steerAngle;
                axleInfo.rightWheel.steerAngle = steerAngle;
            }

            if (axleInfo.motor) {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }

            if (axleInfo.doesBreak) {
                axleInfo.leftWheel.brakeTorque = breakTorque;
                axleInfo.rightWheel.brakeTorque = breakTorque;
            }
        }
    }

    /**
     * Determines how "smooth" the entrance would be at the given point of the path
     * if entering straight following the vector fromPosToEntrance, which is
     * expected to be the vector going from the current position to the entrance point.
     */
    private float EntranceSmoothness(float aimingAt, Vector3 fromPosToEntrance) {
        var tangent = path.path.GetDirectionAtDistance(aimingAt);
        var tangent2d = new Vector2(tangent.x, tangent.z).normalized;
        var dir2d = new Vector2(fromPosToEntrance.x, fromPosToEntrance.z).normalized;
        return Vector2.Dot(dir2d, tangent2d); // By now we estimate by the cos
    }

    /**
     * Calculate the next point of the path the car should try to aim at
     */
    private void CalculateNextPointToAimAt() {
        var pos = transform.position;

        var aiming = _aimingAt;
        var nextAiming = aiming + samplingStep;

        var currentPoint = path.path.GetPointAtDistance(aiming);
        var nextPoint = path.path.GetPointAtDistance(nextAiming);

        var currentDir = currentPoint - pos;
        var nextDir = nextPoint - pos;

        var currentDirSqrMagn = currentDir.sqrMagnitude;
        var nextDirSqrMagn = nextDir.sqrMagnitude;

        var currentSmoothness = EntranceSmoothness(aiming, currentDir);
        var nextSmoothness = EntranceSmoothness(nextAiming, nextDir);

        // Repeat, sampling the path always forward
        while (
            nextDirSqrMagn < currentDirSqrMagn // If the next sampled point is going to be closer to the car
            || currentDirSqrMagn < 25 // If the distance from the car is too short (avoid too tight steering)
            || nextSmoothness > currentSmoothness // If the next point has a smoother entrance than current one
        ) {
            aiming = nextAiming;
            currentPoint = nextPoint;
            currentDir = nextDir;
            currentSmoothness = nextSmoothness;
            currentDirSqrMagn = nextDirSqrMagn;

            nextAiming = aiming + samplingStep;
            nextPoint = path.path.GetPointAtDistance(nextAiming);
            nextDir = nextPoint - pos;
            nextSmoothness = EntranceSmoothness(nextAiming, nextDir);
            nextDirSqrMagn = nextDir.sqrMagnitude;
        }

        _aimingAt = aiming;
        _aimedPoint = currentPoint;
        _aimedDir = currentDir;
    }
}