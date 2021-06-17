using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
    // Start is called before the first frame update
    public Button btn1;
    public Button btn2;
    public Button btn1_2;
    public Button btn2_2;
    public GameObject p1;
    public GameObject p2;
    public GameObject p1_2;
    public GameObject p2_2;

    private bool _trigger1;
    private bool _trigger2;
    private bool _loadingScene;
    private bool _wasBreaking1;
    private bool _wasBreaking2;

    public void LoadRacingScene() {
        SceneManager.LoadScene(
            "Racing on better road"); // we will need to rename our final scene we choose just to "Racing scene"
    }

    public void Start() {
        // These are used to prevent accepting pulse maintained from previous scene
        _wasBreaking1 = PortReading.IsBreaking(1);
        _wasBreaking2 = PortReading.IsBreaking(2);
    }

    public void Update() {
        float r = 1;
        float g = 0;
        float b = 0;
        ColorBlock colorVar = btn1.colors;
        ColorBlock colorVar2 = btn2.colors;
        colorVar.normalColor = new Color(r, g, b);
        colorVar2.normalColor = new Color(r, g, b);

        var p1Pressed = PortReading.IsBreaking(1);
        var p2Pressed = PortReading.IsBreaking(2);

        if (PortReading.IsHRConnected(1)) {
            if (p1.activeSelf) p1.SetActive(false);
            if (p1_2.activeSelf) p1_2.SetActive(false);
        } else {
            if (!p1.activeSelf) p1.SetActive(true);
            if (!p1_2.activeSelf) p1_2.SetActive(true);
        }
        
        if (PortReading.IsHRConnected(2)) {
            if (p2.activeSelf) p2.SetActive(false);
            if (p2_2.activeSelf) p2_2.SetActive(false);
        } else {
            if (!p2.activeSelf) p2.SetActive(true);
            if (!p2_2.activeSelf) p2_2.SetActive(true);
        }
        
        if (!_trigger1 && !_wasBreaking1 && p1Pressed) {
            _trigger1 = true;
            btn1.colors = colorVar;
            btn1_2.colors = colorVar;
        }

        if (!_trigger2 && !_wasBreaking2 && p2Pressed) {
            _trigger2 = true;
            btn2.colors = colorVar2;
            btn2_2.colors = colorVar2;
        }

        _wasBreaking1 = p1Pressed;
        _wasBreaking2 = p2Pressed;

        if (_trigger1 && _trigger2 && !_loadingScene) {
            LoadRacingScene();
            _loadingScene = true;
        }
    }
}