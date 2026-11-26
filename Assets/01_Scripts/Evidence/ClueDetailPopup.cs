using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueDetailPopup : MonoBehaviour
{
    public static ClueDetailPopup Instance { get; private set; }

    [Header("연결할 대화 UI")]
    [SerializeField] private GameObject dialoguePanel;

    private bool dialogueWasActive;

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Image clueImage;
    [SerializeField] private TMP_Text clueNameText;
    [SerializeField] private TMP_Text clueDescText;

    [Header("열리는 연출 (popupPanel에 붙인 PopupSequence)")]
    [SerializeField] private PopupSequence popupSequence;

    [Header("안전장치")]
    [Tooltip("연출에 문제가 생겨도 시간(초)이 지나면 닫을 수 있음")]
    [SerializeField] private float safetyUnlockTime = 2f;
    [Tooltip("연출 후 클릭 막혀 있는 시간(초)")]
    [SerializeField] private float readDelay = 0.5f;

    private bool canClose = false;

    // 팝업이 닫힌 뒤 실행할 함수 (조사율/핵심단서 갱신 등)
    private Action onCloseCallback;

    // 지금 떠 있는 단서
    private ClueData currentClue;

    // 팝업이 떠 있는 동안 들어온 다른 단서 요청 (잃어버리지 않고 순서대로 보여줌)
    private struct PendingPopup
    {
        public ClueData clue;
        public Action onClose;
    }
    private readonly List<PendingPopup> pendingPopups = new List<PendingPopup>();

    // 다른 스크립트(예: 클릭 감지)에서 "지금 팝업이 떠 있는지" 확인할 때 사용
    public bool IsOpen => popupPanel != null && popupPanel.activeSelf;

    // 팝업이 닫힌 바로 그 프레임의 클릭까지 막고 싶을 때 사용 (월드 클릭이 같이 처리되는 것 방지)
    private int closedFrame = -1;
    public bool IsBusy => IsOpen || Time.frameCount <= closedFrame;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (popupSequence == null)
        {
            Debug.LogWarning("ClueDetailPopup: Popup Sequence 가 연결되어 있지 않음. " +
                             "연출 없이 바로 열리고, 클릭으로 일찍 닫힐 수 있습니다.", this);
        }
    }

    private void Update()
    {
        // 다른 스크립트가 popupPanel을 직접 꺼버린 경우에도
        // 닫힌 뒤 실행할 함수(조사율 갱신, 대화)가 사라지지 않게 처리
        if (currentClue != null && !IsOpen)
        {
            CloseNow();
            return;
        }

    }

    public void ShowPopup(
        ClueData clueData,
        Action onClose = null)
    {
        if (clueData == null)
        {
            return;
        }

        // 이미 팝업이 떠 있을 때(빠르게 여러 번 눌렀을 때)
        // 이전 요청의 "닫힌 뒤 실행할 함수"를 덮어쓰지 않고 보관한다.
        if (IsOpen)
        {
            if (clueData == currentClue)
            {
                // 같은 단서를 또 눌렀음 → 함수만 합쳐 두고, 연출은 다시 시작하지 않음
                onCloseCallback += onClose;
            }
            else
            {
                AddPending(clueData, onClose);
            }
            return;
        }

        Display(clueData, onClose);
    }

    private void AddPending(ClueData clueData, Action onClose)
    {
        for (int i = 0; i < pendingPopups.Count; i++)
        {
            if (pendingPopups[i].clue == clueData)
            {
                PendingPopup p = pendingPopups[i];
                p.onClose += onClose;
                pendingPopups[i] = p;
                return;
            }
        }

        pendingPopups.Add(new PendingPopup { clue = clueData, onClose = onClose });
    }

    // 실제로 팝업을 띄우는 부분
    private void Display(ClueData clueData, Action onClose)
    {
        currentClue = clueData;

        // 이번 팝업이 닫힌 뒤 실행할 함수 저장
        onCloseCallback = onClose;

        // 이미지 설정
        if (clueImage != null)
        {
            if (clueData.clueIcon != null)
            {
                clueImage.sprite = clueData.clueIcon;
                clueImage.gameObject.SetActive(true);
            }
            else
            {
                clueImage.gameObject.SetActive(false);
            }
        }

        // 이름
        if (clueNameText != null)
        {
            clueNameText.text = clueData.clueName;
        }

        // 설명
        if (clueDescText != null)
        {
            string firstClickFallback =
                GetFirstClickFallbackText(clueData);

            if (!string.IsNullOrEmpty(
                    clueData.inventoryDescription))
            {
                clueDescText.text =
                    clueData.inventoryDescription;
            }
            else if (!string.IsNullOrEmpty(firstClickFallback))
            {
                clueDescText.text = firstClickFallback;
            }
            else if (!string.IsNullOrEmpty(
                         clueData.secondClickText))
            {
                clueDescText.text =
                    clueData.secondClickText;
            }
            else
            {
                clueDescText.text =
                    "설명이 없습니다.";
            }
        }

        if (dialoguePanel != null)
        {
            dialogueWasActive = dialoguePanel.activeSelf;
            dialoguePanel.SetActive(false);
        }

        // 팝업 켜기
        popupPanel.SetActive(true);
        popupPanel.transform.SetAsLastSibling();

        // 연출이 끝나기 전에는 닫히지 않게 막기
        canClose = false;
        CancelInvoke(nameof(EnableClose));
        CancelInvoke(nameof(AllowClose));

        if (popupSequence != null)
        {
            // 글리치 프레임 → 내용 fade in 연출이 끝나면 닫을 수 있게 됨
            popupSequence.Open(EnableClose);

            // 연출에 문제가 생겨도 영원히 안 닫히는 일이 없도록 안전장치
            Invoke(nameof(EnableClose), safetyUnlockTime);
        }
        else
        {
            // 연출이 연결되지 않았으면 기존 방식 그대로
            Invoke(nameof(EnableClose), 0.1f);
        }
    }

    // firstClickText(DialogueEntry[])에서 설명 대체용으로 쓸 첫 번째 비어있지 않은 대사를 반환
    private string GetFirstClickFallbackText(ClueData clueData)
    {
        if (clueData.firstClickText == null)
        {
            return null;
        }

        foreach (DialogueEntry entry in clueData.firstClickText)
        {
            if (!string.IsNullOrEmpty(entry.text))
            {
                return entry.text;
            }
        }

        return null;
    }

    private void AllowClose()
    {
        canClose = true;
    }
    private void EnableClose()
    {
        CancelInvoke(nameof(EnableClose));
        CancelInvoke(nameof(AllowClose));
        Invoke(nameof(AllowClose), readDelay);
    }

    // 연출이 끝난 뒤에만 닫힘 (연출 중에는 어디서 불러도 무시)
    public void ClosePopup()
    {
        if (!IsOpen || !canClose)
        {
            return;
        }

        CloseNow();
    }

    // 연출과 상관없이 즉시 닫아야 할 때(씬 전환 등) 사용
    public void ForceClose()
    {
        if (!IsOpen)
        {
            return;
        }

        CloseNow();
    }

    private void CloseNow()
    {
        Debug.Log("[ClueDetailPopup] 닫힘 / canClose=" + canClose + "\n" +
                  StackTraceUtility.ExtractStackTrace());

        canClose = false;
        CancelInvoke(nameof(EnableClose));
        CancelInvoke(nameof(AllowClose));
        closedFrame = Time.frameCount;

        // 팝업 닫기
        popupPanel.SetActive(false);

        // 팝업 뒤에 숨겨두었던 대화창 다시 표시
        if (dialoguePanel != null && dialogueWasActive)
        {
            dialoguePanel.SetActive(true);
        }

        Action callback = onCloseCallback;
        onCloseCallback = null;
        currentClue = null;

        // 조사율/핵심단서 갱신 등 닫힌 뒤 실행할 함수들
        callback?.Invoke();

        // 기다리고 있던 단서가 있으면 이어서 보여줌
        // (위 함수 안에서 이미 새 팝업이 열렸다면 그대로 둔다)
        if (!IsOpen && pendingPopups.Count > 0)
        {
            PendingPopup next = pendingPopups[0];
            pendingPopups.RemoveAt(0);
            Display(next.clue, next.onClose);
        }
    }
}