using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using TMPro;

public enum AudioChannel { Master, Music, SFX }

public class AudioSettingsUI : MonoBehaviour
{
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;

    private FMOD.Studio.Bus masterBus;
    private FMOD.Studio.Bus musicBus;
    private FMOD.Studio.Bus sfxBus;

    private void Start()
    {
        masterBus = RuntimeManager.GetBus("bus:/");
        musicBus = RuntimeManager.GetBus("bus:/Music");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");

        Initialize(masterSlider, "Volume.Master", SetMaster);
        Initialize(musicSlider, "Volume.Music", SetMusic);
        Initialize(sfxSlider, "Volume.SFX", SetSFX);
        ApplyMute(AudioChannel.Master);
        ApplyMute(AudioChannel.Music);
        ApplyMute(AudioChannel.SFX);
    }

    public static string MuteKey(AudioChannel channel) => "Volume." + channel + ".Muted";

    public static bool IsMuted(AudioChannel channel) => PlayerPrefs.GetInt(MuteKey(channel), 0) == 1;

    public void ToggleMute(AudioChannel channel)
    {
        PlayerPrefs.SetInt(MuteKey(channel), IsMuted(channel) ? 0 : 1);
        ApplyMute(channel);
        PlayerPrefs.Save();
    }

    private void ApplyMute(AudioChannel channel)
    {
        if (channel == AudioChannel.Master)
        {
            // FMOD reapplies its global mute state every frame in the Editor.
            // Set that state through its API so the master bus is not unmuted again.
            RuntimeManager.MuteAllEvents(IsMuted(channel));
            return;
        }
        var bus = channel == AudioChannel.Music ? musicBus : sfxBus;
        bus.setMute(IsMuted(channel));
    }

    private void Initialize(
        Slider slider,
        string key,
        UnityEngine.Events.UnityAction<float> callback)
    {
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.wholeNumbers = false;

        float value = Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1));
        slider.SetValueWithoutNotify(value);
        callback(value);
        slider.onValueChanged.AddListener(callback);
    }

    public void SetMaster(float value)
    {
        value = Mathf.Clamp01(value);
        masterBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume.Master", value);
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void SetMusic(float value)
    {
        value = Mathf.Clamp01(value);
        musicBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume.Music", value);
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void SetSFX(float value)
    {
        value = Mathf.Clamp01(value);
        sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("Volume.SFX", value);
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(SetMaster);
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(SetMusic);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(SetSFX);
    }
}
