using UnityEngine;
using UnityEngine.UI;

public class ScrollPulse : MonoBehaviour
{
    /// <summary>
    /// Only relevant when adaptToHeartrate is -1
    /// </summary>
    public float speed;

    /// <summary>
    /// Set to -1 if no adaptation is desired
    /// </summary>
    [Min(-1)] public int adaptToHeartrate = -1;

    private RawImage _image;

    // Start is called before the first frame update
    void Start() {
        // Fetch the RawImage component from the gameobject
        _image = gameObject.GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update() {
        var spd = speed;
        if (adaptToHeartrate > 0) {
            // The image has 4 peaks, so speed 1 = 4 beats/sec = 4 * 60 bpm = 240bpm
            spd = PortReading.IsHRConnected(adaptToHeartrate) ? 
                PortReading.HeartRate(adaptToHeartrate) / 240f :
                0; // do not move if disconnected
        }

        // scroll raw image
        var uvRect = _image.uvRect;
        uvRect = new Rect(
            uvRect.x + Time.unscaledDeltaTime * spd,
            uvRect.y,
            uvRect.width,
            uvRect.height
        );
        _image.uvRect = uvRect;
    }
}