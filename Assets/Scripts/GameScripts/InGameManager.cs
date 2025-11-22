using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TMPro;
using UnityEngine;

public class InGameManager : MonoBehaviour
{
    #region 싱글톤 및 기타
    public static InGameManager Instance { get; private set; }
    public AnimationCurve scoreCurve;
    #endregion

    #region ★ 게임에 필요한 변수들

    //게임 목록
    private string[] gameList = new string[] { "가위바위보", "참참참", "제로게임" };
    // 손 사진의 목록
    private string[] handList = new string[] { "rock", "scissors", "paper",       // 바위가위보
                                             "ccc_left", "ccc_mid", "ccc_right",  // 참참참(좌중우)
                                             "zero_0", "zero_1", "zero_2"};      // 제로(0, 1, 2)

    private List<Texture2D> preloadedHandTextures; // 텍스처를 미리 담아둘 리스트

    // 게임 점수 및 콤보 수
    private int _Score = 0;
    private int _Combo = 0;
    private int _MaxCombo = 0;

    // 게임 남은 시간
    private float remainTime = 60.0f;
    // 생존 시간
    private float survivalTime = 0.0f;

    private bool isGame = false;        // 게임 중인지
    private int nowGame = 0;    // 현재 게임 (0 : 가위바위보, 1 : 참참참, 2 : 제로게임)
    private int nextGame = 2;   // 다음 게임 (0 : 가위바위보, 1 : 참참참, 2 : 제로게임)

    private int playerHand = 0;  // 플레이어 손
    private int oppoentHand = 0; // 상대 손

    // 최초 게임당 시간
    private const float MAXpartTime = 3.0f;

    // 각 게임당 시간
    public float partTime { get; private set; } = 3.0f;
    // 미니게임 남은 시간 (최초 3초부터 점점 줄어듦)
    private float minigameTime = 3.0f;
    // 성공한 게임 수
    private int clearedGame = 0;
    private int playedgame = 0;
    // 미션 텍스트
    public static string missionString = "가위바위보 시작!";
    #endregion

    #region ★ 하이라키창에서 선택할 것들 (건들 필요X)
    [SerializeField]
    private TextMeshProUGUI survivalTime_Text;
    [SerializeField]
    private TextMeshProUGUI remainTime_Text;
    [SerializeField]
    private TextMeshProUGUI nextGame_Text;
    [SerializeField]
    private TextMeshProUGUI nowGame_Text;
    [SerializeField]
    private TextMeshProUGUI score_Text;
    [SerializeField]
    private ParticleSystem playerHand_effect;
    [SerializeField]
    private ParticleSystem oppoentHand_effect;
    [SerializeField]
    private TextMeshProUGUI missionText;
    [SerializeField]
    private RectTransform timeGauge;
    #endregion

    private string pythonExeName = "main.exe";

    // ▼▼▼ [추가] UDP 네트워크 변수들 ▼▼▼
    private Thread receiveThread;
    private UdpClient client;
    private int port = 12345; // 파이썬과 동일하게 맞출 포트

    // 네트워크 스레드 -> 메인 스레드 데이터 전달용 큐
    private Queue<int[]> handDataQueue = new Queue<int[]>();
    private readonly object queueLock = new object();
    private int[] lastReceivedHandArray = null; // 마지막으로 받은 배열 (중복 전송 방지용)
    // ▲▲▲ [추가] ▲▲▲

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;


        // ▼▼▼ [수정] 텍스처 미리 불러오기 ▼▼▼
        preloadedHandTextures = new List<Texture2D>();
        foreach (string handName in handList) {
            Texture2D tex = Resources.Load<Texture2D>("Hand/" + handName);
            if (tex != null) {
                preloadedHandTextures.Add(tex);
            }
            else {
                UnityEngine.Debug.LogError("텍스처 로드 실패! : Hand/" + handName);
            }
        }
        // ▲▲▲ [수정] ▲▲▲

        nowGame_Text.text = gameList[nowGame];
        nextGame_Text.text = gameList[nextGame];

