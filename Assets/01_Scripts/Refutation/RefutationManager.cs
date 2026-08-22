using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RefutationManager : MonoBehaviour
{
    [Header("매니저")]
    [SerializeField] private ChatDialogueManager chatManager;
    [SerializeField] private RefutationDatabase refutationDatabase;

    [Header("논파 관련")]
    [SerializeField] private List<string> testimonyIdList; // 논파할 증언 ID 리스트
    [SerializeField] private int playerLife = 5; // 플레이어 목숨 수

    [Header("UI")]
    [SerializeField] private GameObject refutationArrowsUI;
    [SerializeField] private GameObject leftArrowBtn;
    [SerializeField] private GameObject rightArrowBtn;
    [SerializeField] private GameObject successPopupUI; // 논파 성공 팝업 UI
    [SerializeField] private TextMeshProUGUI lifeText; // 플레이어 목숨 수 표시 텍스트
    
    [Header("인벤토리")]
    [SerializeField] private GameObject evidenceSelectionPanel; // 증거 선택 패널
    [SerializeField] private GameObject evidenceSlotPrefab; // 증거 슬롯 프리팹
    [SerializeField] private Transform evidenceContentParent; // 증거 슬롯들이 들어갈 Content의 Transform

    [Header("Standing")]
    [SerializeField] private StandingController standingController;
    [SerializeField] private string characterStandName = "기본";

    [Header("Default Responses")]
    [SerializeField] private string defaultWrongMessage = "이건 모순과 관련 없는 것 같아.";

    private int currentIndex = 0;
    private string lockedTestimonyId;
    private bool isSelectingEvidence = false;
    private bool isWaitingForClick = false;

    private void Start()
    {
        Time.timeScale = 1f; 
        StartTestimony();
    }

private void Update()
    {
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
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                CloseEvidenceSelection();
            }
        }
    }
    
    // 논파 시작
    public void StartTestimony()
    {
        if (testimonyIdList.Count == 0) return;
        currentIndex = 0;
        isSelectingEvidence = false;
        isWaitingForClick = false;

        UpdateLifeUI();

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);
        if (successPopupUI != null) successPopupUI.SetActive(false);
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(false);

        if (standingController != null)
        {
            StandStep[] stands = new StandStep[]
            {
                new StandStep { standName = characterStandName }
            };
            standingController.SetSprite(stands);
        }

        ShowCurrentLine();
    }

    // 현재 증언 ID에 해당하는 대사와 캐릭터를 화면에 표시
    private void ShowCurrentLine()
    {
        var entry = refutationDatabase.GetRefutationData(testimonyIdList[currentIndex]);
        if (entry != null)
        {
            chatManager.ShowSingleLine(entry.character, entry.dialogue, null);

            if (standingController != null)
            {
                if (!string.IsNullOrEmpty(entry.expression))
                {
                    standingController.SetExpression(entry.expression);
                }
                standingController.SetColor(entry.character);
            }
        }

        if (leftArrowBtn != null) leftArrowBtn.SetActive(currentIndex > 0);
        if (rightArrowBtn != null) rightArrowBtn.SetActive(currentIndex < testimonyIdList.Count - 1);
    }

    // 플레이어 목숨 수 UI 업데이트
    private void UpdateLifeUI()
    {
        if (lifeText != null)
        {
            lifeText.text = $"남은 목숨: {playerLife}";
        }
    }

    // 이전 증언으로 이동
    public void OnClickPrev()
    {
        if (isSelectingEvidence || isWaitingForClick) return; 
        if (currentIndex <= 0) return; 

        currentIndex--;
        ShowCurrentLine();
    }

    // 다음 증언으로 이동
    public void OnClickNext()
    {
        if (isSelectingEvidence || isWaitingForClick) return; 
        if (currentIndex >= testimonyIdList.Count - 1) return; 

        currentIndex++;
        ShowCurrentLine();
    }

    // 엔터키로 증거 선택 패널 열기
    private void OpenEvidenceSelection()
    {
        lockedTestimonyId = testimonyIdList[currentIndex];
        isSelectingEvidence = true;

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false);
        if (evidenceSelectionPanel != null) evidenceSelectionPanel.SetActive(true);

        RefreshEvidenceUI();
    }

    // 인벤토리 데이터를 읽어와 증거 슬롯을 생성하는 함수
    private void RefreshEvidenceUI()
    {
        if (evidenceSlotPrefab == null || evidenceContentParent == null) return;

        foreach (Transform child in evidenceContentParent)
        {
            Destroy(child.gameObject);
        }

        if (InventoryManager.Instance != null)
        {
            foreach (ClueData clue in InventoryManager.Instance.acquiredItems)
            {
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
    }

    // 증거 선택 후 호출되는 함수
    public void OnSelectEvidence(string selectedEvidenceId)
    {
        if (!isSelectingEvidence) return;

        var entry = refutationDatabase.GetRefutationData(lockedTestimonyId);
        if (entry == null) return;

        if (evidenceSelectionPanel != null)
        {
            evidenceSelectionPanel.SetActive(false);
        }

        if (entry.isWeakPoint && entry.correctEvidenceId == selectedEvidenceId)
        {
            chatManager.HideDialogueUI();
            if (successPopupUI != null) successPopupUI.SetActive(true);
            isSelectingEvidence = false;
            isWaitingForClick = false;
        }
        else
        {
            playerLife--;
            UpdateLifeUI();
            string wrongMsg = GetWrongMessage(lockedTestimonyId, selectedEvidenceId);
            
            chatManager.ShowSingleLine("", wrongMsg, null); 
            if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false); 
            
            isSelectingEvidence = false;
            isWaitingForClick = true; 
            
            Debug.Log($"오답! 남은 목숨: {playerLife}");
        }
    }

    // 오답 대사 가져오기
    private string GetWrongMessage(string testimonyId, string evidenceId)
    {
        string customMessage = refutationDatabase.GetCustomWrongMessage(testimonyId, evidenceId);
        
        if (!string.IsNullOrEmpty(customMessage))
        {
            return customMessage;
        }
        
        return defaultWrongMessage;
    }

    // 오답 대사 출력 후 화면 클릭 시 호출되는 복구 함수
    private void ReturnToTestimony()
    {
        isWaitingForClick = false; 
        isSelectingEvidence = false;

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);
        
        ShowCurrentLine(); 
    }

    // 증거 선택 패널 닫기
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