using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class GoogleTTSScript : MonoBehaviour
{
    [Header("Google Cloud Settings")]
    [SerializeField] private string apiKey = "구글 클라우드 API키 입력";
    [SerializeField] private string languageCode = "en-US";
    [SerializeField] private string voiceName = "en-US-Wavenet-D"; // 원하는 보이스 이름
    [SerializeField] private string testMessage = "Hello";

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        //StartCoroutine(GetAndPlayAudio(testMessage));

        OpenAIManager.Instance.OnReceivedMessage += ReceivedMessage;
    }

     private void ReceivedMessage(string message){
        StartCoroutine(GetAndPlayAudio(message));
    }

    IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        // JSON 페이로드 생성
        GoogleTTSRequest requestBody = new GoogleTTSRequest
        {
            input = new GoogleTTSInput { text = textToSynthesize },
            voice = new GoogleTTSVoice { languageCode = languageCode, name = voiceName },
            audioConfig = new GoogleTTSAudioConfig { audioEncoding = "LINEAR16" }
        };

        string jsonPayload = JsonUtility.ToJson(requestBody);

        // 요청 생성
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] postData = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 헤더 설정
        request.SetRequestHeader("Content-Type", "application/json");

        // 요청 전송
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 응답 파싱
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

    // 요청 본문에 사용할 클래스들
    [Serializable]
    private class GoogleTTSRequest
    {
        public GoogleTTSInput input;
        public GoogleTTSVoice voice;
        public GoogleTTSAudioConfig audioConfig;
    }

    [Serializable]
    private class GoogleTTSInput
    {
        public string text;
    }

    [Serializable]
    private class GoogleTTSVoice
    {
        public string languageCode;
        public string name;
    }

    [Serializable]
    private class GoogleTTSAudioConfig
    {
        public string audioEncoding;
    }

    // Google TTS 응답을 파싱하기 위한 클래스
    [Serializable]
    private class GoogleTTSResponse
    {
        public string audioContent;
    }

    // WAV 데이터를 AudioClip으로 변환하는 메서드
    private AudioClip ToAudioClip(byte[] wavFile, int offsetSamples = 0, string name = "wav")
    {
        // WAV 헤더 분석
        int channels = wavFile[22]; // 채널 수
        int sampleRate = BitConverter.ToInt32(wavFile, 24); // 샘플 레이트

        int subchunk2 = BitConverter.ToInt32(wavFile, 40); // 데이터 크기
        int sampleCount = subchunk2 / 2; // 16비트 오디오이므로 2로 나눕니다.

        float[] data = new float[sampleCount];

        int pos = 44; // WAV 헤더는 44바이트입니다.
        int i = 0;

        while (pos < wavFile.Length)
        {
            // 16비트 샘플을 읽어서 -1.0f ~ 1.0f 범위로 변환
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
