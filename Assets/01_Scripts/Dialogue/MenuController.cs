using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipConfirmPanel;

    [Header("Log")]
    [SerializeField] private GameObject logPanel;

    [Header("References")]
    [SerializeField] private ScenarioRunner scenarioRunner;

    private bool isMenuOpen;
    private bool isLogOpen;

    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (skipConfirmPanel != null)
        {
            skipConfirmPanel.SetActive(false);
        }

        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }

        UpdateSkipButton();
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        menuPanel.SetActive(isMenuOpen);
    }

    // Skip 버튼 클릭
    public void OnSkipButton()
    {
        if (GameProgressManager.Instance == null)
        {
            return;
        }

        if (!GameProgressManager.Instance.CanSkip)
        {
            return;
        }

        // 바로 스킵하지 않고 확인창 표시
        skipConfirmPanel.SetActive(true);
    }

    // Yes
    public void ConfirmSkip()
    {
        skipConfirmPanel.SetActive(false);

        if (scenarioRunner == null)
        {
            Debug.LogError(
                "ScenarioRunner가 연결되지 않았습니다."
            );

            return;
        }

        scenarioRunner.StartSkipToNextChoice();
    }

    // No
    public void CancelSkip()
    {
        skipConfirmPanel.SetActive(false);
    }

    public void ToggleLog()
    {
        isLogOpen = !isLogOpen;

        logPanel.SetActive(isLogOpen);
    }

    private void UpdateSkipButton()
    {
        if (skipButton == null)
        {
            return;
        }

        bool canSkip =
            GameProgressManager.Instance != null &&
            GameProgressManager.Instance.CanSkip;

        skipButton.interactable = canSkip;
    }
}
