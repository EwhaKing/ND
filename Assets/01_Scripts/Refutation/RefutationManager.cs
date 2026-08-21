using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RefutationManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private ChatDialogueManager chatManager;
    [SerializeField] private RefutationDatabase refutationDatabase;

    [Header("Testimony Settings")]
    [SerializeField] private List<string> testimonyIdList; 
    [SerializeField] private int playerLife = 5;

    [Header("UI Settings")]
    [SerializeField] private GameObject refutationArrowsUI;
    [SerializeField] private GameObject leftArrowBtn;
    [SerializeField] private GameObject rightArrowBtn;
    [SerializeField] private GameObject successPopupUI;
    [SerializeField] private GameObject evidenceSelectionPanel;

    [Header("Standing")]
    [SerializeField] private StandingController standingController;
    [SerializeField] private string characterStandName = "기본";

    [Header("Default Responses")]
    [SerializeField] private string defaultWrongMessage = "이건 모순과 관련 없는 것 같아.";

    private int currentIndex = 0;
    private string lockedTestimonyId; 
    private bool isSelectingEvidence = false; 
    private bool isWaitingForClick = false; // ★ 오답 대사 출력 후 클릭 대기 상태 플래그

    private void Start()
    {
        StartTestimony();
    }

    private void Update()
    {
        // 1. 오답 대사 출력 후 클릭을 기다리는 상태일 때
        if (isWaitingForClick)
        {
            if (Input.GetMouseButtonDown(0)) // 화면 클릭 시
            {
                ReturnToTestimony();
            }
            return; // 클릭할 때까지 다른 입력 차단
        }

        // 2. 증거 선택 창이 열려있지 않을 때만 조작 허용
        if (!isSelectingEvidence)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) OnClickPrev();
            if (Input.GetKeyDown(KeyCode.RightArrow)) OnClickNext();
            
            // 엔터키를 누르면 증거 선택 패널 열기
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                OpenEvidenceSelection();
            }
        }
        else
        {
            // 증거 선택 패널이 열려있을 때 ESC 키를 누르면 패널 닫기
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseEvidenceSelection();
            }
        }
    }

    // 논파 테스트 시작
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

    // 현재 대사 보여주기
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

    // 좌/우 화살표 버튼 클릭 시 호출
    public void OnClickPrev()
    {
        if (isSelectingEvidence || isWaitingForClick) return; 
        if (currentIndex <= 0) return; 

        currentIndex--;
        ShowCurrentLine();
    }

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
    }

    // 증거 선택 후 호출되는 메서드
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
            
            // 오답 시 이름 비우고, 화살표 숨기고, 클릭 대기 상태 진입
            chatManager.ShowSingleLine("", defaultWrongMessage, null); 
            if (refutationArrowsUI != null) refutationArrowsUI.SetActive(false); 
            
            isSelectingEvidence = false;
            isWaitingForClick = true; // ★ 클릭 대기 시작
            
            Debug.Log($"오답! 남은 목숨: {playerLife}");
        }
    }

    // 오답 대사 출력 후 화면 클릭 시 호출되는 복구 함수
    private void ReturnToTestimony()
    {
        isWaitingForClick = false; // ★ 클릭 대기 해제
        isSelectingEvidence = false;

        if (refutationArrowsUI != null) refutationArrowsUI.SetActive(true);
        
        ShowCurrentLine(); // 틀렸던 그 대사부터 다시 시작
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