using System;
using System.Collections.Generic;
using UnityEngine;

public class RefutationRunner : MonoBehaviour
{
    [Serializable]
    public struct StageMapping
    {
        [Tooltip("GameProgressManager의 CurrentStep 값")]
        public GameFlowStep step;
        
        [Tooltip("해당 Step에서 실행할 논파 StageData")]
        public RefutationData stageData;
    }

    [Header("기존 매니저 연결")]
    [SerializeField] private ChatDialogueManager dialogueManager;
    [SerializeField] private RefutationDatabase refutationDatabase;
    [SerializeField] private RefutationManager refutationManager;
    [SerializeField] private StandingController standingController;
    [SerializeField] private Fade fadeController;
    [SerializeField] private CGController cgController;

    [Header("UI 그룹 제어")]
    [SerializeField] private GameObject generalDialogueGroup;
    [SerializeField] private GameObject refutationUIGroup;

    [Header("논파 데이터 매핑")]
    [SerializeField] private List<StageMapping> stageMappings;

    private RefutationData currentStageData;
    private int currentStepIndex = 0;

    private void Start()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("[RefutationRunner] GameProgressManager가 존재하지 않습니다.");
            return;
        }

        currentStageData = GetStageDataByCurrentStep();

        if (currentStageData == null)
        {
            Debug.LogError($"[RefutationRunner] 현재 Step({GameProgressManager.Instance.CurrentStep})에 매핑된 RefutationData가 없습니다.");
            return;
        }

        RunStage(currentStageData);
    }

    private RefutationData GetStageDataByCurrentStep()
    {
        GameFlowStep currentStep = GameProgressManager.Instance.CurrentStep;

        foreach (var map in stageMappings)
        {
            if (map.step == currentStep)
            {
                return map.stageData;
            }
        }

        return null;
    }

    public void RunStage(RefutationData data)
    {
        currentStageData = data;
        if (currentStageData == null) return;

        if (refutationDatabase != null && currentStageData.refutationCsv != null)
        {
            refutationDatabase.Initialize(currentStageData.refutationCsv, currentStageData.customWrongCsv);
        }

        if (refutationManager != null)
        {
            refutationManager.InitializeDatabase(currentStageData);
        }

        currentStepIndex = 0;
        ExecuteCurrentStep();
    }

    private void ExecuteCurrentStep()
    {
        if (currentStepIndex >= currentStageData.steps.Count)
        {
            FinishStage(true);
            return;
        }

        RefutationStep step = currentStageData.steps[currentStepIndex];

        switch (step.stepType)
        {
            case RefutationStepType.Fade:
                if (fadeController != null)
                    fadeController.FadeIn(step.fadeDuration, 0f, GoNextStep);
                else
                    GoNextStep();
                break;

            case RefutationStepType.StandingShow:
                if (standingController != null && step.stands != null)
                    standingController.SetSprite(step.stands);
                GoNextStep();
                break;

            case RefutationStepType.DialogueGroup:
                if (generalDialogueGroup != null) generalDialogueGroup.SetActive(true);
                if (refutationUIGroup != null) refutationUIGroup.SetActive(false);
                RunDialogueGroupStep(step.dialogueGroupId);
                break;

            case RefutationStepType.RefutationGame:
                if (generalDialogueGroup != null) generalDialogueGroup.SetActive(false);
                if (refutationUIGroup != null) refutationUIGroup.SetActive(true);

                if (refutationManager != null)
                {
                    refutationManager.StartRefutation(
                        step.testimonyIds,
                        onSuccess: GoNextStep,
                        onFail: () => FinishStage(false)
                    );
                }
                else
                {
                    GoNextStep();
                }
                break;

            case RefutationStepType.CGShow:
                if (cgController != null && step.cgSprite != null)
                    cgController.Show(step.cgSprite, GoNextStep);
                else
                    GoNextStep();
                break;

            case RefutationStepType.MiniGame:
                Debug.LogWarning("[RefutationRunner] 미니게임 단계 호출");
                GoNextStep();
                break;
        }
    }

    private void GoNextStep()
    {
        currentStepIndex++;
        ExecuteCurrentStep();
    }

    private void RunDialogueGroupStep(string groupId)
    {
        var rawDataList = refutationDatabase != null ? refutationDatabase.GetDialogueGroup(groupId) : null;

        if (rawDataList == null || rawDataList.Count == 0)
        {
            GoNextStep();
            return;
        }

        if (dialogueManager != null)
        {
            dialogueManager.gameObject.SetActive(true);
            dialogueManager.ShowDialogueUI();
            
            // 대사를 한 줄씩 순차적으로 재생하며 스탠딩 강조를 제어합니다.
            PlayDialogueLine(rawDataList, 0);
        }
        else
        {
            GoNextStep();
        }
    }

    // 대사를 한 줄씩 처리하는 재귀/순차 메서드
    private void PlayDialogueLine(IList<RefutationDatabase.RefutationEntry> rawDataList, int index)
    {
        if (index >= rawDataList.Count)
        {
            // 모든 대사가 끝나면 다음 스텝으로 진행
            GoNextStep();
            return;
        }

        var item = rawDataList[index];

        // 1. 현재 대사를 치는 캐릭터의 스탠딩을 밝게, 나머지를 회색조로 처리
        if (standingController != null)
        {
            string targetStand = !string.IsNullOrWhiteSpace(item.standName) ? item.standName : item.character;
            standingController.SetColor(targetStand);
            
            // 표정이 있다면 표정까지 함께 동적 제어
            if (!string.IsNullOrWhiteSpace(item.expression))
            {
                standingController.SetExpression(item.expression);
            }
        }

        // 2. 한 줄의 대사를 출력하고, 클릭해서 다음 줄로 넘어갈 때 콜백으로 다음 인덱스 호출
        if (dialogueManager != null)
        {
            dialogueManager.ShowSingleLine(item.character, item.dialogue, () => {
                PlayDialogueLine(rawDataList, index + 1);
            });
        }
        else
        {
            GoNextStep();
        }
    }

    private void FinishStage(bool isSuccess)
    {
        Debug.Log($"[RefutationRunner] 스테이지 완료 콜백 호출 (성공 여부: {isSuccess})");

        GameProgressManager.Instance?.OnRefutationFinished(isSuccess);
    }
}