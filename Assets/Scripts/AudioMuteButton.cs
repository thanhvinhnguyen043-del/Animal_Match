using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class AudioMuteButton : MonoBehaviour
{
    public AudioChannel channel;
    public Image background;
    public TextMeshProUGUI stateText;
    public Sprite onSprite;
    public Sprite offSprite;
    public Color onColor = new Color(.25f, .95f, .5f);
    public Color offColor = new Color(1f, .3f, .36f);

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void OnEnable() { Refresh(); }

    private void Toggle()
    {
        var audio = FindObjectOfType<AudioSettingsUI>();
        if (audio == null) return;
        audio.ToggleMute(channel);
        Refresh();
    }

    public void Refresh()
    {
        bool muted = AudioSettingsUI.IsMuted(channel);
        if (background != null)
        {
            var sprite=muted ? offSprite : onSprite;
            if(sprite) { background.sprite=sprite; background.color=Color.white; }
            else background.color=muted ? offColor : onColor;
        }
        if (stateText != null) stateText.text = muted ? "OFF" : "ON";
    }
}
