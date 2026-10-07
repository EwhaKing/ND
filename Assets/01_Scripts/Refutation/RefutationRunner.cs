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
    [SerializeField] private RefutationDialogueDatabase generalDialogueDb;
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

        // DB 초기화
        if (generalDialogueDb != null && currentStageData.stageDialogueCsv != null)
        {
            generalDialogueDb.Initialize(currentStageData.stageDialogueCsv);
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
                        currentStageData.testimonyIds,
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
        }
    }

    private void GoNextStep()
    {
        currentStepIndex++;
        ExecuteCurrentStep();
    }

    private void RunDialogueGroupStep(string groupId)
    {
        var rawDataList = generalDialogueDb != null ? generalDialogueDb.GetDialogueGroup(groupId) : null;

        if (rawDataList == null || rawDataList.Count == 0)
        {
            GoNextStep();
            return;
        }

        List<ChatDialogueManager.DialogueLine> lines = new();
        foreach (var item in rawDataList)
        {
            lines.Add(new ChatDialogueManager.DialogueLine
            {
                speaker = item.speaker,
                dialogue = item.dialogue
            });
        }

        if (dialogueManager != null)
        {
            dialogueManager.gameObject.SetActive(true);
            dialogueManager.ShowDialogueUI();
            dialogueManager.StartDialogue(lines.ToArray(), GoNextStep);
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