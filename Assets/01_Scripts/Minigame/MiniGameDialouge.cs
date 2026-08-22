using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameDialogue : MonoBehaviour
{
    private enum DialoguePhase
    {
        None,
        Intro,
        Choosing,
        Result
    }

    [Header("말풍선 UI")]
    [SerializeField] private GameObject speechBubble;
    [SerializeField] private Button speechBubbleButton;
    [SerializeField] private TMP_Text dialogueText;

    [Header("선택지 UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private TMP_Text choiceText1;
    [SerializeField] private TMP_Text choiceText2;

    [Header("대사 출력")]
    [SerializeField] private float typingSpeed = 0.04f;

    private CorrectSpot currentSpot;
    private DialoguePhase currentPhase = DialoguePhase.None;

    private Coroutine typingCoroutine;
    private int currentDialogueIndex;

    private bool isTyping;

    private void Awake()
    {
        speechBubble.SetActive(false);
        choicePanel.SetActive(false);

        speechBubbleButton.onClick.AddListener(OnSpeechBubbleClicked);

        choiceButton1.onClick.AddListener(OnChoice1Clicked);

        choiceButton2.onClick.AddListener(OnChoice2Clicked);
    }

    private void OnDestroy()
    {
        speechBubbleButton.onClick.RemoveListener(OnSpeechBubbleClicked);

        choiceButton1.onClick.RemoveListener(OnChoice1Clicked);

        choiceButton2.onClick.RemoveListener(OnChoice2Clicked);
    }

    public void OpenQuestion(CorrectSpot spot)
    {
        // 다른 문제나 대화가 진행 중이라면 새 문제를 열지 않음
        if (currentPhase != DialoguePhase.None)
        {
            return;
        }

        if (spot == null || spot.IsSolved)
        {
            return;
        }

        currentSpot = spot;
        currentDialogueIndex = 0;
        currentPhase = DialoguePhase.Intro;

        choicePanel.SetActive(false);

        string[] dialogues = currentSpot.PreChoiceDialogues;

        // 선택 전 대사가 하나도 없다면 바로 선택지 표시
        if (dialogues == null || dialogues.Length == 0)
        {
            ShowChoices();
            return;
        }

        speechBubble.SetActive(true);
        StartDialogue(dialogues[currentDialogueIndex]);
    }

    private void OnSpeechBubbleClicked()
    {
        // 타이핑 중 클릭하면 현재 대사를 즉시 완성
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        switch (currentPhase)
        {
            case DialoguePhase.Intro:
                ShowNextIntroDialogue();
                break;

            case DialoguePhase.Result:
                HideAllUI();
                break;
        }
    }

    private void ShowNextIntroDialogue()
    {
        currentDialogueIndex++;

        string[] dialogues = currentSpot.PreChoiceDialogues;

        // 다음 대사가 남아 있으면 출력
        if (currentDialogueIndex < dialogues.Length)
        {
            StartDialogue(dialogues[currentDialogueIndex]);
            return;
        }

        ShowChoices();
    }

    private void ShowChoices()
    {
        StopTyping();

        currentPhase = DialoguePhase.Choosing;

        choicePanel.SetActive(true);
        
        choiceText1.text = "(1) " + currentSpot.Choice1;
        choiceText2.text = "(2) " + currentSpot.Choice2;
    }

    private void OnChoice1Clicked()
    {
        SelectChoice(1);
    }

    private void OnChoice2Clicked()
    {
        SelectChoice(2);
    }

    private void SelectChoice(int selectedChoice)
    {
        if (currentPhase != DialoguePhase.Choosing)
        {
            return;
        }

        bool isCorrect = currentSpot.CheckAnswer(selectedChoice);

        choicePanel.SetActive(false);

        currentPhase = DialoguePhase.Result;

        string resultDialogue;

        if (isCorrect)
        {
            resultDialogue = currentSpot.CorrectDialogue;
        }
        else
        {
            resultDialogue = currentSpot.WrongDialogue;
        }

        StartDialogue(resultDialogue);
    }

    private void StartDialogue(string dialogue)
    {
        StopTyping();

        speechBubble.SetActive(true);

        typingCoroutine = StartCoroutine(TypeDialogue(dialogue));
    }

    private IEnumerator TypeDialogue(string dialogue)
    {
        isTyping = true;

        dialogueText.text = dialogue;
        dialogueText.maxVisibleCharacters = 0;

        dialogueText.ForceMeshUpdate();

        int characterCount = dialogueText.textInfo.characterCount;

        for (int i = 1; i <= characterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i;

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        dialogueText.maxVisibleCharacters =int.MaxValue;

        isTyping = false;
        typingCoroutine = null;
    }

    private void CompleteTyping()
    {
        StopTyping();

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    private void HideAllUI()
    {
        StopTyping();

        speechBubble.SetActive(false);
        choicePanel.SetActive(false);

        dialogueText.text = string.Empty;

        currentSpot = null;
        currentDialogueIndex = 0;
        currentPhase = DialoguePhase.None;
    }
}