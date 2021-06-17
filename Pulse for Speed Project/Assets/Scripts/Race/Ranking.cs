using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ranking : MonoBehaviour
{
    // Positions of the last race for each player, [1, 0] would mean
    // the first player was 2nd and the 2nd player was first. If no previous race is null
    public static int[] sLastResults;
    public static string ToOrdinal(int num) {
        if (num <= 0) return num.ToString();

        switch (num % 100) {
            case 11:
            case 12:
            case 13:
                return $"{num}th";
        }

        switch (num % 10) {
            case 1:
                return $"{num}:a";
            case 2:
                return $"{num}:a";
            case 3:
                return $"{num}rd";
            default:
                return $"{num}th";
        }
    }

    public Text[] texts;

    private CarController[] _carControllers;
    private LapCounter[] _lapCounters;
    private PlayersAndDistancesComparer _comparer = new PlayersAndDistancesComparer();
    private int[] _textIndices;

    // Start is called before the first frame update
    void Start() {
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
        
        _textIndices = new int[_carControllers.Length];
        for (var i = 0; i < _carControllers.Length; ++i) {
            _textIndices[_carControllers[i].player - 1] = i < texts.Length ? i : -1;
        }
    }

    public int GetPlayerPosition(int player) {
        var positions = GetPlayerPositions();
        int idx = -1;
        for (var i = 0; i < positions.Length; ++i) {
            if (_carControllers[i].player == player) {
                idx = i;
                break;
            }
        }

        if (idx == -1) return -1;
        return positions[idx];
    }
    
    // Update is called once per frame
    void Update() {
        var positions = GetPlayerPositions();
        for (var i = 0; i < positions.Length; ++i) {
            // Update text if possible
            if (_textIndices[i] >= 0) {
                texts[_textIndices[i]].text = ToOrdinal(positions[i]);
            }
        }
    }

    private int[] GetPlayerPositions() {
        var playersPositions = new PlayerPosition[_carControllers.Length];
        for (var i = 0; i < playersPositions.Length; ++i) {
            playersPositions[i] = new PlayerPosition(
                i, _lapCounters[i].Laps, _carControllers[i].AchievedDistanceAlongRoadPath);
        }

        Array.Sort(playersPositions, _comparer);
        var positionIdx = 0; // Used to account for ties
        var result = new int[playersPositions.Length];
        for (var i = 0; i < playersPositions.Length; ++i) {
            var position = playersPositions[i];
            // Increment position only if this one is not overlapping the other
            if (i == 0 || !position.IsSamePosition(playersPositions[i - 1])) {
                positionIdx += 1;
            }
            result[position.playerIndex] = positionIdx;
        }

        return result;
    }

    private class PlayerPosition
    {
        public readonly int playerIndex;
        public readonly int laps;
        public readonly float distanceInLap;

        public PlayerPosition(int playerIndex, int laps, float distanceInLap) {
            this.playerIndex = playerIndex;
            this.laps = laps;
            this.distanceInLap = distanceInLap;
        }

        public bool IsSamePosition(PlayerPosition other) {
            if (other.laps != laps) return false;
            return Mathf.Abs(distanceInLap - other.distanceInLap) < 0.001f;
        }
    }

    private class PlayersAndDistancesComparer : IComparer<PlayerPosition>
    {
        public int Compare(PlayerPosition x, PlayerPosition y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return -1;
            if (ReferenceEquals(null, x)) return 1;
            var lapsComparison = -x.laps.CompareTo(y.laps);
            if (lapsComparison != 0) return lapsComparison;
            return -x.distanceInLap.CompareTo(y.distanceInLap);
        }
    }
}