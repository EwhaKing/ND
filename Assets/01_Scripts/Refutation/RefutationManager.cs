using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// RefutationManager
///
/// 담당:
/// - 논파 씬에서 피심판자의 증언을 순서대로 보여주고, 플레이어가 증거를 제시해 모순을 찾는 흐름을 관리합니다.
/// - 좌우 이동 버튼 또는 방향키를 통해 증언을 넘길 수 있습니다.
/// - Enter 입력으로 증거 선택 패널을 열고, 선택한 증거가 현재 증언의 정답 증거인지 판정합니다.
/// - 정답 증거를 제시하면 논파 성공 팝업을 표시합니다.
/// - 오답 증거를 제시하면 플레이어 목숨을 감소시키고, 목숨이 0 이하가 되면 심판 씬으로 이동합니다.
/// - 논파 성공 시 GameProgressManager를 통해 미니게임 씬으로 이동합니다.
///
/// 사용 위치:
/// - RefutationScene의 논파 관리 오브젝트에 붙여 사용합니다.
/// - 논파 UI, 증언 데이터베이스, 대화창, 스탠딩 컨트롤러, 증거 선택 패널을 Inspector에서 연결해야 합니다.
///
/// 연결:
/// - RefutationDatabase에서 testimonyId에 맞는 증언 데이터를 가져옵니다.
/// - ChatDialogueManager를 통해 증언 대사와 오답 대사를 출력합니다.
/// - StandingController를 통해 캐릭터 스탠딩과 표정을 제어합니다.
/// - GameProgressManager.AcquiredClues를 통해 조사 씬에서 획득한 단서를 논파 증거로 사용합니다.
/// - GameProgressManager.OnRefutationFinished(true/false)를 호출하여 성공/실패 분기를 전체 진행 흐름에 전달합니다.
///
/// TODO:
/// - 현재는 정답 1회 성공 시 바로 성공 팝업을 표시합니다. 여러 논파 단계를 사용할 경우 성공 카운트 구조 추가 필요
/// - 목숨 0일 때 바로 JudgeScene으로 이동하는 흐름이 맞는지 기획 확인 필요
/// - 증거 슬롯에 단서 이름/설명 툴팁을 표시하는 기능 추가 검토
/// - successPopupUI 확인 버튼에 ConfirmSuccess()가 연결되어 있는지 Inspector 확인 필요
/// - RefutationScene에서 GameProgressManager가 없을 때 단독 테스트할 수 있는 테스트 모드 추가 검토
/// </summary>
public class RefutationManager : MonoBehaviour
{
    [Header("매니저")]
    [SerializeField] private ChatDialogueManager chatManager;
    [SerializeField] private RefutationDatabase refutationDatabase;

    [Header("논파 관련")]
    [SerializeField] private List<string> testimonyIdList;
    [SerializeField] private int playerLife = 5;

    [Header("UI")]
    [SerializeField] private GameObject refutationArrowsUI;
    [SerializeField] private GameObject leftArrowBtn;
    [SerializeField] private GameObject rightArrowBtn;
    [SerializeField] private GameObject successPopupUI;
    [SerializeField] private TextMeshProUGUI lifeText;

    [Header("인벤토리")]
    [SerializeField] private GameObject evidenceSelectionPanel;
    [SerializeField] private GameObject evidenceSlotPrefab;
    [SerializeField] private Transform evidenceContentParent;

    [Header("Standing")]
    [SerializeField] private StandingController standingController;
    [SerializeField] private string characterStandName = "기본";

    [Header("Default Responses")]
    [SerializeField] private string defaultWrongMessage =
        "이건 모순과 관련 없는 것 같아.";

    private int currentIndex = 0;
    private string lockedTestimonyId;
    private bool isSelectingEvidence = false;
    private bool isWaitingForClick = false;
    private bool isRefutationFinished = false;

    private void Start()
    {
        Time.timeScale = 1f;

        StartTestimony();
    }

    private void Update()
    {
        if (isRefutationFinished)
        {
            return;
        }

        if (isWaitingForClick)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ReturnToTestimony();
            }

