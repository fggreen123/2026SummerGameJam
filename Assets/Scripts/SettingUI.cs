using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    void Start()
    {
        float bgm = PlayerPrefs.GetFloat("BGM");
        float sfx = PlayerPrefs.GetFloat("SFX");

        bgmSlider.value = bgm;
        sfxSlider.value = sfx;

        AudioManager.instance.SetBgmVolume(bgm);
        AudioManager.instance.SetSfxVolume(sfx);

        bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    void OnBgmChanged(float value)
    {
        AudioManager.instance.SetBgmVolume(value);
    }

    void OnSfxChanged(float value)
    {
        AudioManager.instance.SetSfxVolume(value);
    }
}
