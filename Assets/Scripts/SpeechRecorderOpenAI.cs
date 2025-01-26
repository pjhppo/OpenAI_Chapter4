using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SpeechRecorderOpenAI : MonoBehaviour
{
    [Header("OpenAI Settings")]
    [SerializeField] private string openAIApiKey = "YOUR_API_KEY_HERE";
    [SerializeField] private string voiceName = "alloy";
    [SerializeField] private string model = "tts-1";

    [Header("Record Settings")]
    [SerializeField] private string saveFolderName = "RecordResult";

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
        if(SpeechRecorderManager.Instance.currentTTS == "Open AI TTS"){
            StartCoroutine(GetAndPlayAudio(message));
        }       
    }

    IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        string url = "https://api.openai.com/v1/audio/speech";

        // JSON 페이로드 생성
        string jsonPayload = $"{{\"model\":\"{model}\",\"input\":\"{textToSynthesize}\",\"voice\":\"{voiceName}\"}}";

        // 요청 생성
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 헤더 설정
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        // 요청 전송
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;

            // MP3 데이터를 AudioClip으로 변환
            AudioClip audioClip = null;

            // MP3 데이터를 처리하기 위한 코루틴 호출
            yield return StartCoroutine(LoadAudioClipFromMp3Data(audioData, clip => audioClip = clip));

            if (audioClip != null)
            {
                // 오디오 재생
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("AudioClip 생성 실패");
            }
        }
        else
        {
            Debug.LogError("TTS 요청 실패: " + request.error);
            Debug.LogError("응답 내용: " + request.downloadHandler.text);
        }
    }

    IEnumerator LoadAudioClipFromMp3Data(byte[] mp3Data, System.Action<AudioClip> callback)
    {
        // Assets/RecordResult/ 폴더 경로 생성
        string folderPath = Path.Combine(Application.dataPath, saveFolderName);

        // 폴더가 없으면 생성
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 날짜 및 시간 기반 파일 이름 생성
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Recording_{timestamp}.mp3";

        // 파일 경로 설정
        string tempFilePath = Path.Combine(folderPath, fileName);

        // MP3 데이터를 파일로 저장
        File.WriteAllBytes(tempFilePath, mp3Data);

        Debug.Log($"오디오 파일이 저장되었습니다: {tempFilePath}");

        // 파일에서 AudioClip 로드
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempFilePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                callback(clip);
            }
            else
            {
                Debug.LogError("AudioClip 로드 실패: " + www.error);
                callback(null);
            }
        }
    }
}
