using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StandingController
///
/// 담당:
/// - 비주얼 노벨 대화 씬에서 캐릭터 스탠딩 이미지를 표시하고 관리
/// - ScenarioData의 StandStep 배열을 받아 여러 캐릭터의 스프라이트를 화면에 배치합니다.
/// - 현재 말하는 화자를 기준으로 스탠딩 색상을 조절하여 발화자를 강조
///
/// 사용 위치:
/// - 대화 씬 또는 GameScene의 스탠딩 이미지들을 관리하는 UI 오브젝트에 부착
/// - 캐릭터 스탠딩 Image 배열을 Inspector에서 연결해 사용
///
/// 연결:
/// - ScenarioRunner의 StandingShow / StandingHide Step에서 호출
/// - ScenarioRunner의 Dialogue Step 실행 중 speaker 값을 받아 현재 발화자를 강조
/// - ScenarioData의 StandStep, ReactionStep 데이터와 연결
///
/// TODO:
/// - 스탠딩 위치와 크기를 코드 고정값이 아니라 데이터 또는 Inspector 설정값으로 분리
/// - 캐릭터가 3명 이상 등장하는 경우의 배치 방식 확장
/// - Narration 타입일 때 스탠딩을 흐리게 할지 숨길지 정책 정리 필요
/// - CG 출력 중에는 스탠딩을 자동으로 숨기는 기능 추가 검토
/// - 스탠딩 등장/퇴장 Fade 연출 추가
/// 
/// </summary>
public class StandingController : MonoBehaviour
{
    [SerializeField] private Image[] images;
    
    [Header("Expressions")]
    [SerializeField]
    private List<ExpressionData> expressions = new List<ExpressionData>();
    private readonly List<StandStep> currentStands = new List<StandStep>();

    public void SetSprite(StandStep[] stands)
    {
        currentStands.Clear();

        for (int i = 0; i < images.Length; i++)
        {
            bool hasStand = i < stands.Length && !string.IsNullOrWhiteSpace(stands[i].standName);

            images[i].gameObject.SetActive(hasStand);

            if (!hasStand)
            {
                continue;
            }

            currentStands.Add(stands[i]);

            Sprite defaultSprite =
                FindDefaultSprite(stands[i].standName);

            images[i].sprite = defaultSprite;
            images[i].rectTransform.sizeDelta = new Vector2(500f, 500f);
            images[i].color = Color.white;

            if (stands.Length > 1)
            {
                images[i].rectTransform.localPosition =
                    i == 0 ? new Vector2(-450f, -50f) : new Vector2(450f, -50f);
            }
            else
            {
                images[i].rectTransform.localPosition = Vector2.zero;
            }
        }
    }
    private Sprite FindDefaultSprite(string standName)
    {
        foreach (ExpressionData expression in expressions)
        {
            if (expression == null)
            {
                continue;
            }

            if (expression.standName == standName &&
                expression.code.EndsWith("00"))
            {
                return expression.sprite;
            }
        }

        Debug.LogWarning(
            $"기본 표정을 찾을 수 없습니다: {standName}"
        );

        return null;
    }

    /*public void SetColor(string speaker)
    {
        int count = Mathf.Min(
            currentStands.Count,
            images.Length
        );

        if (count == 0)
        {
            return;
        }

        bool isNarration =
            string.IsNullOrWhiteSpace(speaker);

        for (int i = 0; i < count; i++)
        {
            if (images[i] == null ||
                !images[i].gameObject.activeSelf)
            {
                continue;
            }

            if (isNarration)
            {
                images[i].color = Color.gray;
                continue;
            }

            bool isCurrentSpeaker =
                currentStands[i].standName == speaker;

            images[i].color =
                isCurrentSpeaker
                    ? Color.white
                    : Color.gray;
        }
    }*/

    public void Hide()
    {
        foreach (Image image in images)
        {
            image.gameObject.SetActive(false);
            image.color = Color.white;
        }

        currentStands.Clear();
    }
    public void ChangeSprite(string standName, Sprite newSprite)
    {
        if (string.IsNullOrWhiteSpace(standName))
        {
            Debug.LogError(
                "변경할 캐릭터 이름이 비어 있습니다."
            );

            return;
        }

        if (newSprite == null)
        {
            Debug.LogError(
                $"'{standName}'에게 적용할 Sprite가 없습니다."
            );

            return;
        }

        for (int i = 0; i < currentStands.Count; i++)
        {
            if (currentStands[i].standName != standName)
            {
                continue;
            }

            images[i].sprite = newSprite;

            Debug.Log(
                $"스탠딩 표정 변경: {standName} -> {newSprite.name}"
            );

            return;
        }

        Debug.LogWarning(
            $"현재 화면에 표시 중인 스탠딩을 찾지 못했습니다: {standName}"
        );
    }
    public void SetExpression(string expressionCode)
    {
        if (string.IsNullOrWhiteSpace(expressionCode))
        {
            return;
        }

        ExpressionData expression =  FindExpression(expressionCode);

        if (expression == null)
        {
            Debug.LogWarning(
                $"등록되지 않은 표정 코드입니다: {expressionCode}"
            );

            return;
        }

        ChangeSprite(
            expression.standName,
            expression.sprite
        );
    }
    private ExpressionData FindExpression(string expressionCode)
    {
        foreach (ExpressionData expression in expressions)
        {
            if (expression == null)
            {
                continue;
            }

            if (expression.code == expressionCode)
            {
                return expression;
            }
        }

        return null;
    }
}