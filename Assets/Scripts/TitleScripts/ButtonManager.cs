using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    GameObject nowPanel = null;

    // 게임시작 버튼, 누르면 게임시작 패널이 나옴
    public void gameStart_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    // 게임설정 버튼, 누르면 게임설정 패널이 나옴
    public void gameSetting_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    // 플레이방법 버튼, 누르면 플레이 방법 패널이 나옴
    public void gameHowToPlay_Button(GameObject obj) {
        back_Button();
        LeanTween.moveLocalX(obj, -400f, 0.3f).setEaseInOutCirc();
        nowPanel = obj;
    }

    // 게임종료 버튼, 누르면 게임이 꺼짐
    public void gameEnd_Button() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // 왼쪽 아래의 뒤로가기 버튼, 누르면 현재 패널이 사라짐
    public void back_Button() {
        if (nowPanel != null) { 
            LeanTween.moveLocalX(nowPanel, -1500f, 0.3f).setEaseOutCirc(); 
        }
    }



}
