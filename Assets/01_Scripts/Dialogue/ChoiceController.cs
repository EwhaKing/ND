using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceController : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("ChoicePanel 오브젝트 (비활성화 상태로 시작)")]
    [SerializeField] private GameObject choicePanel;

    [Header("Judgment Choice Panel Internal Elements")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button judgeButton;      // 심판한다 버튼 (첫번째 선택지)
    [SerializeField] private Button digDeeperButton;  // 파고든다 버튼 (두번째 선택지)

    private void Awake()
    {
        if (choicePanel == null)
        {
            choicePanel = gameObject;
        }

        // 시작할 때 패널 비활성화
        HideChoices();
    }

    /// <summary>
    /// ScenarioRunner에서 선택지 Step이 올 때 호출됩니다.
    /// </summary>
    public void ShowChoices(
        List<ChoiceData> choices,
        Action<ChoiceData> onChoiceSelected)
    {
        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("표시할 선택지 데이터가 없습니다.");
            HideChoices();
            return;
        }

        // 기존 버튼 리스너 초기화
        if (judgeButton != null) judgeButton.onClick.RemoveAllListeners();
        if (digDeeperButton != null) digDeeperButton.onClick.RemoveAllListeners();

        // 1번째 선택지 세팅 (예: 심판한다 / 법봉을 세 번 두두리기)
        if (choices.Count > 0 && judgeButton != null)
        {
            judgeButton.gameObject.SetActive(true);
            SetButtonData(judgeButton, choices[0], onChoiceSelected);
        }
        else if (judgeButton != null)
        {
            judgeButton.gameObject.SetActive(false);
        }

        // 2번째 선택지 세팅 (예: 파고든다 / 심판을 그만두기)
        if (choices.Count > 1 && digDeeperButton != null)
        {
            digDeeperButton.gameObject.SetActive(true);
            SetButtonData(digDeeperButton, choices[1], onChoiceSelected);
        }
        else if (digDeeperButton != null)
        {
            digDeeperButton.gameObject.SetActive(false);
        }

        // ChoicePanel 전체 활성화
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }
    }

    private void SetButtonData(Button button, ChoiceData choice, Action<ChoiceData> onChoiceSelected)
    {
        TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            btnText.text = choice.choiceText;
        }

        button.onClick.AddListener(() =>
        {
            Debug.Log($"선택지 클릭: {choice.choiceText}");
            HideChoices(); // 선택 후 패널 닫기
            onChoiceSelected?.Invoke(choice);
        });
    }

    /// <summary>
    /// 상황에 따라 설명문(DescriptionText)이나 단계(StageText) 텍스트를 외부에서 변경할 필요가 있을 때 사용합니다.
    /// </summary>
    public void SetJudgmentInfo(string stage, string description)
    {
        if (stageText != null) stageText.text = stage;
        if (descriptionText != null) descriptionText.text = description;
    }

    /// <summary>
    /// ChoicePanel을 비활성화합니다.
    /// </summary>
    public void HideChoices()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }
    }
}