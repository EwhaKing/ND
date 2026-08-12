using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ChoiceController
///
/// 담당:
/// - 선택지 버튼을 동적으로 생성하고 관리
/// - ChoiceData 목록을 받아 선택지 UI를 화면에 출력
/// - 플레이어가 선택지를 클릭하면 선택된 ChoiceData를 콜백으로 전달
///
/// 사용 위치:
/// - 선택지 버튼들이 배치될 UI 부모 오브젝트에 부착
/// - 대화 중 선택지가 필요한 ScenarioStepType.Choice 단계에서 호출
///
/// 연결:
/// - ScenarioRunner에서 Choice 타입 Step을 실행할 때 사용
/// - ScenarioData의 ChoiceData 정보를 받아 버튼 텍스트와 선택 결과를 처리
/// - 선택 결과에 따라 다음 Step 진행, 반응 대사 출력, 씬 이동 등의 처리를 연결 가능
///
/// TODO:
/// - 선택지 등장/퇴장 애니메이션 추가
/// - 선택지 선택 시 사운드/하이라이트 효과 추가
/// - 선택 결과가 GameFlagManager나 ChapterManager에 영향을 주도록 확장
/// 
/// </summary>
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
            Debug.LogWarning("선택지 표시.");
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
            Debug.Log($"선택한 텍스트: {choice.choiceText}");

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