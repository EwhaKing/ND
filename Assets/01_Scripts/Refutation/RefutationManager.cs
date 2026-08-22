using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    private void Awake()
    {
        Debug.Log("[디버그] RefutationManager의 Awake() 실행됨!");
    }

    private void OnEnable()
    {   
        Debug.Log("[디버그] RefutationManager가 켜졌습니다 (OnEnable)");
    }

    private void OnDisable()
    {
        // ★ using System.Diagnostics를 쓰지 않고 풀네임으로 적어 충돌을 막습니다.
        Debug.Log("[디버그] 🚨 RefutationManager가 꺼진 원인 (스택 트레이스):\n" + new System.Diagnostics.StackTrace());
    }

    private void Start()
    {
        Debug.Log("[디버그] RefutationManager의 Start() 실행됨!");
        Time.timeScale = 1f; 
        StartTestimony();
    }

    private void Update()
    {
        // 1. 오답 대사 출력 후 클릭을 기다리는 상태일 때
        if (isWaitingForClick)
        {
            if (Input.GetMouseButtonDown(0)) 
            {
                ReturnToTestimony();
            }
            return; 
        }

        // 2. 증거 선택 창이 열려있지 않을 때만 조작 허용
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

    // 논파 시작
    public void StartTestimony()
    {
        if (testimonyIdList.Count == 0) return;
        currentIndex = 0;
        isSelectingEvidence = false;
        isWaitingForClick = false;

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

                // 이후 newSlotObj 내부의 Image 컴포넌트를 찾아 clue.clueIcon을 넣고
                // Text 컴포넌트를 찾아 clue.clueName을 넣어주는 코드 작성 예정
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
            Debug.Log("논파 성공!");
        }
        else
        {
            playerLife--;
            
            string wrongMsg = GetWrongMessage(lockedTestimonyId, selectedEvidenceId);
            
            chatManager.ShowSingleLine("", wrongMsg, null); 
            if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false); 
            
            isSelectingEvidence = false;
            isWaitingForClick = true; 
            
            Debug.Log($"오답! 남은 목숨: {playerLife}");
        }
    }

    // 오답 대사 가져오기 (현재는 기본 오답 메시지 반환)
    private string GetWrongMessage(string testimonyId, string evidenceId)
    {
        // 이후 CSV에서 특정 오답 대사를 찾아오는 코드 추가 예정
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