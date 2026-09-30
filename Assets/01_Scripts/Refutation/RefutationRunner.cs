using System;
using System.Collections.Generic;
using UnityEngine;

public class RefutationRunner : MonoBehaviour
{
    public enum StepType
    {
        DIALOGUE,
        REFUTATION,
        END
    }

    public class SequenceStep
    {
        public StepType stepType;
        public string targetId;
    }

    [Header("시퀀스 CSV")]
    [SerializeField] private TextAsset sequenceCsv;

    [Header("연결 매니저")]
    [SerializeField] private ChatDialogueManager chatManager; // 💡 반드시 일반 대화용(DialougeManager_1) 연결
    [SerializeField] private RefutationManager refutationManager;
    [SerializeField] private RefutationDialogueDatabase generalDialogueDb;

    [Header("UI 그룹 제어")]
    [SerializeField] private GameObject generalDialogueGroup; // 💡 새로 추가: 일반 대화 부모 오브젝트
    [SerializeField] private GameObject refutationUIGroup;    // 💡 새로 추가: 논파 UI 부모 오브젝트

    private readonly List<SequenceStep> sequenceList = new();
    private int currentStepIndex = 0;

    private void Start()
    {
        LoadSequenceCsv();
        ExecuteCurrentStep();
    }

    private void LoadSequenceCsv()
    {
        sequenceList.Clear();
        if (sequenceCsv == null) return;

        List<string[]> rows = CsvParser.Parse(sequenceCsv.text);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 3) continue;

            string typeStr = row[1].Trim().ToUpper();
            string targetId = row[2].Trim();

            if (Enum.TryParse<StepType>(typeStr, out var stepType))
            {
                sequenceList.Add(new SequenceStep
                {
                    stepType = stepType,
                    targetId = targetId
                });
            }
        }
    }

    private void ExecuteCurrentStep()
    {
        if (currentStepIndex >= sequenceList.Count)
        {
            FinishScene(true);
            return;
        }

        SequenceStep step = sequenceList[currentStepIndex];

        switch (step.stepType)
        {
            case StepType.DIALOGUE:
                // 💡 1. 일반 대화 시작: 일반 UI 그룹을 켜고, 논파 UI 그룹을 확실히 끕니다.
                if (generalDialogueGroup != null) generalDialogueGroup.SetActive(true);
                if (refutationUIGroup != null) refutationUIGroup.SetActive(false);

                if (refutationManager != null) 
                {
                    refutationManager.gameObject.SetActive(false);
                }
                RunDialogueStep(step.targetId);
                break;

            case StepType.REFUTATION:
                // 💡 2. 논파 시작: 일반 UI 그룹을 끄고, 논파 UI 그룹을 켭니다.
                if (generalDialogueGroup != null) generalDialogueGroup.SetActive(false);
                if (refutationUIGroup != null) refutationUIGroup.SetActive(true);

                if (refutationManager != null) 
                {
                    refutationManager.gameObject.SetActive(true);
                }
                RunRefutationStep(step.targetId);
                break;

            case StepType.END:
                FinishScene(true);
                break;
        }
    }

    private void GoNextStep()
    {
        currentStepIndex++;
        ExecuteCurrentStep();
    }

    private void RunDialogueStep(string groupId)
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

        if (chatManager != null)
        {
            chatManager.ShowDialogueUI();
            chatManager.StartDialogue(lines.ToArray(), GoNextStep);
        }
        else
        {
            GoNextStep();
        }
    }

    private void RunRefutationStep(string testimonyIdsCsv)
    {
        List<string> testimonyList = new List<string>(
            testimonyIdsCsv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
        );

        for (int i = 0; i < testimonyList.Count; i++)
        {
            testimonyList[i] = testimonyList[i].Trim();
        }

        if (refutationManager != null)
        {
            refutationManager.StartRefutation(
                testimonyList,
                onSuccess: GoNextStep,
                onFail: () => FinishScene(false)
            );
        }
        else
        {
            GoNextStep();
        }
    }

    private void FinishScene(bool isSuccess)
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnRefutationFinished(isSuccess);
        }
        else
        {
            Debug.Log($"[RefutationRunner] 씬 완료 판정: {isSuccess}");
        }
    }
}