        InitializeUDP();
        RunPythonScript();
    }

    void Update() {

        ProcessHandDataQueue();

        if (!isGame) return;

        timeTick();

        // 플레이어 손 키보드로 구현
        if (nowGame == 0) {
            if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(0);
            if (Input.GetKeyDown(KeyCode.X)) playerHandChange(1);
            if (Input.GetKeyDown(KeyCode.C)) playerHandChange(2);
        }

        else if (nowGame == 1) {
            if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(3);
            if (Input.GetKeyDown(KeyCode.X)) playerHandChange(4);
            if (Input.GetKeyDown(KeyCode.C)) playerHandChange(5);
        }
        else if (nowGame == 2) {
            if (Input.GetKeyDown(KeyCode.Z)) playerHandChange(6);
            if (Input.GetKeyDown(KeyCode.X)) playerHandChange(7);
            if (Input.GetKeyDown(KeyCode.C)) playerHandChange(8);
        }
    }

    private Process pythonProcess;

    private void RunPythonScript() {
        ProcessStartInfo psi = new ProcessStartInfo();
        string Pypath = Path.Combine(Application.dataPath, "../main.exe");
        Pypath = Path.GetFullPath(Pypath);

        psi.FileName = Pypath;
        psi.Arguments = "";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        try {
            pythonProcess = Process.Start(psi);
            UnityEngine.Debug.Log($"AI 모델 실행 성공: {Pypath}");
        }
        catch (System.Exception e) {
            UnityEngine.Debug.LogError($"실행 실패: {e.Message}");
        }

    }

    private bool miniEnd = false;
    // 게임의 시간을 다루는 함수 (제한시간, 생존시간, 미니게임 시간 등)
    private void timeTick()
    {
        remainTime -= Time.deltaTime;               // 제한시간
        survivalTime += Time.deltaTime;             // 생존시간
        if (minigameTime >= 0.0f) minigameTime -= Time.deltaTime; // 미니게임 시간

        // minigameTime을 정규화 시킨 후, 화면 아래의 시간게이지를 감소시킴
        float normalized = minigameTime / partTime;
        normalized = Mathf.Clamp01(normalized);

        // 미니게임이 시작되면 화면 아래의 게이지가 줄어듦 (0이 되면 미니게임 성공여부 판정)
        timeGauge.sizeDelta = new Vector2(1920 * normalized, 30);




        ////////////////// ★★★★★ 이 아래 switch 부분을 작성해주세요 ★★★★★ //////////////////

        // 미니게임 제한시간이 끝났을 떄
        if (minigameTime <= 0.0f && miniEnd == false) {

            miniEnd = true;

            // 현재 게임을 확인하고 각 게임 스크립트의 승리인지 패배인지를 여부를 불러옴
            // 0 : 가위바위보, 1 : 참참참, 2 : 제로게임
            switch (nowGame)
            {
                case 0:
                    RPS_Webcam_Controller.Instance.JudgeRPSResult(); // 가위바위보 패배, 승리 여부 판단
                    break;                          
                case 1:
                    ChamChamCham_Webcam_Controller.Instance.JubgeCCCResult();
                    break;                          // 참참참.cs에서 이걸 불러오기
                case 2:
                    ZeroGame_Main.Instance.JudgeZeroGameResult();
                    break;                         
                default:
                    break;
            }
        }

        // 게임 제한시간이 끝났을 때
        if (remainTime <= 0.0f)
        {
            remainTime = 0.0f;
            GameOver();
        }

        int minutes = (int)(survivalTime / 60);
        int seconds = (int)(survivalTime % 60);
        survivalTime_Text.text = $"{minutes}:{seconds:00}";

        if (remainTime > 10.0f) remainTime_Text.text = remainTime.ToString("F0");
        else remainTime_Text.text = remainTime.ToString("F1");
    }

    // ▼▼▼ [추가] 네트워크 스레드에서 큐에 쌓인 데이터를 처리하는 함수 ▼▼▼
    private void ProcessHandDataQueue() {
        
        while (handDataQueue.Count > 0) {
            int[] handArray;
            lock (queueLock) {
                handArray = handDataQueue.Dequeue();
            }
            
            // handArray[3]와 nowGame을 기반으로 최종 playerHand 값을 매핑
            if (handArray == null || handArray.Length != 3) continue;

            int finalHandValue = -1;
            int gameSpecificValue = -1;
            
            switch (nowGame) {
                // nowGame 0: 가위바위보
                case 0:
                    gameSpecificValue = handArray[0]; // 배열의 첫 번째 값
                    // [매핑] 
                    // 파이썬 (0:가위, 1:바위, 2:보) -> handList (1:가위, 0:바위, 2:보)
                    if (gameSpecificValue == 0) finalHandValue = 0; // 0(가위) -> 1(scissors)
                    else if (gameSpecificValue == 1) finalHandValue = 1; // 1(바위) -> 0(rock)
                    else if (gameSpecificValue == 2) finalHandValue = 2; // 2(보) -> 2(paper)
                    break;
                // nowGame 1: 참참참
                case 1:
                    gameSpecificValue = handArray[1]; // 배열의 두 번째 값
                    // [매핑]
                    // 파이썬 (0:왼, 1:중, 2:오) -> handList (3:왼, 4:중, 5:오)
                    if (gameSpecificValue >= 0 && gameSpecificValue <= 2) {
                        finalHandValue = gameSpecificValue + 3; // 0->3, 1->4, 2->5
                    }
                    break;

                // nowGame 2: 제로게임
                case 2:
                    gameSpecificValue = handArray[2]; // 배열의 세 번째 값
                    // [매핑]
                    // 파이썬 (0:0, 1:1, 2:2) -> handList (6:0, 7:1, 8:2)
                    if (gameSpecificValue >= 0 && gameSpecificValue <= 2) {
                        finalHandValue = gameSpecificValue + 6; // 0->6, 1->7, 2->8
                    }
                    break;
            }

            // 유효한 값이 매핑되었을 때만 playerHandChange 호출
            if (finalHandValue != -1) {
                playerHandChange(finalHandValue);
            }
        }
    }
    // ▲▲▲ [추가] ▲▲▲

    // ▼▼▼ [추가] UDP 통신 관련 함수들 ▼▼▼

    /// <summary>
    /// UDP 수신 스레드 초기화
    /// </summary>
    private void InitializeUDP() {
        lock (queueLock) {
            handDataQueue.Clear();
        }
        lastReceivedHandArray = null;

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        UnityEngine.Debug.Log("UDP Thread Started. Listening on port " + port);
    }

    /// <summary>
    /// UDP 데이터 수신 스레드 (백그라운드 실행)
    /// </summary>
    private void ReceiveData() {
        client = new UdpClient(port);
        while (true) {
            try {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP); // 파이썬에서 보낸 byte 배열

                // 파이썬에서 1바이트 정수 3개를 보냈다고 가정 (총 3바이트)
                // 예: [0, 1, 2]
                if (data != null && data.Length == 3) {
                    // byte[] -> int[] 변환
                    int[] detectedHandArray = new int[3];
                    detectedHandArray[0] = (int)data[0];
                    detectedHandArray[1] = (int)data[1];
                    detectedHandArray[2] = (int)data[2];

                    // 마지막 값과 비교하여 다를 때만 큐에 추가 (최적화)
                    if (!AreArraysEqual(detectedHandArray, lastReceivedHandArray)) {
                        lastReceivedHandArray = detectedHandArray;
                        lock (queueLock) {
                            handDataQueue.Enqueue(detectedHandArray);
                        }
                    }
                }

            }
            catch (Exception err) {
                UnityEngine.Debug.LogError(err.ToString());
            }
        }
    }

    /// <summary>
    /// 두 int 배열의 내용이 같은지 비교 (Helper 함수)
    /// </summary>
    private bool AreArraysEqual(int[] arr1, int[] arr2) {
        if (arr1 == null && arr2 == null) return true;
        if (arr1 == null || arr2 == null) return false;
        if (arr1.Length != arr2.Length) return false;

        for (int i = 0; i < arr1.Length; i++) {
            if (arr1[i] != arr2[i]) return false;
        }
        return true;
    }



    //////////////// PUBLIC 구간 ///////////////////
    //// 아래의 함수는 다른 스크립트에서 호출 가능 ////
    ////////////////////////////////////////////////

    // 미니게임 성공 시, 함수를 호출 (본인의 스크립트에서 미니게임 성공 조건을 달성했다면 이걸 호출하세요)
    public void gameClear()
    {
        AudioManager.Instance.playSFX(6); // 클리어 효과음 재생예정
        _Combo++;
        if (_Combo > _MaxCombo) _MaxCombo = _Combo;
        clearedGame++;
        playedgame++;
        remainTime += (5.0f - (survivalTime / 20.0f));
        addScore();
        pickGame();
    }

    // 미니게임 실패 시, 함수를 호출 (본인의 스크립트에서 미니게임이 실패했다면 이걸 호출하세요)
    public void gameFail()
    {
        AudioManager.Instance.playSFX(7); // 패배 효과음 재생예정
        playedgame++;
        _Combo = 0;
        remainTime -= 10.0f;
        pickGame();
    }

    // 매개변수 val 값으로 플레이어 손을 바꿈 (플레이어 손을 인식한 val값으로 바꾸세요)
    public void playerHandChange(int val)
    {
        if (isGame == false) return;
        if (playerHand == val) return;
        playerHand_effect.Clear();
        // Texture2D loadTexture
        //     = Resources.Load<Texture2D>("Hand/" + handList[val]);
        Texture2D loadTexture = preloadedHandTextures[val];

        var shapeModule_p = playerHand_effect.shape;
        playerHand = val;
        shapeModule_p.texture = loadTexture;
        playerHand_effect.Emit(15000);
    }

    // 매개변수 val 값으로 상대방의 손을 바꿈 (본인의 스크립트에서 상대의 손을 바꿨다면 이걸 호출하세요)
    public void oppoentHandChange(int val)
    {
        if (isGame == false) return;
        if (oppoentHand == val) return;
        oppoentHand_effect.Clear();
        // Texture2D loadtexture_oppoent
        //     = Resources.Load<Texture2D>("Hand/" + handList[val]);

        Texture2D loadTexture = preloadedHandTextures[val];

        var shapeModule_o = oppoentHand_effect.shape;
        oppoentHand = val;
        shapeModule_o.texture = loadTexture;
        oppoentHand_effect.Emit(15000);
    }

    // 상대방의 손이 현재 무엇인지 확인함 (본인의 스크립트에서 상대의 손이 무엇인지 알고 싶다면 호출)
    public int checkOppoentHand()
    {
        return oppoentHand;
    }

    // 플레이어의 손이 현재 무엇인지 확인함 (본인의 스크립트에서 플레이어의 손이 무엇인지 알고 싶다면 호출)
    public int checkPlayerHand()
    {
        return playerHand;
    }

    // 미션 텍스트 출력 (이 함수를 사용하지 말고 IngameManager.Instance.missionString를 바꾸세요,
    // missionString을 바꾼다면 바뀐 텍스트가 미니게임이 바뀔 때, 자동으로 뜹니다)
    public void MissionCall()
    {

        missionText.text = missionString;
        missionText.transform.localScale = new Vector3(2.5f, 2.5f, 1f);
        LeanTween.value(gameObject, 0f, 1f, 0.2f).
        setOnUpdate((float val) => {
            missionText.alpha = val;
        });
        LeanTween.scale(missionText.gameObject, new Vector3(1f, 1f, 1f), 0.1f).
            setEase(LeanTweenType.easeOutBack);
        LeanTween.value(gameObject, 2.5f, 0f, 1f).
        setOnUpdate((float val) => {
            missionText.alpha = val;
        });
    }


    //////////// 이 아래는 건들필요 없음 /////////////


    // 처음에 손이 안 보이는데, 보이게 만들기
    public void opening()
    {
        playerHand_effect.Play();
        oppoentHand_effect.Play();
    }


    // 게임 시작 시, 함수를 호출 (이미 사용됨, 재사용 ㄴ)
    public void GameStart()
    {
        AudioManager.Instance.playBGM(1);
        isGame = true;
        MissionCall();
    }




    //////////////// PRIVATE 구간 ///////////////////////////
    //// 아래의 함수는 여기에서만 사용됨 (다른 스크립트는 X) ////
    /////////////////////////////////////////////////////////

    // 제한시간이 종료되어 게임오버됨
    private void GameOver()
    {
        isGame = false;
        AudioManager.Instance.playSFX(7); // 게임 실패 효과음
        // AudioManager.Instance.playBGM(2); // 게임 실패 브금
        float percent = (float)clearedGame / (float)playedgame;
        string scoreNpercent = $"{clearedGame} ({percent.ToString("P1")})";
        GameEndScripts.Instance.resultPanelOpen(_Score, scoreNpercent, _MaxCombo, survivalTime_Text.text);
        DataManager.Instance.IshighScore(_Score);
    }

    // 게임을 오래할 수록 점수가 증가됨
    // 계산 : 기본 1000점 x 콤보 수 x 생존 시간(최대 3배)
    private void addScore()
    {
        float multi = scoreCurve.Evaluate(survivalTime);
        _Score += (int)((1000 * _Combo) * (multi));
        score_Text.text = _Score.ToString("N0");
    }

    // 게임을 뽑고 텍스트를 바꿈
    private void pickGame() {

        nowGame = nextGame;
        nowGame_Text.text = gameList[nowGame];
        
        int randInt = UnityEngine.Random.Range(0, 3);
        nextGame = randInt;
        nextGame_Text.text = gameList[randInt];

        /// 가위바위보만 등장하게 바꿉니다 ///
        /// 
        // nowGame = 0;
        // nextGame = 0;
        // nowGame_Text.text = gameList[nowGame];
        // nextGame_Text.text = gameList[nextGame];

        // 게임 중간에 3, 2, 1 카운트다운
        StartCoroutine(CountDown.Instance.CountDownStart(timeSet));

    }

    // 미니게임 시간을 partTime(미니게임 시간 설정값)으로 설정
    private void timeSet()
    {
        // partTime : 최초 3초로 시작하며, 0.5초까지 줄어드는 변수
        // 게임 10개 클리어 시, 미니게임 시간이 1초 감소
        partTime = MAXpartTime - (clearedGame / 10);
        partTime = Mathf.Clamp(partTime, 0.5f, 3.0f); // partTime을 0.5 ~ 3초로 제한
        minigameTime = partTime;
        miniEnd = false;
    }

    public void StopUDP() {
        try {
            if (receiveThread != null && receiveThread.IsAlive) {
                receiveThread.Abort();  // 스레드 종료
                receiveThread = null;
            }

            if (client != null) {
                client.Close();         // 소켓 닫기
                client = null;
            }

            UnityEngine.Debug.Log("UDP 통신 중단 완료");
        }
        catch (Exception e) {
            UnityEngine.Debug.LogError("UDP 중단 중 오류: " + e);
        }
    }

    private void KillPythonForce() {
        try {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "taskkill";
            // /IM: 이미지 이름(파일이름), /F: 강제종료, /T: 자식프로세스까지 전부
            psi.Arguments = $"/IM {pythonExeName} /F /T";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            Process.Start(psi);
            UnityEngine.Debug.Log($"{pythonExeName} 강제 종료 명령 실행됨");
        }
        catch (Exception e) {
            UnityEngine.Debug.LogError($"강제 종료 실패: {e.Message}");
        }
    }

    void OnDestroy() {
        StopUDP();
        KillPythonForce();
    }

    void OnApplicationQuit() {
        if (receiveThread != null && receiveThread.IsAlive) {
            receiveThread.Abort();
        }
        if (client != null)
            client.Close();
        KillPythonForce();
    }

}

