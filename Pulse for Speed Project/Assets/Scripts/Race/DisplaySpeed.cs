using UnityEngine;
using UnityEngine.UI;

public class DisplaySpeed : MonoBehaviour
{
    public Text displayText;
    public float scale;
    private Rigidbody _car;
    void Start() {
        _car = GetComponent<Rigidbody>();
    }

    void Update() {
        displayText.text = $"{Mathf.RoundToInt(scale * _car.velocity.magnitude)} km/h";
    }
}
