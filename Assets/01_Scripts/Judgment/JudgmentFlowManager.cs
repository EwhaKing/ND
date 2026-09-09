using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// JudgmentFlowManager
///
/// 담당:
/// - 심판 파트의 선택 흐름과 최종 판결 흐름을 관리
/// - JudgmentChoiceUI를 통해 “심판한다 / 더 파고든다” 선택을 표시할 수 있음
/// - JudgmentUI를 통해 AND / END 최종 심판 선택을 표시
/// - 최종 판결 결과를 GameProgressManager에 전달하여 다음 진행 단계로 이동
///
/// 사용 위치:
/// - JudgeScene의 심판 시스템 관리 오브젝트에 붙여 사용
/// - JudgmentChoiceUI, JudgmentUI, JudgmentStageData를 Inspector에서 연결해야 힘
///
/// 연결:
/// - JudgmentChoiceUI에서 심판/더 파고들기 선택 결과를 전달받음
/// - JudgmentUI에서 AND / END 선택 결과를 전달받음
/// - GameProgressManager.OnJudgmentFinished()를 호출하여 심판 결과를 전체 진행 흐름에 반영
/// - 추후 RefutationManager와 연결하면 더 파고들기 선택 시 논파 시스템으로 이동할 수 있음
///
/// TODO:
/// - 현재 JudgeScene 진입 시 발표용으로 바로 최종 심판만 실행
/// - 추후 심판 선택지부터 시작하는 흐름이 필요하면 StartJudgmentFlow()를 사용하도록 분기 처리 필요
/// - AND / END 결과에 따라 다른 결과 대사나 Stage2 분기를 보여주는 기능 확장 필요
/// - 여러 JudgmentStageData를 사용하는 구조와 전체 GameProgress 흐름의 관계 정리 필요
/// </summary>
public class JudgmentFlowManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private JudgmentChoiceUI choiceUI;
    [SerializeField] private JudgmentUI judgmentUI;

    [Header("Stage")]
    [SerializeField] private List<JudgmentStageData> stages;

    private int currentStageIndex = 0;
    private JudgmentFlowState currentState;

    public int CurrentStageIndex => currentStageIndex;
    public JudgmentFlowState CurrentState => currentState;

    private void Awake()
    {
        choiceUI.Initialize(this);
        judgmentUI.Initialize(this);

        HideAllUI();
    }

    private void Start()
    {
        StartFinalJudgmentOnly();
    }

    public void StartJudgmentFlow()
    {
        if (stages == null ||
            stages.Count == 0)
        {
            Debug.LogError("Judgment Stage가 설정되어 있지 않습니다.");
            return;
        }

        currentStageIndex = 0;

        ShowChoice();
    }

    public void StartFinalJudgmentOnly()
    {
        if (stages == null ||
            stages.Count == 0)
        {
            Debug.LogError("Judgment Stage가 설정되어 있지 않습니다.");
            return;
        }

        currentStageIndex = stages.Count - 1;

        ShowJudgment(true);
    }

    public void ShowChoice()
    {
        currentState = JudgmentFlowState.Choice;

        HideAllUI();

        JudgmentStageData currentStage =
            stages[currentStageIndex];

        choiceUI.Show(
            currentStageIndex,
            stages.Count,
            currentStage
        );
    }

    public void ChooseJudgeNow()
    {
        ShowJudgment(false);
    }

    public void ChooseDigDeeper()
    {
        choiceUI.Hide();

        Debug.Log(
            $"더 파고든다 선택 - Stage {currentStageIndex + 1}"
        );

        // 추후 논파 시스템과 연결할 수 있음.
        // 현재 발표용 흐름에서는 JudgeScene에 들어온 경우 바로 최종 심판을 사용.
    }

    public void OnDigDeeperFinished()
    {
        currentStageIndex++;

        if (currentStageIndex >= stages.Count)
        {
            ShowJudgment(true);
            return;
        }

        ShowChoice();
    }

    private void ShowJudgment(bool isFinal)
    {
        currentState = JudgmentFlowState.Judgment;

        HideAllUI();

        judgmentUI.Show(isFinal);
    }

    public void SelectVerdict(JudgmentVerdict verdict)
    {
        currentState = JudgmentFlowState.Finished;

        HideAllUI();

        switch (verdict)
        {
            case JudgmentVerdict.AND:
                Debug.Log("AND - 생을 이어준다.");
                break;

            case JudgmentVerdict.END:
                Debug.Log("END - 생을 끝낸다.");
                break;
        }

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnJudgmentFinished(verdict);
        }
        else
        {
            Debug.LogError("GameProgressManager가 없습니다.");
        }
    }

    private void HideAllUI()
    {
        choiceUI.Hide();
        judgmentUI.Hide();
    }
}

public enum JudgmentFlowState
{
    Choice,
    Judgment,
    Finished
}

public enum JudgmentVerdict
{
    AND,
    END
}