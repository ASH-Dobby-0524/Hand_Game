using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleAudio : MonoBehaviour
{
    // 소리 재생
    public void audioPlay(AudioSource audio) {
        audio.Play();
    }

    // 게임 시작 시, BGM을 확 끄지 않고 점점 줄이기
    public void gameStart() {
        AudioManager.Instance.easesoundOff();
    }
}
