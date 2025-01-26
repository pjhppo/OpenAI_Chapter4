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
    [SerializeField] private string voiceName = "en-US-Wavenet-D";
    [SerializeField] private string testMessage = "Today is a wonderful day to build something people love!";

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(GetAndPlayAudio(testMessage));
    }

    [Serializable]
    private class GoogleTTSResponse
    {
        public string audioContent;
    }

    private IEnumerator GetAndPlayAudio(string textToSynthesize)
    {
        string url = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";
        string jsonPayload = $@"
        {{
            ""input"": {{ ""text"": ""{textToSynthesize.Replace("\"", "\\\"")}"", }},
            ""voice"": {{ ""languageCode"": ""{languageCode}"", ""name"": ""{voiceName}"" }},
            ""audioConfig"": {{ ""audioEncoding"": ""LINEAR16"" }}
        }}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                var response = JsonUtility.FromJson<GoogleTTSResponse>(responseText);
                byte[] audioData = Convert.FromBase64String(response.audioContent);
                PlayAudioClip(audioData);
            }
            else
            {
                Debug.LogError($"TTS 요청 실패: {request.error}");
            }
        }
    }

    private void PlayAudioClip(byte[] audioData)
    {
        AudioClip audioClip = ToAudioClip(audioData, 0, "TTS_Audio");
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("AudioClip 생성 실패");
        }
    }

    private AudioClip ToAudioClip(byte[] wavFile, int offsetSamples = 0, string name = "wav")
    {
        int channels = wavFile[22];
        int sampleRate = BitConverter.ToInt32(wavFile, 24);
        int sampleCount = BitConverter.ToInt32(wavFile, 40) / 2;
        float[] data = new float[sampleCount];

        for (int i = 0, pos = 44; i < sampleCount; i++, pos += 2)
        {
            data[i] = BitConverter.ToInt16(wavFile, pos) / 32768.0f;
        }

        AudioClip audioClip = AudioClip.Create(name, sampleCount, channels, sampleRate, false);
        audioClip.SetData(data, offsetSamples);
        return audioClip;
    }
}
