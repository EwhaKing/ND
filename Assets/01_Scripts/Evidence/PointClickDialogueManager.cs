using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// PointClickDialogueManager
///
/// 담당:
/// - 포인트 앤 클릭 조사 파트에서 사용하는 간단한 대화창을 관리
/// - 조사 오브젝트 클릭 시 출력할 대사(DialogueEntry)들을 Queue에 저장하고 순서대로 표시
/// - 대사마다 캐릭터 이름, 스탠딩 이미지, 대사 텍스트를 함께 갱신
/// - 대화가 끝나면 대화 패널을 비활성화
/// - 단서 획득 후 필요한 경우 인벤토리 UI와 조사 진행 UI를 갱신
/// - 대화 종료 시 외부에서 등록한 콜백을 호출 가능
///
/// 사용 위치:
/// - 포인트 앤 클릭 조사 씬의 조사 대화창 UI 오브젝트에 부착
/// - ClueInteract 등 조사 오브젝트 상호작용 스크립트에서 호출 가능
///
/// 연결:
/// - InventoryManager와 연결되어 단서 획득 후 인벤토리 UI를 갱신
/// - InvestigationManager와 연결되어 조사 진행도 UI를 갱신
/// - 조사 대화 종료 후 특정 처리가 필요한 경우 onDialogueClosedCallback을 통해 후속 동작을 실행
///
/// TODO:
/// - ChatDialogueManager와 역할이 겹치는 부분이 있으므로 추후 대화 시스템 통합 여부 검토
/// - 현재는 조사 전용 대화창으로 유지하되, 이름을 InvestigationDialogueManager로 변경하는 것 검토
/// - 대화 출력 방식에 타이핑 효과, 지문 타입, CG 타입을 적용할지 검토
/// - Singleton 구조 유지 여부 검토
/// </summary>
public class PointClickDialogueManager : MonoBehaviour
{
    public static PointClickDialogueManager Instance;

    [Header("UI 컴포넌트 연결")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI nameText;        // 캐릭터 이름 텍스트
    public GameObject nameBox;              // 이름 텍스트 배경 오브젝트 (없으면 nameText 오브젝트 자체를 숨김)
    public Image standingImage;             // 스탠딩 이미지

    private Queue<DialogueEntry> sentences = new Queue<DialogueEntry>(); // 대사 저장용 큐
    private bool pendingInventoryRefresh = false; // 인벤토리 UI 갱신 여부 확인용 플래그
    public Action onDialogueClosedCallback = null; // 대화창이 닫힐 때 호출될 콜백

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // 여러 줄 대사 출력용 함수 (이름/스탠딩 포함)
    public void ShowTexts(DialogueEntry[] entriesToShow, bool shouldRefreshInventory = false)
    {
        if (dialoguePanel == null || dialogueText == null) return;

        sentences.Clear();

        if (entriesToShow != null)
        {
            foreach (DialogueEntry entry in entriesToShow)
            {
                // 대사 내용이 비어 있는 항목은 건너뛴다
                if (!string.IsNullOrEmpty(entry.text))
                {
                    sentences.Enqueue(entry);
                }
            }
        }

        pendingInventoryRefresh = shouldRefreshInventory;

        DisplayNextSentence();
    }

    // 여러 줄 대사 출력용 함수 (문자열만 있는 기존 방식 호환: 이름/스탠딩 없는 지문으로 처리)
    public void ShowTexts(string[] textsToShow, bool shouldRefreshInventory = false)
    {
        List<DialogueEntry> entries = new List<DialogueEntry>();

        if (textsToShow != null)
        {
            foreach (string text in textsToShow)
            {
                entries.Add(DialogueEntry.Narration(text));
            }
        }

        ShowTexts(entries.ToArray(), shouldRefreshInventory);
    }

    // 한 줄 대사 출력용 함수 (이름/스탠딩 포함)
    public void ShowText(DialogueEntry entryToShow, bool shouldRefreshInventory = false)
    {
        ShowTexts(new DialogueEntry[] { entryToShow }, shouldRefreshInventory);
    }

    // 한 줄 대사 출력용 함수 (문자열만, 지문으로 처리)
    public void ShowText(string textToShow, bool shouldRefreshInventory = false)
    {
        ShowText(DialogueEntry.Narration(textToShow), shouldRefreshInventory);
    }

    // 다음 대사 출력용 함수
    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            FinishDialogue();
            return;
        }

        DialogueEntry current = sentences.Dequeue();

        dialogueText.text = current.text;
        ApplyName(current.characterName);
        ApplyStanding(current.standingSprite);

        dialoguePanel.SetActive(true);
    }

    // 캐릭터 이름 갱신 (이름이 없으면 이름창을 숨김)
    private void ApplyName(string characterName)
    {
        bool hasName = !string.IsNullOrEmpty(characterName);

        if (nameText != null)
        {
            nameText.text = hasName ? characterName : string.Empty;
        }

        GameObject nameRoot = nameBox != null
            ? nameBox
            : (nameText != null ? nameText.gameObject : null);

        if (nameRoot != null)
        {
            nameRoot.SetActive(hasName);
        }
    }

    // 스탠딩 이미지 갱신 (스프라이트가 없으면 이미지를 숨김)
    private void ApplyStanding(Sprite sprite)
    {
        if (standingImage == null) return;

        if (sprite != null)
        {
            standingImage.sprite = sprite;
            standingImage.gameObject.SetActive(true);
        }
        else
        {
            standingImage.gameObject.SetActive(false);
        }
    }

    // 다음 대사가 있는지 확인
    public void CloseDialogue()
    {
        if (sentences.Count > 0)
        {
            DisplayNextSentence();
            return;
        }

        FinishDialogue();
    }

    // 대사 종료 처리 및 인벤토리 UI 갱신
    private void FinishDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // 다음 대화를 위해 스탠딩/이름 표시 초기화
        if (standingImage != null) standingImage.gameObject.SetActive(false);
        ApplyName(string.Empty);

        if (pendingInventoryRefresh)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.UpdateInventoryUI();
            }

            if (InvestigationManager.Instance != null)
            {
                InvestigationManager.Instance.UpdateUI();
            }

            pendingInventoryRefresh = false;
        }

        if (onDialogueClosedCallback != null)
        {
            onDialogueClosedCallback.Invoke();
            onDialogueClosedCallback = null;
        }
    }
}