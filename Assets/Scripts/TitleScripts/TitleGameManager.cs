using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleGameManager : MonoBehaviour
{
    public GameObject introUIPrefab;   // Prefab drag & drop
    GameObject ui;

    private static bool isInitialized = false;

    // 진짜 게임시작 버튼


    private void Awake() {
        if (isInitialized) return;  // 이미 로드된 경우 무시

        ui = Instantiate(introUIPrefab);
        DontDestroyOnLoad(ui);

        isInitialized = true;  // 중복 생성 방지
    }
    public void GAMESTART() {
        GameObject targetObject = GameObject.Find("Intro(Clone)");
        targetObject.GetComponent<Animator>().Rebind();
        SceneManager.LoadScene("RPS");
    }


    public Text high;

    // 게임Scene이 이동할 때, 애니메이션이 출력되게 바꿈
    void Start() {

        high.text = DataManager.Instance.highScore.ToString("N0");
    }

}
