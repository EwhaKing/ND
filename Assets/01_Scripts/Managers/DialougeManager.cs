using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Serializable]
    public class DialogueLine
    {
        public string speaker;

        [TextArea(2, 6)]
        public string dialogue;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject nextIndicator;
    [SerializeField] private Button clickArea;

    [Header("Typing")]
    [SerializeField, Min(0.001f)]
    private float typingSpeed = 0.04f;

    [SerializeField, Min(0f)]
    private float nextInputDelay = 0.3f;

    private DialogueLine[] currentLines;

    private int currentLineIndex;

    private bool isTyping;
    private bool canProceed;

    private string currentFullText;

    private Coroutine typingCoroutine;
    private Coroutine inputDelayCoroutine;

    private Action onDialogueFinished;

    private void Awake()
    {
        if (clickArea != null)
        {
            clickArea.onClick.AddListener(OnDialogueClicked);
        }

        ResetDialogueUI();
    }

    private void OnDestroy()
    {
        if (clickArea != null)
        {
            clickArea.onClick.RemoveListener(OnDialogueClicked);
        }
    }

    public void StartDialogue(
        DialogueLine[] lines,
        Action finishedCallback = null)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("출력할 대사가 없습니다.");
            finishedCallback?.Invoke();
            return;
        }

        StopDialogueCoroutines();

        currentLines = lines;
        currentLineIndex = 0;
        onDialogueFinished = finishedCallback;

        gameObject.SetActive(true);

        ShowCurrentLine();
    }

    public void ShowSingleLine(
        string speaker,
        string dialogue,
        Action finishedCallback = null)
    {
        DialogueLine[] singleLine =
        {
            new DialogueLine
            {
                speaker = speaker,
                dialogue = dialogue
            }
        };

        StartDialogue(singleLine, finishedCallback);
    }

    public void OnDialogueClicked()
    {
        // 타이핑 중 클릭하면 현재 문장을 즉시 완성한다.
        if (isTyping)
        {
            CompleteTypingImmediately();
            return;
        }

        // 문장 출력 직후 연속 클릭 방지
        if (!canProceed)
        {
            return;
        }

        ShowNextLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null ||
            currentLineIndex < 0 ||
            currentLineIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        DialogueLine currentLine = currentLines[currentLineIndex];

        // 화자가 없으면 빈 문자열을 넣는다.
        speakerText.text = currentLine.speaker ?? string.Empty;

        currentFullText = currentLine.dialogue ?? string.Empty;

        dialogueText.text = string.Empty;
        nextIndicator.SetActive(false);

        isTyping = true;
        canProceed = false;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLineRoutine());
    }

    private IEnumerator TypeLineRoutine()
    {
        foreach (char character in currentFullText)
        {
            dialogueText.text += character;

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        typingCoroutine = null;
        isTyping = false;

        StartNextInputDelay();
    }

    private void CompleteTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueText.text = currentFullText;

        isTyping = false;

        StartNextInputDelay();
    }

    private void StartNextInputDelay()
    {
        if (inputDelayCoroutine != null)
        {
            StopCoroutine(inputDelayCoroutine);
        }

        inputDelayCoroutine =
            StartCoroutine(EnableNextInputRoutine());
    }

    private IEnumerator EnableNextInputRoutine()
    {
        yield return new WaitForSecondsRealtime(nextInputDelay);

        canProceed = true;
        nextIndicator.SetActive(true);

        inputDelayCoroutine = null;
    }

    private void ShowNextLine()
    {
        canProceed = false;
        nextIndicator.SetActive(false);

        currentLineIndex++;

        if (currentLineIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void FinishDialogue()
    {
        StopDialogueCoroutines();

        speakerText.text = string.Empty;
        dialogueText.text = string.Empty;
        nextIndicator.SetActive(false);

        Action finishedCallback = onDialogueFinished;

        currentLines = null;
        currentLineIndex = 0;
        currentFullText = string.Empty;
        onDialogueFinished = null;

        gameObject.SetActive(false);

        finishedCallback?.Invoke();
    }

    private void StopDialogueCoroutines()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (inputDelayCoroutine != null)
        {
            StopCoroutine(inputDelayCoroutine);
            inputDelayCoroutine = null;
        }

        isTyping = false;
        canProceed = false;
    }

    private void ResetDialogueUI()
    {
        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (nextIndicator != null)
        {
            nextIndicator.SetActive(false);
        }
    }
}