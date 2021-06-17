using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class DisplayBpm : MonoBehaviour
{
    [Min(1)] public int player;

    private Text _text;

    void Start() {
        _text = GetComponent<Text>();
    }

    void Update() {
        var bpm = PortReading.HeartRate(player);
        // bpm == 0 => not known or invalid
        if (bpm > 0 && PortReading.IsHRConnected(player)) {
            _text.text = $"{bpm} bpm";
        } else {
            _text.text = "...";
        }
    }
}