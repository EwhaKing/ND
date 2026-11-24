using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 패널에 "현재 스테이지 이름"과 "조사율 막대"를 표시합니다.
// MainInventoryPanel 아래 빈 오브젝트(예: ProgressUI)에 붙이세요.
public class InventoryProgressUI : MonoBehaviour
{
    public static InventoryProgressUI Instance { get; private set; }

    [Header("스테이지 이름 (회색 막대 위 글씨)")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private bool autoFillStageFromProgress = true; // GameProgressManager 의 챕터 문구를 자동 표시

    [Header("조사율 막대")]
    [SerializeField] private Image fillImage;       // 초록색 막대 (Image Type = Filled, Horizontal, Origin Left)
    [SerializeField] private TMP_Text percentText;  // 선택: "60%" 숫자 (필요 없으면 비워 두기)
    [SerializeField] private float fillSpeed = 1.5f; // 차오르는 속도 (1 = 0%→100%가 1초)

    private float targetFill;

    private void Awake()
    {
        Instance = this;

        if (fillImage != null) fillImage.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        RefreshStage();
    }

    private void Update()
    {
        if (fillImage == null) return;

        // 초록색이 점차 차오르게 (부드럽게 따라가기)
        fillImage.fillAmount = Mathf.MoveTowards(
            fillImage.fillAmount, targetFill, fillSpeed * Time.unscaledDeltaTime);
    }

    // 스테이지 이름 갱신
    public void RefreshStage()
    {
        if (!autoFillStageFromProgress || stageText == null) return;
        if (GameProgressManager.Instance == null) return;

        stageText.text = GameProgressManager.Instance.GetChapterLabel();
    }

    // 직접 글씨를 넣고 싶을 때
    public void SetStage(string label)
    {
        if (stageText != null) stageText.text = label;
    }

    // 조사율 0~1 (예: 60% → 0.6)
    public void SetProgress(float progress01)
    {
        targetFill = Mathf.Clamp01(progress01);

        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(targetFill * 100f) + "%";
        }
    }

    // 조사율을 "획득 수 / 전체 수"로 줄 때
    public void SetProgress(int acquired, int total)
    {
        SetProgress(total > 0 ? (float)acquired / total : 0f);
    }

    // 인벤토리를 열 때마다 0부터 다시 차오르는 연출을 보고 싶을 때 호출
    public void ReplayFill()
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
    }
}