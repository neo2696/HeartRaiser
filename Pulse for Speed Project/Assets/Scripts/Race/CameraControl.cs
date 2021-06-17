// Smooth Follow from Standard Assets
// Converted to C# because I fucking hate UnityScript and it's inexistant C# interoperability
// If you have C# code and you want to edit SmoothFollow's vars ingame, use this instead.

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private int playerFollowed;

    // The distance in the x-z plane to the target
    public float distance = 10.0f;

    // the height we want the camera to be above the target
    public float height = 5.0f;

    // How much we 
    public float heightDamping = 2.0f;
    public float rotationDamping = 3.0f;

    // The target we are following
    private Transform _target;
    private Transform[] _otherPlayers;
    private Camera _camera;

    public int FollowedPlayer => playerFollowed;

    public void FollowPlayer(int playerNum) {
        var players = GameObject.FindGameObjectsWithTag("Player");
        var others = new List<Transform>();
        foreach (var player in players) {
            if (player.GetComponent<CarController>().player == playerNum) {
                _target = player.transform;
            } else {
                others.Add(player.transform);
            }
        }

        _otherPlayers = others.ToArray();
        playerFollowed = playerNum;
    }
    
    private void Start() {
        _camera = GetComponent<Camera>();
        FollowPlayer(playerFollowed);
    }

    void LateUpdate() {
        // Early out if we don't have a target
        if (!_target) return;

        // Hide players which are too close
        foreach (var player in _otherPlayers) {
            var visible = Vector3.Distance(player.position, _target.position) > .5f;
            if (visible) _camera.cullingMask |= (1 << player.gameObject.layer);
            else _camera.cullingMask &= ~(1 << player.gameObject.layer);
        }

        // Calculate the current rotation angles
        float wantedRotationAngle = _target.eulerAngles.y;
        float wantedHeight = _target.position.y + height;

        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // Damp the rotation around the y-axis
        currentRotationAngle =
            Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);

        // Damp the height
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Convert the angle into a rotation
        var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

        // Set the position of the camera on the x-z plane to:
        // distance meters behind the target
        transform.position = _target.position;
        transform.position -= currentRotation * Vector3.forward * distance;

        // Set the height of the camera
        transform.position = new Vector3(transform.position.x, currentHeight, transform.position.z);

        // Always look at the target
        transform.LookAt(_target);
    }
}