            return;
        }

        if (!isSelectingEvidence)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                OnClickPrev();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                OnClickNext();
            }

            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                OpenEvidenceSelection();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                CloseEvidenceSelection();
            }
        }
    }

    /// <summary>
    /// 논파를 시작하고 첫 번째 증언을 표시합니다.
    /// </summary>
    public void StartTestimony()
    {
        if (testimonyIdList == null ||
            testimonyIdList.Count == 0)
        {
            Debug.LogWarning("논파할 증언 ID가 없습니다.");
            return;
        }

        currentIndex = 0;
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = false;

        UpdateLifeUI();

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(true);
        }

        if (successPopupUI != null)
        {
            successPopupUI.SetActive(false);
        }

        if (evidenceSelectionPanel != null)
        {
            evidenceSelectionPanel.SetActive(false);
        }

        if (standingController != null)
        {
            StandStep[] stands =
            {
                new StandStep
                {
                    standName = characterStandName
                }
            };

            standingController.SetSprite(stands);
        }

        ShowCurrentLine();
    }

    /// <summary>
    /// 현재 증언 ID에 해당하는 대사와 캐릭터 상태를 화면에 표시합니다.
    /// </summary>
    private void ShowCurrentLine()
    {
        if (refutationDatabase == null)
        {
            Debug.LogError("RefutationDatabase가 연결되지 않았습니다.");
            return;
        }

        string currentTestimonyId =
            testimonyIdList[currentIndex];

        var entry =
            refutationDatabase.GetRefutationData(currentTestimonyId);

        if (entry != null)
        {
            if (chatManager != null)
            {
                chatManager.ShowSingleLine(
                    entry.character,
                    entry.dialogue,
                    null
                );
            }

            if (standingController != null)
            {
                if (!string.IsNullOrWhiteSpace(entry.expression))
                {
                    standingController.SetExpression(entry.expression);
                }

                standingController.SetColor(entry.character);
            }
        }

        if (leftArrowBtn != null)
        {
            leftArrowBtn.SetActive(currentIndex > 0);
        }

        if (rightArrowBtn != null)
        {
            rightArrowBtn.SetActive(
                currentIndex < testimonyIdList.Count - 1
            );
        }
    }

    /// <summary>
    /// 플레이어 목숨 수 UI를 갱신합니다.
    /// </summary>
    private void UpdateLifeUI()
    {
        if (lifeText != null)
        {
            lifeText.text = $"남은 목숨: {playerLife}";
        }
    }

    /// <summary>
    /// 이전 증언으로 이동합니다.
    /// </summary>
    public void OnClickPrev()
    {
        if (isSelectingEvidence ||
            isWaitingForClick)
        {
            return;
        }

        if (currentIndex <= 0)
        {
            return;
        }

        currentIndex--;

        ShowCurrentLine();
    }

    /// <summary>
    /// 다음 증언으로 이동합니다.
    /// </summary>
    public void OnClickNext()
    {
        if (isSelectingEvidence ||
            isWaitingForClick)
        {
            return;
        }

        if (currentIndex >= testimonyIdList.Count - 1)
        {
            return;
        }

        currentIndex++;

        ShowCurrentLine();
    }

    /// <summary>
    /// 현재 증언에 대해 제시할 증거를 선택하는 패널을 엽니다.
    /// </summary>
    private void OpenEvidenceSelection()
    {
        if (testimonyIdList == null ||
            testimonyIdList.Count == 0)
        {
            return;
        }

        lockedTestimonyId = testimonyIdList[currentIndex];
        isSelectingEvidence = true;

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(false);
        }

        if (evidenceSelectionPanel != null)
        {
            evidenceSelectionPanel.SetActive(true);
        }

        RefreshEvidenceUI();
    }

    /// <summary>
    /// GameProgressManager에 저장된 획득 단서 목록을 읽어와 증거 슬롯을 생성합니다.
    /// </summary>
    private void RefreshEvidenceUI()
    {
        if (evidenceSlotPrefab == null ||
            evidenceContentParent == null)
        {
            Debug.LogWarning("증거 슬롯 프리팹 또는 Content Parent가 연결되지 않았습니다.");
            return;
        }

        foreach (Transform child in evidenceContentParent)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<ClueData> clues = null;

        if (GameProgressManager.Instance != null)
        {
            clues = GameProgressManager.Instance.AcquiredClues;
        }

        if ((clues == null || clues.Count == 0) &&
            InventoryManager.Instance != null)
        {
            clues = InventoryManager.Instance.acquiredItems;
        }

        if (clues == null ||
            clues.Count == 0)
        {
            Debug.LogWarning("논파에 사용할 증거가 없습니다.");
            return;
        }

        foreach (ClueData clue in clues)
        {
            if (clue == null)
            {
                continue;
            }

            GameObject newSlotObj = Instantiate(
                evidenceSlotPrefab,
                evidenceContentParent
            );

            Button slotBtn =
                newSlotObj.GetComponent<Button>();

            if (slotBtn != null)
            {
                string capturedClueID = clue.clueID;

                slotBtn.onClick.AddListener(
                    () => OnSelectEvidence(capturedClueID)
                );
            }

            Transform iconTransform =
                newSlotObj.transform.Find("ClueIcon");

            if (iconTransform != null &&
                clue.clueIcon != null)
            {
                Image iconImage =
                    iconTransform.GetComponent<Image>();

                if (iconImage != null)
                {
                    iconImage.sprite = clue.clueIcon;
                }
            }
        }
    }

    /// <summary>
    /// 플레이어가 증거를 선택했을 때 정답 여부를 판정합니다.
    /// </summary>
    public void OnSelectEvidence(string selectedEvidenceId)
    {
        if (!isSelectingEvidence ||
            isRefutationFinished)
        {
            return;
        }

        if (refutationDatabase == null)
        {
            Debug.LogError("RefutationDatabase가 연결되지 않았습니다.");
            return;
        }

        var entry =
            refutationDatabase.GetRefutationData(
                lockedTestimonyId
            );

        if (entry == null)
        {
            return;
        }

        if (evidenceSelectionPanel != null)
        {
            evidenceSelectionPanel.SetActive(false);
        }

        if (entry.isWeakPoint &&
            entry.correctEvidenceId == selectedEvidenceId)
        {
            HandleCorrectEvidence();
        }
        else
        {
            HandleWrongEvidence(selectedEvidenceId);
        }
    }

    /// <summary>
    /// 정답 증거를 선택했을 때 논파 성공 처리를 합니다.
    /// </summary>
    private void HandleCorrectEvidence()
    {
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = true;

        if (chatManager != null)
        {
            chatManager.HideDialogueUI();
        }

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(false);
        }

        if (successPopupUI != null)
        {
            successPopupUI.SetActive(true);
        }
        else
        {
            ConfirmSuccess();
        }
    }

    /// <summary>
    /// 오답 증거를 선택했을 때 목숨 차감 및 실패 여부를 처리합니다.
    /// </summary>
    private void HandleWrongEvidence(string selectedEvidenceId)
    {
        playerLife--;
        UpdateLifeUI();

        Debug.Log($"오답! 남은 목숨: {playerLife}");

        if (playerLife <= 0)
        {
            FailRefutation();
            return;
        }

        string wrongMsg = GetWrongMessage(
            lockedTestimonyId,
            selectedEvidenceId
        );

        if (chatManager != null)
        {
            chatManager.ShowSingleLine(
                string.Empty,
                wrongMsg,
                null
            );
        }

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(false);
        }

        isSelectingEvidence = false;
        isWaitingForClick = true;
    }

    /// <summary>
    /// 성공 팝업의 확인 버튼에서 호출합니다.
    /// 논파 성공 결과를 GameProgressManager에 전달합니다.
    /// </summary>
    public void ConfirmSuccess()
    {
        if (successPopupUI != null)
        {
            successPopupUI.SetActive(false);
        }

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnRefutationFinished(true);
        }
        else
        {
            Debug.LogError("GameProgressManager가 없습니다.");
        }
    }

    /// <summary>
    /// 논파 실패 결과를 GameProgressManager에 전달합니다.
    /// </summary>
    private void FailRefutation()
    {
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = true;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnRefutationFinished(false);
        }
        else
        {
            Debug.LogError("GameProgressManager가 없습니다.");
        }
    }

    /// <summary>
    /// 선택한 증거에 맞는 오답 대사를 가져옵니다.
    /// 별도 지정된 오답 대사가 없으면 기본 오답 대사를 반환합니다.
    /// </summary>
    private string GetWrongMessage(
        string testimonyId,
        string evidenceId)
    {
        string customMessage =
            refutationDatabase.GetCustomWrongMessage(
                testimonyId,
                evidenceId
            );

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            return customMessage;
        }

        return defaultWrongMessage;
    }

    /// <summary>
    /// 오답 대사 출력 후 화면 클릭 시 다시 현재 증언 화면으로 복귀합니다.
    /// </summary>
    private void ReturnToTestimony()
    {
        isWaitingForClick = false;
        isSelectingEvidence = false;

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(true);
        }

        ShowCurrentLine();
    }

    /// <summary>
    /// 증거 선택 패널을 닫고 증언 화면으로 돌아갑니다.
    /// </summary>
    public void CloseEvidenceSelection()
    {
        if (evidenceSelectionPanel != null)
        {
            evidenceSelectionPanel.SetActive(false);
        }

        if (refutationArrowsUI != null)
        {
            refutationArrowsUI.SetActive(true);
        }

        isSelectingEvidence = false;
        isWaitingForClick = false;
    }
}