using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Menu & Animation")]
    [SerializeField] private RectTransform menuPanelRect; // 너비가 늘어날 패널
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isMenuOpen = false;
    private Coroutine menuCoroutine;
    private float targetWidth; // 펼쳐졌을 때의 원본 너비

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipConfirmPanel;

    [Header("Log")]
    [SerializeField] private GameObject logPanel;

    [Header("UI Hide")]
    [SerializeField] private GameObject[] hideTargets;      // 숨길 UI 오브젝트 목록
    [SerializeField] private GameObject uiHideClickArea;   // 숨김 해제용 화면 전체 클릭 영역
    private bool isUIHidden;
    private readonly Dictionary<GameObject, bool> previousActiveStates = new Dictionary<GameObject, bool>();

    [Header("References")]
    [SerializeField] private ScenarioRunner scenarioRunner;

    private void Start()
    {
        if (menuPanelRect != null)
        {
            // 에디터에서 설정해둔 패널의 기본 너비 저장
            targetWidth = menuPanelRect.rect.width;

            // 시작할 때는 너비를 0으로 설정해 메뉴 버튼 뒤로 숨겨둠
            menuPanelRect.sizeDelta = new Vector2(0, menuPanelRect.sizeDelta.y);
        }

        if (skipConfirmPanel != null) skipConfirmPanel.SetActive(false);
        if (logPanel != null) logPanel.SetActive(false);
        if (uiHideClickArea != null) uiHideClickArea.SetActive(false);

        UpdateSkipButton();
    }

    // Menu 버튼 눌렀을 때 토글 (펼치기 / 접기)
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuCoroutine != null)
            StopCoroutine(menuCoroutine);

        float endWidth = isMenuOpen ? targetWidth : 0f;
        menuCoroutine = StartCoroutine(AnimateWidth(endWidth));
    }

    private IEnumerator AnimateWidth(float endWidth)
    {
        float startWidth = menuPanelRect.sizeDelta.x;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = animCurve.Evaluate(Mathf.Clamp01(elapsed / animDuration));
            
            float currentWidth = Mathf.Lerp(startWidth, endWidth, t);
            menuPanelRect.sizeDelta = new Vector2(currentWidth, menuPanelRect.sizeDelta.y);
            
            yield return null;
        }

        menuPanelRect.sizeDelta = new Vector2(endWidth, menuPanelRect.sizeDelta.y);
    }

    // --- Log 기능 ---
    public void ToggleLogPanel()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(!logPanel.activeSelf);
        }
    }

    // --- Skip 기능 ---
    public void OnSkipButton()
    {
        if (GameProgressManager.Instance == null || !GameProgressManager.Instance.CanSkip) return;
        skipConfirmPanel.SetActive(true);
    }

    public void ConfirmSkip()
    {
        skipConfirmPanel.SetActive(false);
        if (scenarioRunner != null) scenarioRunner.StartSkipToNextChoice();
    }

    public void CancelSkip()
    {
        skipConfirmPanel.SetActive(false);
    }

    private void UpdateSkipButton()
    {
        if (skipButton == null) return;
        bool canSkip = GameProgressManager.Instance != null && GameProgressManager.Instance.CanSkip;
        skipButton.interactable = canSkip;
    }

    // --- UI Hide 기능 ---
    public void HideUI()
    {
        if (isUIHidden) return;
        isUIHidden = true;
        previousActiveStates.Clear();

        foreach (GameObject target in hideTargets)
        {
            if (target == null) continue;
            previousActiveStates[target] = target.activeSelf;
            target.SetActive(false);
        }

        if (uiHideClickArea != null) uiHideClickArea.SetActive(true);
    }

    public void RestoreUI()
    {
        if (!isUIHidden) return;
        isUIHidden = false;

        foreach (KeyValuePair<GameObject, bool> pair in previousActiveStates)
        {
            if (pair.Key != null) pair.Key.SetActive(pair.Value);
        }

        previousActiveStates.Clear();
        if (uiHideClickArea != null) uiHideClickArea.SetActive(false);
    }
}