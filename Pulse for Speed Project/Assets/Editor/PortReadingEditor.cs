using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PortReading))]
[CanEditMultipleObjects]
public class PortReadingEditor : Editor
{
    private SerializedProperty ports, baudRate;
    private bool showingPorts = true;
    private bool showingPlayers = true;

    private void Awake() {
        EditorApplication.update += AskToRepaint;
    }

    private void AskToRepaint() {
        Repaint();
    }

    void OnEnable() {
        ports = serializedObject.FindProperty("ports");
        baudRate = serializedObject.FindProperty("baudRate");
    }

    private static string[] GetPortNameList() {
        string[] portNames;
        switch (Application.platform) {
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxPlayer:
                portNames = System.IO.Ports.SerialPort.GetPortNames();
                if (portNames.Length == 0) {
                    portNames = System.IO.Directory.GetFiles("/dev/");
                }

                break;
            default: // Windows
                portNames = System.IO.Ports.SerialPort.GetPortNames();
                break;
        }

        var withFake = new string[portNames.Length + 1];
        portNames.CopyTo(withFake, 0);
        withFake[withFake.Length - 1] = PortReading.FakePortName;
        return withFake;
    }

    private static string GetDefaultPortName() {
        var portNames = GetPortNameList();

        switch (Application.platform) {
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.LinuxPlayer:
                foreach (var portName in portNames) {
                    if (portName.StartsWith("/dev/tty.usb") || portName.StartsWith("/dev/ttyUSB"))
                        return portName;
                }

                return null;
            default: // Windows
                portNames = System.IO.Ports.SerialPort.GetPortNames();
                return portNames.Length > 0 ? portNames[0] : null;
        }
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();
        // Players
        var players = Mathf.Max(EditorGUILayout.IntField("Players", ports.arraySize), 1);
        if (players < ports.arraySize) {
            for (var i = players; i < ports.arraySize; i++) {
                ports.DeleteArrayElementAtIndex(i);
            }
        } else if (players > ports.arraySize) {
            for (var i = ports.arraySize; i < players; i++) {
                ports.InsertArrayElementAtIndex(i);
                ports.GetArrayElementAtIndex(i).stringValue = GetDefaultPortName();
            }
        }

        // Baud rate
        EditorGUILayout.PropertyField(baudRate);

        // Ports
        var portNames = GetPortNameList();
        var options = new string[portNames.Length];
        portNames.CopyTo(options, 0);
        for (var i = 0; i < options.Length; ++i) {
            options[i] = options[i].TrimStart('/');
        }

        for (var i = 0; i < ports.arraySize; ++i) {
            var idx = Array.IndexOf(portNames, ports.GetArrayElementAtIndex(i).stringValue);
            var newIdx = EditorGUILayout.Popup("Port P" + (i + 1), idx, options);
            if (newIdx != idx) {
                ports.GetArrayElementAtIndex(i).stringValue = portNames[newIdx];
            }
        }

        EditorGUILayout.Separator();
        showingPorts = EditorGUILayout.Foldout(showingPorts, "Ports");
        if (showingPorts) {
            for (var i = 0; i < PortReading.PortsCount; ++i) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(PortReading.Port(i));
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(PortReading.IsPortOpen(i) ? "open" : "closed");
                EditorGUILayout.LabelField($"last: {PortReading.PortLastHeartRate(i)}");
                if (!PortReading.IsPortHRStable(i)) {
                    EditorGUILayout.LabelField("Unstable Hear-Rate!");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        showingPlayers = EditorGUILayout.Foldout(showingPlayers, "Players");
        if (showingPlayers) {
            const int w = 40;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HR", GUILayout.Width(2*w));
            EditorGUILayout.LabelField("Brk", GUILayout.Width(w));
            EditorGUILayout.LabelField("Max", GUILayout.Width(w));
            EditorGUILayout.LabelField("Avg", GUILayout.Width(w));
            EditorGUILayout.EndHorizontal();
            for (var i = 1; i <= PortReading.PlayersCount; ++i) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Toggle(PortReading.IsHRConnected(i), GUILayout.Width(w));
                EditorGUILayout.LabelField(PortReading.HeartRate(i).ToString(), GUILayout.Width(w));
                EditorGUILayout.Toggle(PortReading.IsBreaking(i), GUILayout.Width(w));
                EditorGUILayout.LabelField(PortReading.MaxHeartRate(i).ToString(), GUILayout.Width(w));
                EditorGUILayout.LabelField(PortReading.AverageHeartRate(i).ToString(), GUILayout.Width(w));
                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}