using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public InputField inputField; // Unity Editor에서 연결할 InputField
    public Text resultText; // 결과를 표시할 Text 컴포넌트

    // 싱글톤 선언
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

    private void Start()
    {
        // InputField에 입력 완료 이벤트 등록
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    private void OnInputFieldEndEdit(string inputText)
    {
        // 입력된 텍스트가 null이 아니고 공백이 아닌지 확인
        if (!string.IsNullOrEmpty(inputText))
        {
            // OpenAIManager의 인스턴스를 사용하여 코루틴 실행
            StartCoroutine(OpenAIManager.Instance.SendOpenAIRequest("answer any question", inputText, resultText));

            // 입력 필드 초기화 (필요할 경우)
            inputField.text = "";
        }
        else
        {
            Debug.LogWarning("Input field is empty or null.");
        }
    }
}
