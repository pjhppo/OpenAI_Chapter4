using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;

public class GoogleTextToSpeech : MonoBehaviour
{
    [Header("Google Cloud Settings")]
    [SerializeField] private string apiKey = "구글 클라우드 API키 입력";
    [SerializeField] private string languageCode = "ko-KR";
    [SerializeField] private string voiceName = "en-US-Wavenet-D"; // 원하는 보이스 이름
    [SerializeField] private string testMessage = "Today is a wonderful day to build something people love!";

    [Header("UI Settings")]
    [SerializeField] private Dropdown dropDownLanguage;
    [SerializeField] private Dropdown dropDownModel;
    [SerializeField] private InputField inputField;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();

        // 드롭다운 초기화
        InitializeDropDowns();

        // 드롭다운 이벤트 리스너 추가
        dropDownLanguage.onValueChanged.AddListener(OnLanguageChanged);
        dropDownModel.onValueChanged.AddListener(OnVoiceChanged);

        // 입력 필드 이벤트 리스너 추가
        inputField.onEndEdit.AddListener(OnInputEndEdit);
    }

    // 드롭다운 초기화
    private void InitializeDropDowns()
    {
        // 언어 드롭다운 초기화
        dropDownLanguage.ClearOptions();
        dropDownLanguage.AddOptions(new List<string> { "한국어", "영어", "중국어", "일본어" });

        // 초기 언어에 따른 보이스 드롭다운 초기화
        UpdateVoiceDropDown();
    }

    // 언어 드롭다운 변경 시 호출
    private void OnLanguageChanged(int index)
    {
        switch (index)
        {
            case 0: // 한국어
                languageCode = "ko-KR";
                break;
            case 1: // 영어
                languageCode = "en-US";
                break;
            case 2: // 중국어
                languageCode = "cmn-CN";
                break;
            case 3: // 일본어
                languageCode = "ja-JP";
                break;
        }

        // 보이스 드롭다운 업데이트
        UpdateVoiceDropDown();
    }

    // 보이스 드롭다운 변경 시 호출
    private void OnVoiceChanged(int index)
    {
        voiceName = dropDownModel.options[index].text;
    }

    // 입력 필드 입력 완료 시 호출
    private void OnInputEndEdit(string text)
    {
        StartCoroutine(GetAndPlayAudio(text));
    }

    // 언어에 따른 보이스 드롭다운 업데이트
    private void UpdateVoiceDropDown()
    {
        dropDownModel.ClearOptions();

        switch (languageCode)
        {
            case "ko-KR": // 한국어
                dropDownModel.AddOptions(new List<string>
                {
                    "ko-KR-Standard-A", "ko-KR-Standard-B", "ko-KR-Standard-C", "ko-KR-Standard-D",
                    "ko-KR-Wavenet-A", "ko-KR-Wavenet-B", "ko-KR-Wavenet-C", "ko-KR-Wavenet-D"
                });
                break;
            case "en-US": // 영어
                dropDownModel.AddOptions(new List<string>
                {
                    "en-US-Standard-B", "en-US-Standard-C", "en-US-Standard-D", "en-US-Standard-E",
                    "en-US-Wavenet-A", "en-US-Wavenet-B", "en-US-Wavenet-C", "en-US-Wavenet-D"
                });
                break;
            case "cmn-CN": // 중국어
                dropDownModel.AddOptions(new List<string>
                {
                    "cmn-CN-Standard-A", "cmn-CN-Standard-B", "cmn-CN-Standard-C", "cmn-CN-Standard-D",
                    "cmn-CN-Wavenet-A", "cmn-CN-Wavenet-B", "cmn-CN-Wavenet-C", "cmn-CN-Wavenet-D"
                });
                break;
            case "ja-JP": // 일본어
                dropDownModel.AddOptions(new List<string>
                {
                    "ja-JP-Standard-A", "ja-JP-Standard-B", "ja-JP-Standard-C", "ja-JP-Standard-D",
                    "ja-JP-Wavenet-A", "ja-JP-Wavenet-B", "ja-JP-Wavenet-C", "ja-JP-Wavenet-D"
                });
                break;
        }

        // 기본 보이스 선택
        voiceName = dropDownModel.options[0].text;
    }

    // Google TTS API에 요청을 보내고, 응답받은 오디오(WAV 형식)를 재생하는 코루틴
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
            ""audioConfig"": {{ ""audioEncoding"": ""LINEAR16"" }}
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

                // WAV 데이터를 AudioClip으로 변환
                AudioClip audioClip = ToAudioClip(audioData, 0, "TTS_Audio");

                // 오디오 재생
                audioSource.clip = audioClip;
                audioSource.Play();
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

    // base64로 받은 WAV 데이터를 AudioClip으로 변환하는 메서드
    private AudioClip ToAudioClip(byte[] wavFile, int offsetSamples = 0, string name = "wav")
    {
        // WAV 헤더 분석 (단순 16비트 WAV 가정)
        int channels = wavFile[22];
        int sampleRate = BitConverter.ToInt32(wavFile, 24);
        int subchunk2 = BitConverter.ToInt32(wavFile, 40);
        int sampleCount = subchunk2 / 2; // 16비트(2바이트) 기준
        float[] data = new float[sampleCount];

        int pos = 44; // WAV 헤더는 일반적으로 44바이트
        int i = 0;

        // PCM 데이터 변환
        while (pos < wavFile.Length)
        {
            short sample = BitConverter.ToInt16(wavFile, pos);
            data[i] = sample / 32768.0f;
            pos += 2;
            i++;
        }

        // AudioClip 생성
        AudioClip audioClip = AudioClip.Create(name, sampleCount, channels, sampleRate, false);
        audioClip.SetData(data, offsetSamples);

        return audioClip;
    }
}