using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class OpenAIManager : MonoBehaviour
{
    public static OpenAIManager Instance;

    public string currentPrompt = "answer any question";
    private const string apiUrl = "https://api.openai.com/v1/chat/completions";
    private const string apiKey = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // OpenAI API에 요청을 보내는 코루틴 함수
    public IEnumerator SendOpenAIRequest(string prompt, string message, Text resultText)
    {
        // JSON 형식의 데이터를 생성
        string jsonData = @"{
            ""model"": ""gpt-4o"",
            ""messages"": [
                {
                    ""role"": ""system"",
                    ""content"": """ + prompt + @"""
                },
                {
                    ""role"": ""user"",
                    ""content"": """ + message + @"""
                }
            ]
        }";

        // UTF-8 인코딩으로 JSON 데이터를 바이트 배열로 변환
        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        // UnityWebRequest를 사용하여 POST 요청을 생성
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            // 요청 헤더 설정
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 요청 데이터 설정
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 요청 전송
            yield return request.SendWebRequest();

            // 에러 핸들링
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
            }
            else
            {
                // 응답 처리
                string responseText = request.downloadHandler.text;
                Debug.Log("Response: " + responseText);

                // 응답 데이터에서 assistant의 메시지 추출
                var responseData = JsonUtility.FromJson<OpenAIResponse>(responseText);
                if (responseData.choices != null && responseData.choices.Length > 0)
                {
                    string assistantMessage = responseData.choices[0].message.content;
                    resultText.text = assistantMessage;
                    StartCoroutine(NPCManager.Instance.TalkThenIdle());
                }
                else
                {
                    Debug.LogWarning("No valid response from the assistant.");
                }
            }
        }
    }
}

[System.Serializable]
public class OpenAIResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string content;
}