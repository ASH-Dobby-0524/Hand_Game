using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class ScrollAudio : MonoBehaviour
{

    public AudioMixer audioMixer;
    public Scrollbar sfxaudioSlider;
    public Scrollbar bgmaudioSlider;

    public Text high;

    public void Start() {
        high.text = PlayerPrefs.GetInt("HighScore").ToString("N0");
        audioMixer.SetFloat("SFX", 0.0f);
        audioMixer.SetFloat("BGM", 0.0f);
    }

    public void BGMControl(string name) {
        float sound = bgmaudioSlider.value;
        if (sound == 0) audioMixer.SetFloat(name, 0);
        audioMixer.SetFloat(name, Mathf.Lerp(-30, 0, sound));
    }

    public void SFXControl(string name) {
        float sound = sfxaudioSlider.value;
        if (sound == 0) audioMixer.SetFloat(name, 0);
        audioMixer.SetFloat(name, Mathf.Lerp(-30, 0, sound));
    }

    public void ResetControl() {
        audioMixer.SetFloat("SFX", 0.0f);
        audioMixer.SetFloat("BGM", 0.0f);
        sfxaudioSlider.value = 1.0f;
        bgmaudioSlider.value = 1.0f;
        PlayerPrefs.SetInt("HighScore", 0);
        high.text = PlayerPrefs.GetInt("HighScore").ToString("N0");
    }

}
