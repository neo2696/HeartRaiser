using System;
using UnityEngine;
using UnityEngine.UI;

public class LapCounter : MonoBehaviour
{
    /// <summary>
    /// Waypoints to check lap completion, the last checkpoint must be the end of the lap one
    /// </summary>
    public Collider[] waypointColliders;

    public Text display;
    public int totalLaps;

    private int _nextWaypoint;
    private int _laps;

    public int Laps => _laps;

    public void ResetLaps() {
        _laps = 0;
    }

    private void Start() {
        _nextWaypoint = 0;
        if (waypointColliders.Length == 0) {
            Debug.LogError(
                $"Please set the waypoints for the lap counter of player {GetComponent<CarController>().player}");
        }

        UpdateDisplayText();
        if (!display) Debug.LogWarning($"No lap display configured for player {GetComponent<CarController>().player}");
    }

    private void OnTriggerEnter(Collider other) {
        if (other != waypointColliders[_nextWaypoint]) return;
        _nextWaypoint += 1;
        if (_nextWaypoint >= waypointColliders.Length) {
            // Lap complete
            _laps += 1;
            _nextWaypoint = 0;
            UpdateDisplayText();
        }
    }

    private void UpdateDisplayText() {
        if (display) display.text = $"Varv: {Mathf.Min(_laps + 1, totalLaps)} / {totalLaps}";
    }
}