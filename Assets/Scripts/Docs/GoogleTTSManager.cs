using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class GoogleTTSManager : MonoBehaviour
{
    [Header("Google Cloud Settings")]
    [SerializeField] private string apiKey = "구글 클라우드 API키 입력";
    [SerializeField] private string languageCode = "en-US";
    [SerializeField] private string voiceName = "en-US-Wavenet-D"; // 원하는 보이스 이름
    [SerializeField] private string testMessage = "Today is a wonderful day to build something people love!";

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();

        StartCoroutine(GetAndPlayAudio(testMessage));
    }

    // Google TTS API에 요청을 보내고, 응답받은 오디오(WAV 형식)를 재생하는 코루틴
    IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        // Google TTS API 호출 URL
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        // JSON 페이로드(문자열 직접 구성)
        //   - 문자열 안에 큰따옴표 " 가 들어갈 수 있으므로 주의 (아래는 간단히 Replace로 처리)
        //   - audioEncoding: LINEAR16, MP3, OGG_OPUS 등 원하는 형식 지정 가능
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
    // (이 부분은 계속 사용해도 되며, '직렬화 요청 바디' 클래스들과는 별개입니다.)
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
