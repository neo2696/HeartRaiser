using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseOnDisconnection : MonoBehaviour
{
    public GameObject[] otherIsDisconnectedPanels;
    public Text[] otherIsDisconnectedText;
    public GameObject[] youAreDisconnectedPanel;

    void Start() {
        foreach (var panel in otherIsDisconnectedPanels) {
            panel.SetActive(false);
        }
    }

    void Update() {
        var disconnectedPlayers = new List<int>();
        for (var player = 1; player <= PortReading.PlayersCount; ++player) {
            if (!PortReading.IsHRConnected(player)) {
                disconnectedPlayers.Add(player);
            }
        }

        if (disconnectedPlayers.Count > 0) {
            var text = disconnectedPlayers[0].ToString();
            for (var i = 1; i < disconnectedPlayers.Count; ++i) {
                if (i < disconnectedPlayers.Count - 1) {
                    text += ", " + disconnectedPlayers[i];
                } else {
                    text += " och " + disconnectedPlayers[i];
                }
            }

            text += " frånkopplade";
            for (var player = 1; player <= PortReading.PlayersCount; ++player) {
                var others = otherIsDisconnectedPanels[player-1];
                var you = youAreDisconnectedPanel[player-1];
                if (!PortReading.IsHRConnected(player)) {
                    if (others.activeSelf) others.SetActive(false);
                    if (!you.activeSelf) you.SetActive(true);
                } else {
                    if (!others.activeSelf) {
                        others.SetActive(true);
                        otherIsDisconnectedText[player-1].text = text;
                    }
                    if (you.activeSelf) you.SetActive(false);
                }
            }

            Time.timeScale = 0f;
        } else if (Time.timeScale == 0f) {
            for (var i = 0; i < PortReading.PlayersCount; ++i) {
                var others = otherIsDisconnectedPanels[i];
                var you = youAreDisconnectedPanel[i];
                if (others.activeSelf) others.SetActive(false);
                if (you.activeSelf) you.SetActive(false);
            }

            Time.timeScale = 1f;
        }
    }
}