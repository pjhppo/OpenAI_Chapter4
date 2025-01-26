using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OpenAITextToSpeech : MonoBehaviour
{
    [Header("OpenAI Settings")]
    [SerializeField] private string openAIApiKey = "YOUR_API_KEY_HERE";
    [SerializeField] private string voiceName = "ash";
    [SerializeField] private string model = "tts-1-hd";

    [Header("UI Settings")]
    [SerializeField] private InputField inputField;
    [SerializeField] private TMP_Dropdown dropDownVoice;
    [SerializeField] private TMP_Dropdown dropDownModel;

    private AudioSource audioSource;

    private void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldSubmit);
        }

        // Dropdown에 대한 이벤트 등록
        if (dropDownVoice != null)
        {
            dropDownVoice.onValueChanged.AddListener(OnDropDownVoiceValueChanged);
        }

        if (dropDownModel != null)
        {
            dropDownModel.onValueChanged.AddListener(OnDropDownModelValueChanged);
        }

        audioSource = gameObject.GetComponent<AudioSource>();
    }

    // InputField에서 텍스트 입력 완료 시 호출
    private void OnInputFieldSubmit(string message)
    {
        Debug.Log($"InputField 텍스트 입력 완료: {message}");
        StartCoroutine(GetAndPlayAudio(message));
    }

    private void OnDropDownVoiceValueChanged(int index)
    {
        if (dropDownVoice != null && dropDownVoice.options.Count > index)
        {
            voiceName = dropDownVoice.options[index].text; // 선택된 옵션을 voiceName에 저장
            Debug.Log($"Voice Dropdown 옵션 변경: {voiceName}");
        }
    }

     private void OnDropDownModelValueChanged(int index)
    {
        if (dropDownModel != null && dropDownVoice.options.Count > index)
        {
            model = dropDownModel.options[index].text; // 선택된 옵션을 voiceName에 저장
            Debug.Log($"Model Dropdown 옵션 변경: {model}");
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
        // 임시 파일 경로 생성
        string tempFilePath = Path.Combine(Application.persistentDataPath, "temp_audio.mp3");

        // MP3 데이터 파일로 저장
        File.WriteAllBytes(tempFilePath, mp3Data);

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

        // 사용 후 임시 파일 삭제
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }
}

