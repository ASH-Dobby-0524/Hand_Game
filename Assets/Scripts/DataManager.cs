using UnityEngine;

public class DataManager : MonoBehaviour {

    public static DataManager Instance;

    public int highScore = 0;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else {
            Destroy(gameObject);
        }
    }

    public void IshighScore(int score) {
        highScore = (highScore > score) ? highScore : score;
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }

}