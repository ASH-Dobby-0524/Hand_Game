using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameEndScripts : MonoBehaviour
{
    public static GameEndScripts Instance { get; private set; }
    
    [SerializeField]
    private GameObject result_Panel;
    [SerializeField]
    private TextMeshProUGUI score;
    [SerializeField]
    private Text successGame;
    [SerializeField]
    private Text maxCombo;
    [SerializeField]
    private Text survivalTime;
    [SerializeField]
    private Animator ani;   // 인트로 애니메이션
    [SerializeField]
    private  CanvasGroup panel;

    bool click = false;

    private void Start() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void MainButton() {
        click = true;
        if (!click) return;
        StartCoroutine(goToMain());
        AudioManager.Instance.easeChangeSound(0);
    } 

    public void resultPanelOpen(int score, string successGame, int maxCombo, string survivalTime) {

        LeanTween.value(gameObject, 0f, 1f, 0.5f).
    setOnUpdate((float val) => {
        panel.alpha = val;
    });
        

        this.score.text = score.ToString("N0");
        this.successGame.text = successGame.ToString();
        this.maxCombo.text = $"{maxCombo} combo";
        this.survivalTime.text = survivalTime;
        LeanTween.moveLocalY(result_Panel, -56f, 2.0f).setEaseInOutCirc();
        AudioManager.Instance.easeChangeSound(2);
    }

    private IEnumerator goToMain() {
        GameObject targetObject = GameObject.Find("Intro(Clone)");
        targetObject.GetComponent<Animator>().Rebind();
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("MainTitle");
    }

}
