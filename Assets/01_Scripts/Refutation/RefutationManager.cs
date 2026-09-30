using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RefutationManager : MonoBehaviour
{
    [Header("UI 그룹 (부모 오브젝트)")]
    [SerializeField] private GameObject generalDialogueGroup;  // GeneralDialogueGroup
    [SerializeField] private GameObject refutationUIGroup;       // RefutationUIGroup

    [Header("매니저")]
    [SerializeField] private ChatDialogueManager generalDialogueManager;   // DialougeManager_1
    [SerializeField] private ChatDialogueManager testimonyDialogueManager;  // DialougeManager_2
    [SerializeField] private RefutationDatabase refutationDatabase;

    [Header("논파 관련")]
    [SerializeField] private List<string> testimonyIdList;
    [SerializeField] private int playerLife = 5;
    [SerializeField] private float successCutinDuration = 3.5f; // 논파 성공 컷인 지속 시간 (초)

    private Coroutine successCutinCoroutine;

    [Header("논파 내부 UI 세부요소")]
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
    [SerializeField] private string defaultWrongMessage = "이건 모순과 관련 없는 것 같아.";

    private int currentIndex = 0;
    private string lockedTestimonyId;
    private bool isSelectingEvidence = false;
    private bool isWaitingForClick = false;
    private bool isRefutationFinished = false;

    private Action onSuccessCallback;
    private Action onFailCallback;

    private void Awake()
    {
        // 💡 씬 실행 즉시 증거창과 성공 팝업을 강제로 꺼둡니다.
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);
        if (successPopupUI != null) successPopupUI.SetActive(false);
    }

    private void Update()
    {
        if (isRefutationFinished) return;

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
            if (Input.GetKeyDown(KeyCode.LeftArrow)) OnClickPrev();
            if (Input.GetKeyDown(KeyCode.RightArrow)) OnClickNext();

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                OpenEvidenceSelection();
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseEvidenceSelection();
            }
        }
    }

    public void StartRefutation(List<string> testimonies, Action onSuccess, Action onFail)
    {
        this.testimonyIdList = testimonies;
        this.onSuccessCallback = onSuccess;
        this.onFailCallback = onFail;

        gameObject.SetActive(true);
        StartTestimony();
    }

    public void StartTestimony()
    {
        if (testimonyIdList == null || testimonyIdList.Count == 0) return;

        currentIndex = 0;
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = false;

        // 💡 UI 초기 상태 리셋
        if (refutationUIGroup != null) refutationUIGroup.SetActive(true);
        if (generalDialogueGroup != null) generalDialogueGroup.SetActive(false);

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);
        if (successPopupUI != null) successPopupUI.SetActive(false);
        
        // 💡 증거 선택 창(CluePanel)을 명시적으로 숨김
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);

        UpdateLifeUI();

        if (standingController != null)
        {
            StandStep[] stands = { new StandStep { standName = characterStandName } };
            standingController.SetSprite(stands);
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (refutationDatabase == null) return;

        string currentTestimonyId = testimonyIdList[currentIndex];
        var entry = refutationDatabase.GetRefutationData(currentTestimonyId);

        if (entry != null)
        {
            if (testimonyDialogueManager != null)
            {
                testimonyDialogueManager.ShowDialogueUI();
                testimonyDialogueManager.ShowSingleLine(entry.character, entry.dialogue, null);
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

        if (leftArrowBtn != null) leftArrowBtn.SetActive(currentIndex > 0);
        if (rightArrowBtn != null) rightArrowBtn.SetActive(currentIndex < testimonyIdList.Count - 1);
    }

    private void HandleWrongEvidence(string selectedEvidenceId)
    {
        playerLife--;
        UpdateLifeUI();

        if (playerLife <= 0)
        {
            FailRefutation();
            return;
        }

        isSelectingEvidence = false;

        // 💡 2. 오답 연출 시: 논파 UI 그룹을 끄고, 일반 대화 UI 그룹을 켬
        if (refutationUIGroup != null) refutationUIGroup.SetActive(false);
        if (generalDialogueGroup != null) generalDialogueGroup.SetActive(true);

        var customWrongDialogue = refutationDatabase.GetCustomWrongDialogue(lockedTestimonyId, selectedEvidenceId);

        if (customWrongDialogue != null && customWrongDialogue.Count > 0)
        {
            List<ChatDialogueManager.DialogueLine> lines = new();
            foreach (var item in customWrongDialogue)
            {
                lines.Add(new ChatDialogueManager.DialogueLine
                {
                    speaker = item.speaker,
                    dialogue = item.wrongMessage
                });
            }

            if (generalDialogueManager != null)
            {
                generalDialogueManager.ShowDialogueUI();
                generalDialogueManager.StartDialogue(lines.ToArray(), ReturnToTestimony);
            }
            else
            {
                ReturnToTestimony();
            }
        }
        else
        {
            if (generalDialogueManager != null)
            {
                generalDialogueManager.ShowDialogueUI();
                generalDialogueManager.ShowSingleLine(string.Empty, defaultWrongMessage, null);
            }
            isWaitingForClick = true;
        }
    }

    private void ReturnToTestimony()
    {
        isWaitingForClick = false;
        isSelectingEvidence = false;

        // 💡 3. 증언으로 복귀 시: 일반 대화 UI 그룹을 끄고, 논파 UI 그룹을 켬
        if (generalDialogueGroup != null) generalDialogueGroup.SetActive(false);
        if (refutationUIGroup != null) refutationUIGroup.SetActive(true);

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);
        ShowCurrentLine();
    }

    private void UpdateLifeUI()
    {
        if (lifeText != null) lifeText.text = $"남은 목숨: {playerLife}";
    }

    public void OnClickPrev()
    {
        if (isSelectingEvidence || isWaitingForClick || currentIndex <= 0) return;
        currentIndex--;
        ShowCurrentLine();
    }

    public void OnClickNext()
    {
        if (isSelectingEvidence || isWaitingForClick || currentIndex >= testimonyIdList.Count - 1) return;
        currentIndex++;
        ShowCurrentLine();
    }

    private void OpenEvidenceSelection()
    {
        if (testimonyIdList == null || testimonyIdList.Count == 0) return;

        lockedTestimonyId = testimonyIdList[currentIndex];
        isSelectingEvidence = true;

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false);
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(true);

        RefreshEvidenceUI();
    }

    private void RefreshEvidenceUI()
    {
        if (evidenceSlotPrefab == null || evidenceContentParent == null) return;

        foreach (Transform child in evidenceContentParent) Destroy(child.gameObject);

        IReadOnlyList<ClueData> clues = null;
        if (GameProgressManager.Instance != null) clues = GameProgressManager.Instance.AcquiredClues;
        if ((clues == null || clues.Count == 0) && InventoryManager.Instance != null) clues = InventoryManager.Instance.acquiredItems;

        if (clues == null || clues.Count == 0) return;

        foreach (ClueData clue in clues)
        {
            if (clue == null) continue;

            GameObject newSlotObj = Instantiate(evidenceSlotPrefab, evidenceContentParent);
            Button slotBtn = newSlotObj.GetComponent<Button>();

            if (slotBtn != null)
            {
                string capturedClueID = clue.clueID;
                slotBtn.onClick.AddListener(() => OnSelectEvidence(capturedClueID));
            }

            Transform iconTransform = newSlotObj.transform.Find("ClueIcon");
            if (iconTransform != null && clue.clueIcon != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null) iconImage.sprite = clue.clueIcon;
            }
        }
    }

    public void OnSelectEvidence(string selectedEvidenceId)
    {
        if (!isSelectingEvidence || isRefutationFinished || refutationDatabase == null) return;

        var entry = refutationDatabase.GetRefutationData(lockedTestimonyId);
        if (entry == null) return;

        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);

        if (entry.isWeakPoint && entry.correctEvidenceId == selectedEvidenceId)
        {
            HandleCorrectEvidence();
        }
        else
        {
            HandleWrongEvidence(selectedEvidenceId);
        }
    }

    private void HandleCorrectEvidence()
    {
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = true; // 💡 Update() 내의 마우스/키보드 입력을 완전히 차단

        // 단서 창 및 논파 UI 요소 숨김
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);
        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false);

        // 연출 코루틴 시작
        if (successCutinCoroutine != null) StopCoroutine(successCutinCoroutine);
        successCutinCoroutine = StartCoroutine(CoSuccessCutinRoutine());
    }

    private System.Collections.IEnumerator CoSuccessCutinRoutine()
    {
        // 1. 컷인 / 정답 텍스트 UI 활성화
        if (successPopupUI != null)
        {
            successPopupUI.SetActive(true);
        }

        // 2. 설정한 시간(예: 3.5초) 동안 클릭/입력을 막고 대기
        yield return new WaitForSeconds(successCutinDuration);

        // 3. 시간이 지나면 컷인 UI를 끄고 자동으로 다음 DIALOGUE 단계로 진행
        ConfirmSuccess();
    }

    public void ConfirmSuccess()
    {
        if (successPopupUI != null) successPopupUI.SetActive(false);
        if (refutationUIGroup != null) refutationUIGroup.SetActive(false);
        gameObject.SetActive(false);

        onSuccessCallback?.Invoke();
    }

    private void FailRefutation()
    {
        isSelectingEvidence = false;
        isWaitingForClick = false;
        isRefutationFinished = true;

        if (generalDialogueGroup != null) generalDialogueGroup.SetActive(false);
        if (refutationUIGroup != null) refutationUIGroup.SetActive(false);
        gameObject.SetActive(false);

        onFailCallback?.Invoke();
    }

    public void CloseEvidenceSelection()
    {
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);
        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);

        isSelectingEvidence = false;
        isWaitingForClick = false;
    }
}