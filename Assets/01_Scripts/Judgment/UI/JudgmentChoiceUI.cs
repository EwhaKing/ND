using System;
using System.Collections.Generic;
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




    public void ShowChoices(List<ChoiceData> choices, Action<ChoiceData> onChoiceSelected)
    {
        gameObject.SetActive(true);

        // 기존 클릭 이벤트 제거
        judgeButton.onClick.RemoveAllListeners();
        digDeeperButton.onClick.RemoveAllListeners();

        // 1번째 선택지 ("심판한다" -> JudgeScene)
        if (choices != null && choices.Count > 0)
        {
            judgeButton.gameObject.SetActive(true);
            SetButtonText(judgeButton, choices[0].choiceText);

            judgeButton.onClick.AddListener(() =>
            {
                Hide();
                onChoiceSelected?.Invoke(choices[0]); // ScenarioRunner로 전달되어 LoadScene 동작!
            });
        }
        else
        {
            judgeButton.gameObject.SetActive(false);
        }

        // 2번째 선택지 ("파고든다" -> RefutationScene)
        if (choices != null && choices.Count > 1)
        {
            digDeeperButton.gameObject.SetActive(true);
            SetButtonText(digDeeperButton, choices[1].choiceText);

            digDeeperButton.onClick.AddListener(() =>
            {
                Hide();
                onChoiceSelected?.Invoke(choices[1]); // ScenarioRunner로 전달되어 LoadScene 동작!
            });
        }
        else
        {
            digDeeperButton.gameObject.SetActive(false);
        }
    }

    private void SetButtonText(Button btn, string text)
    {
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }
    
}