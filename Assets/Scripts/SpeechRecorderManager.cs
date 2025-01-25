using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class SpeechRecorderManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SpeechRecorderManager Instance { get; private set; }

    [Header("UI Elements")]
    public InputField inputField; // 사용자 입력 필드
    public TMP_Dropdown dropDown;     // 드롭다운 메뉴

    [HideInInspector]
    public string currentTTS;    // 현재 선택된 TTS 옵션

    // UnityEvent<string> 정의
    [System.Serializable]
    public class StringEvent : UnityEvent<string> { }

    // 이벤트 정의
    public StringEvent onInputFieldSubmit;  // InputField 텍스트 완료 이벤트

    private void Awake()
    {
        currentTTS = dropDown.options[0].text;

        // 싱글톤 인스턴스 초기화
        if (Instance == null)
        {
            Instance = this; // 현재 인스턴스를 설정
        }
        else
        {
            Debug.LogWarning("SpeechRecorderManager 인스턴스가 이미 존재합니다. 중복된 인스턴스를 제거합니다.");
            Destroy(gameObject); // 중복된 인스턴스를 제거
            return;
        }

        // 다른 씬에서도 유지
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // InputField에 대한 이벤트 등록
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputFieldSubmit);
        }

        // Dropdown에 대한 이벤트 등록
        if (dropDown != null)
        {
            dropDown.onValueChanged.AddListener(OnDropDownValueChanged);
        }
    }

    // InputField에서 텍스트 입력 완료 시 호출
    private void OnInputFieldSubmit(string text)
    {
        Debug.Log($"InputField 텍스트 입력 완료: {text}");
        onInputFieldSubmit.Invoke(text); // 입력된 텍스트를 이벤트로 전달
    }

    // Dropdown 옵션 변경 시 호출
    private void OnDropDownValueChanged(int index)
    {
        if (dropDown != null && dropDown.options.Count > index)
        {
            currentTTS = dropDown.options[index].text; // 선택된 옵션을 currentTTS에 저장
            Debug.Log($"Dropdown 옵션 변경: {currentTTS}");
        }
    }

    private void OnDestroy()
    {
        // 이벤트 해제 및 싱글톤 인스턴스 제거
        if (Instance == this)
        {
            Instance = null;
        }

        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnInputFieldSubmit);
        }
        if (dropDown != null)
        {
            dropDown.onValueChanged.RemoveListener(OnDropDownValueChanged);
        }
    }
}
