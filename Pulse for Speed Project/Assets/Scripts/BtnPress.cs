using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class BtnPress : MonoBehaviour
{
    public Button display1_Btn1;
    public Button display1_Btn2;
    public Button display2_Btn1;
    public Button display2_Btn2;

    public Color newColor;

    bool p1PressBtn = false;
    bool p2PressBtn = false;

    void Start()
    {
        StartCoroutine(ForceSwitch());
    }

    // Update is called once per frame
    void Update()
    {
        float r = 0;
        float g = 0;
        float b = 0;
        ColorBlock colorVar = display1_Btn1.colors;
        colorVar.normalColor = new Color(r, g, b);

        if (!p1PressBtn && PortReading.IsBreaking(1)) {
            p1PressBtn = true;
            display1_Btn1.colors = colorVar;
            display2_Btn1.colors = colorVar;
        }

        if (!p2PressBtn && PortReading.IsBreaking(2)) {
            p2PressBtn = true;
            display1_Btn2.colors = colorVar;
            display2_Btn2.colors = colorVar;
        }

            if (p1PressBtn && p2PressBtn)
        {
            LoadLandingScene();
        }
    }

public void LoadLandingScene()
    {
       SceneManager.LoadScene("Landing Scene");
    }

    IEnumerator ForceSwitch()
    {
        yield return new WaitForSeconds(3*60);
        LoadLandingScene();
    }
}
