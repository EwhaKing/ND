using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceController : MonoBehaviour
{
    [SerializeField] private Button choiceButtonPrefab;

    private readonly List<Button> createdButtons = new();

    public void ShowChoices(
        List<ChoiceData> choices,
        Action<ChoiceData> onChoiceSelected)
    {
        ClearButtons();

            if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("표시할 선택지가 없습니다.");
            return;
        }

        foreach (ChoiceData choice in choices)
        {
            CreateChoiceButton(choice, onChoiceSelected);
        }
    }

    private void CreateChoiceButton(
    ChoiceData choice,
    Action<ChoiceData> onChoiceSelected)
    {
        Button button = Instantiate(
            choiceButtonPrefab,
            transform
        );

        TMP_Text buttonText =
            button.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = choice.choiceText;
        }

        button.onClick.AddListener(() =>
        {
            Debug.Log($"선택지 선택: {choice.choiceText}");

            ClearButtons();
            onChoiceSelected?.Invoke(choice);
        });

        createdButtons.Add(button);
    }
    public void ClearButtons()
    {
        foreach (Button button in createdButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        createdButtons.Clear();
    }
}