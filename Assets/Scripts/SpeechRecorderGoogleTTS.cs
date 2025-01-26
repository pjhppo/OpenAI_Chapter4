using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.IO;

public class SpeechRecorderGoogleTTS : MonoBehaviour
{
    [Header("Google Cloud Settings")]
    [SerializeField] private string apiKey = "구글 클라우드 API키 입력";
    [SerializeField] private string languageCode = "ko-KR";
    [SerializeField] private string voiceName = "ko-KR-Wavenet-A"; // 원하는 보이스 이름
    
    [Header("Record Settings")]
    [SerializeField] private string saveFolderName = "RecordResult"; // 저장할 폴더 이름

    private AudioSource audioSource;

    private void Start()
    {
        // 싱글톤 인스턴스를 통해 이벤트 구독
        if (SpeechRecorderManager.Instance != null)
        {
            SpeechRecorderManager.Instance.onInputFieldSubmit.AddListener(OnInputFieldCompleted);
        }
        else
        {
            Debug.LogError("SpeechRecorderManager 인스턴스가 없습니다.");
        }

        audioSource = gameObject.GetComponent<AudioSource>();
    }
    private void OnInputFieldCompleted(string message)
    {
        if (SpeechRecorderManager.Instance.currentTTS == "Google TTS")
        {
            StartCoroutine(GetAndPlayAudio(message));
        }
    }

    // Google TTS API에 요청을 보내고, 응답받은 오디오(MP3 형식)를 저장 및 재생하는 코루틴
    IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        // Google TTS API 호출 URL
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        // JSON 페이로드(문자열 직접 구성)
        string escapedText = textToSynthesize.Replace("\"", "\\\"");
        string jsonPayload = $@"
        {{
            ""input"": {{ ""text"": ""{escapedText}"" }},
            ""voice"": {{ ""languageCode"": ""{languageCode}"", ""name"": ""{voiceName}"" }},
            ""audioConfig"": {{ ""audioEncoding"": ""MP3"" }}
        }}";

        // UnityWebRequest 생성
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // 요청 바디 설정
            byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");

            // 요청 전송
            yield return request.SendWebRequest();

            // 결과 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 응답(JSON) 파싱
                string responseText = request.downloadHandler.text;
                GoogleTTSResponse response = JsonUtility.FromJson<GoogleTTSResponse>(responseText);

                // base64 오디오 콘텐츠 디코딩
                byte[] audioData = Convert.FromBase64String(response.audioContent);

                // MP3 파일 저장
                string filePath = SaveAudioToFile(audioData);

                // 저장된 MP3 파일에서 AudioClip 로드 및 재생
                yield return StartCoroutine(LoadAndPlayAudio(filePath));
            }
            else
            {
                Debug.LogError("TTS 요청 실패: " + request.error);
                Debug.LogError("응답 내용: " + request.downloadHandler.text);
            }
        }
    }

    // Google TTS 응답(JSON) 파싱용 클래스
    [Serializable]
    private class GoogleTTSResponse
    {
        public string audioContent;  // base64 인코딩된 오디오 데이터
    }

    // MP3 데이터를 파일로 저장하는 메서드
    private string SaveAudioToFile(byte[] audioData)
    {
        // 폴더 경로 생성
        string folderPath = Path.Combine(Application.dataPath, saveFolderName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 파일 이름 생성
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Recording_{timestamp}.mp3";
        string filePath = Path.Combine(folderPath, fileName);

        // 파일 저장
        File.WriteAllBytes(filePath, audioData);
        Debug.Log($"오디오 파일이 저장되었습니다: {filePath}");

        return filePath;
    }

    // 저장된 MP3 파일에서 AudioClip을 로드하고 재생하는 코루틴
    IEnumerator LoadAndPlayAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = audioClip;
                audioSource.Play();
                Debug.Log($"오디오 재생 시작: {filePath}");
            }
            else
            {
                Debug.LogError("AudioClip 로드 실패: " + www.error);
            }
        }
    }
}
