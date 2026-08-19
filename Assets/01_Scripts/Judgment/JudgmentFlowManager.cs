using System.Collections.Generic;
using UnityEngine;

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


    /// <summary>
    /// 심판 시스템 시작
    /// </summary>
    public void StartJudgmentFlow()
    {
        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("Judgment Stage가 설정되어 있지 않습니다.");
            return;
        }

        currentStageIndex = 0;

        ShowChoice();
    }


    // =========================
    // 선택
    // =========================

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


    /// <summary>
    /// 심판한다 선택
    /// </summary>
    public void ChooseJudgeNow()
    {
        ShowJudgment(false);
    }


    /// <summary>
    /// 더 파고든다 선택
    /// </summary>
    public void ChooseDigDeeper()
    {
        choiceUI.Hide();

        Debug.Log(
            $"더 파고든다 선택 - Stage {currentStageIndex + 1}"
        );

        // TODO
        // 여기서 논파 담당자 시스템 호출
        //
        // 예:
        // RefutationManager.Instance
        //     .StartRefutation(currentStageIndex);
        //
        // 논파 완료 후에는
        //
        // OnDigDeeperFinished();
        //
        // 를 호출해주면 됨.
    }


    /// <summary>
    /// 다른 팀원의 논파 시스템이 끝나면 호출
    /// </summary>
    public void OnDigDeeperFinished()
    {
        currentStageIndex++;

        // 마지막 단계까지 모두 파고든 경우
        if (currentStageIndex >= stages.Count)
        {
            ShowJudgment(true);
            return;
        }

        // 아직 남은 단계가 있는 경우
        ShowChoice();
    }


    // =========================
    // 심판
    // =========================

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
                
                // TODO
                // AND 분기 연결
                
                break;

            case JudgmentVerdict.END:
                Debug.Log("END - 생을 끝낸다.");

                // TODO
                // AND 분기 연결

                break;
        }
    }


    // =========================
    // UI
    // =========================

    private void HideAllUI()
    {
        choiceUI.Hide();
        judgmentUI.Hide();
    }
}