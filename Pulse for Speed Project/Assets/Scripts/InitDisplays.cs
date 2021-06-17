using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitDisplays : MonoBehaviour
{
    void Start() {
        for (int i = 1; i < Display.displays.Length && i < 2; i++) {
            Display.displays[i].Activate();
        }
    }
}