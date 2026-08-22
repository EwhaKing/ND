using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CorrectSpot : MonoBehaviour
{
    [Header("대화 UI")]
    [SerializeField] private MiniGameDialogue miniGameDialogue;

    [Header("선택지 출력 전 대사")]
    [TextArea(2, 5)]
    [SerializeField] private string[] preChoiceDialogues;

    [Header("선택지")]
    [SerializeField] private string choice1;
    [SerializeField] private string choice2;

    [Header("정답 번호")]
    [SerializeField] private int correctChoice = 1;

    [Header("선택 결과 대사")]
    [TextArea(2, 5)]
    [SerializeField] private string correctDialogue;

    [TextArea(2, 5)]
    [SerializeField] private string wrongDialogue;

    private Button button;
    private bool isSolved;

    public string[] PreChoiceDialogues => preChoiceDialogues;

    public string Choice1 => choice1;

    public string Choice2 => choice2;

    public string CorrectDialogue => correctDialogue;

    public string WrongDialogue => wrongDialogue;

    public bool IsSolved => isSolved;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(OnSpotClicked);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnSpotClicked);
    }

    private void OnSpotClicked()
    {
        if (isSolved)
        {
            return;
        }

        miniGameDialogue.OpenQuestion(this);
    }

    public bool CheckAnswer(int selectedChoice)
    {
        bool isCorrect = selectedChoice == correctChoice;

        if (isCorrect)
        {
            isSolved = true;
            button.interactable = false;
        }

        return isCorrect;
    }
}