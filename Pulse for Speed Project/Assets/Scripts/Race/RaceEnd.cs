using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaceEnd : MonoBehaviour
{
    public GameObject endGamePrefab;

    private Ranking _ranking;
    private CarController[] _carControllers;
    private LapCounter[] _lapCounters;
    private CameraControl[] _cameraControls;
    private Transform[] _canvas;
    private bool[] _finished;
    private uint _nFinished = 0;
    private int[] _finalPositions;

    void Start() {
        FindCarControllersAndLapCounters();
        FindCamerasAndCanvas();
        _ranking = GetComponent<Ranking>();
        _finished = new bool[_carControllers.Length];
        _finalPositions = new int[_carControllers.Length];
        for (var i = 0; i < _finished.Length; ++i) _finished[i] = false;
        _nFinished = 0;

        if (_ranking == null) {
            Debug.LogError("Can't find Ranking component on the GameObject with the RaceEnd component!");
        }
    }

    void Update() {
        for (var i = 0; i < _lapCounters.Length; ++i) {
            if (_finished[i]) continue; // Already finished
            if (_lapCounters[i].Laps >= _lapCounters[i].totalLaps) {
                _carControllers[i].FreezeCar();

                var position = _ranking.GetPlayerPosition(_carControllers[i].player);

                if (endGamePrefab) {
                    var endGame = Instantiate(endGamePrefab, _canvas[i]);
                    var text = endGame.transform.Find("PositionText").GetComponent<Text>();
                    text.text = Ranking.ToOrdinal(position);
                } else Debug.LogWarning("No endGamePrefab in Race End");
                
                PortReading.StopRecordingPlayerStats(_carControllers[i].player);
                
                _finished[i] = true;
                _nFinished += 1;
                _finalPositions[_carControllers[i].player - 1] = position - 1; // 1-based to 0-based

                StartCoroutine(FollowOtherCarAfterSomeTime(i));
            }
        }

        if (_nFinished == _carControllers.Length) {
            // All finished
            Ranking.sLastResults = _finalPositions;
            SceneManager.LoadScene("Final Scene");
        }
    }

    private IEnumerator FollowOtherCarAfterSomeTime(int i) {
        yield return new WaitForSeconds(1.0f);
        var toFollow = FirstNonFinishedPlayer();
        if (toFollow >= 0)
            _cameraControls[i].FollowPlayer(toFollow);
    }

    private int FirstNonFinishedPlayer() {
        for (var i = 0; i < _finished.Length; ++i) {
            if (!_finished[i]) return _carControllers[i].player;
        }

        return -1;
    }

    private void FindCamerasAndCanvas() {
        if (_carControllers == null) {
            Debug.LogError("Should call FindCarControllersAndLapCounters before FindCamerasAndCanvas.");
            return;
        }

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera");
        var cameraControls = new List<CameraControl>();
        var canvases = new List<Transform>();
        foreach (var camera in cameras) {
            var control = camera.GetComponent<CameraControl>();
            var canvas = camera.GetComponent<Transform>().GetChild(0);
            cameraControls.Add(control);
            canvases.Add(canvas);
        }

        // Sort so they are all aligned
        _canvas = new Transform[canvases.Count];
        _cameraControls = new CameraControl[cameraControls.Count];
        for (var i = 0; i < cameraControls.Count; ++i) {
            var playerIdx = FindPlayerIdx(cameraControls[i].FollowedPlayer);
            _canvas[playerIdx] = canvases[i];
            _cameraControls[playerIdx] = cameraControls[i];
        }
    }

    private void FindCarControllersAndLapCounters() {
        // Get players by tag and obtain controllers and lap counters
        var players = GameObject.FindGameObjectsWithTag("Player");
        var carControllers = new List<CarController>();
        var lapCounters = new List<LapCounter>();
        foreach (var player in players) {
            var carController = player.GetComponent<CarController>();
            var lapCounter = player.GetComponent<LapCounter>();
            if (lapCounter && carController) {
                carControllers.Add(carController);
                lapCounters.Add(lapCounter);
            }
        }

        _carControllers = carControllers.ToArray();
        _lapCounters = lapCounters.ToArray();
    }

    private int FindPlayerIdx(int player) {
        for (var i = 0; i < _carControllers.Length; ++i) {
            if (_carControllers[i].player == player) {
                return i;
            }
        }

        return -1;
    }
}