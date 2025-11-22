using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CheckPython : MonoBehaviour
{

    [SerializeField]
    private Text output_message;
    [SerializeField]
    private Button validationButton;
    void Start() {
        // 버튼에 함수 연결
        if (validationButton != null)
            validationButton.onClick.AddListener(OnValidateClick);
    }

    public void OnValidateClick() {
        StartCoroutine(ValidationRoutine());
    }

    private IEnumerator ValidationRoutine() {

        output_message.text = "유효성 검사 중...";
        var originColor = output_message.color;
        output_message.color = Color.yellow;
        validationButton.interactable = false;

        string Pypath = Path.Combine(Application.dataPath, "../main.exe");
        Pypath = Path.GetFullPath(Pypath);

        if (!File.Exists(Pypath)) {
            output_message.text = "Python 파일이 존재하지 않습니다. main.exe가 필요합니다.";
            output_message.color = Color.red;
            validationButton.interactable = true;
            yield break;
        }



        // 2. 프로세스 설정 (로그를 캡처하기 위한 설정)
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = Pypath;
        psi.Arguments = "";
        psi.UseShellExecute = false;    // 로그를 읽으려면 false여야 함
        psi.RedirectStandardError = true; // 에러 로그 캡처
        psi.RedirectStandardOutput = true; // 일반 로그 캡처
        psi.CreateNoWindow = true;      // 검사니까 창 숨김

        psi.StandardOutputEncoding = System.Text.Encoding.UTF8;
        psi.StandardErrorEncoding = System.Text.Encoding.UTF8;

        Process test = null;

        try {
            test = Process.Start(psi);
        }
        catch (System.Exception e) {
            output_message.text = "파일을 실행할 수 없습니다." + e.Message;
            validationButton.interactable = true;
            yield break;
        }

        // 3. 7초간 대기 (파이썬이 라이브러리 로딩하고 웹캠 켜는 시간)
        float waitTime = 7.0f;
        float timer = 0.0f;
        bool hasCrashed = false;

        while (timer < waitTime) {
            if (test.HasExited) {
                hasCrashed = true;
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (hasCrashed) {

            // print()로 찍은 내용 읽기
            string errorLog = test.StandardError.ReadToEnd();

            output_message.text = "웹캠이 없습니다. 연결을 확인하세요.";
            output_message.color = Color.red;
        }

        else {
            output_message.text = "유효성 검사 성공! Python이 정상적으로 작동됩니다!";
            output_message.color = originColor;

            test.Kill();
            test.Dispose();
            KillPythonForce();
            UnityEngine.Debug.Log("테스트 프로세스 정상 종료됨");
        }

        validationButton.interactable = true;
    }

    private string pythonExeName = "main.exe";
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

}
