using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class PointClickDialogueManager : MonoBehaviour
{
    public static PointClickDialogueManager Instance;

    [Header("UI 컴포넌트 연결")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    private Queue<string> sentences = new Queue<string>(); // 대사 저장용 큐
    private bool pendingInventoryRefresh = false; // 인벤토리 UI 갱신 여부 확인용 플래그
    public Action onDialogueClosedCallback = null; // 대화창이 닫힐 때 호출될 콜백

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // 여러 줄 대사 출력용 함수
    public void ShowTexts(string[] textsToShow, bool shouldRefreshInventory = false)
    {
        if (dialoguePanel == null || dialogueText == null) return;

        sentences.Clear();

        foreach (string text in textsToShow)
        {
            if (!string.IsNullOrEmpty(text))
            {
                sentences.Enqueue(text);
            }
        }

        pendingInventoryRefresh = shouldRefreshInventory;
        
        DisplayNextSentence();
    }

    // 한 줄 대사 출력용 함수
    public void ShowText(string textToShow, bool shouldRefreshInventory = false)
    {
        ShowTexts(new string[] { textToShow }, shouldRefreshInventory);
    }

    // 다음 대사 출력용 함수
    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            FinishDialogue();
            return;
        }

        string currentSentence = sentences.Dequeue();
        dialogueText.text = currentSentence;
        dialoguePanel.SetActive(true);
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

        if (pendingInventoryRefresh)
        {
            InventoryManager.Instance.UpdateInventoryUI();

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