using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JudgmentChoiceUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Button")]
    [SerializeField] private Button judgeButton;
    [SerializeField] private Button digDeeperButton;

    private JudgmentFlowManager manager;


    public void Initialize(JudgmentFlowManager flowManager)
    {
        manager = flowManager;

        judgeButton.onClick.AddListener(OnClickJudge);
        digDeeperButton.onClick.AddListener(OnClickDigDeeper);
    }


    public void Show(
        int stageIndex,
        int totalStage,
        JudgmentStageData stageData)
    {
        gameObject.SetActive(true);

        stageText.text =
            $"{stageData.stageName}  {stageIndex + 1} / {totalStage}";

        descriptionText.text =
            stageData.choiceDescription;
    }


    public void Hide()
    {
        gameObject.SetActive(false);
    }


    private void OnClickJudge()
    {
        manager.ChooseJudgeNow();
    }


    private void OnClickDigDeeper()
    {
        manager.ChooseDigDeeper();
    }
}