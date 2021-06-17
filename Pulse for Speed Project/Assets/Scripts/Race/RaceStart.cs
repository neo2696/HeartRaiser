using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceStart : MonoBehaviour
{
    public float timeLeft = 3.0f;
    public Text[] texts; // used for showing countdown from 3, 2, 1 
    private CarController[] _carControllers;

    void Start() {
        var players = GameObject.FindGameObjectsWithTag("Player");
        var others = new List<CarController>();
        foreach (var player in players) {
            var controller = player.GetComponent<CarController>();
            if (controller) others.Add(controller);
        }

        _carControllers = others.ToArray();

        foreach (var carController in _carControllers) {
            carController.FreezeCar();
        }

        UpdateTexts();
    }


    void Update() {
        if (Time.time < 1f) return; // 1 extra second to wait to be in place
        timeLeft -= Time.deltaTime;
        UpdateTexts();

        if (timeLeft < 1) {
            foreach (var carController in _carControllers) {
                carController.UnfreezeCar();
            }
            
            PortReading.RecordPlayerStats(); // Record stats

            foreach (var text in texts) {
                text.enabled = false;
            }

            this.enabled = false; // Disable this component, not needed anymore
        }
    }

    private void UpdateTexts() {
        foreach (var text in texts) {
            text.text = timeLeft.ToString("F0");
        }
    }
}