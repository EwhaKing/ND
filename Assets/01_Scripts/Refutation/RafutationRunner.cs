using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RafutationRunner : MonoBehaviour
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
    [SerializeField] private ChatDialogueManager chatManager;
    [SerializeField] private RefutationManager refutationManager;
    [SerializeField] private RafutationDialogueDatabase generalDialogueDb;

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

        List<string[]> rows = ParseCsv(sequenceCsv.text);
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
                RunDialogueStep(step.targetId);
                break;

            case StepType.REFUTATION:
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
            Debug.Log($"[RafutationRunner] 씬 완료 판정: {isSuccess}");
        }
    }

    private List<string[]> ParseCsv(string csvText)
    {
        List<string[]> rows = new();
        List<string> currentRow = new();
        StringBuilder currentValue = new();
        bool insideQuotes = false;

        csvText = csvText.TrimStart('\uFEFF');
        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];
            if (c == '"') { insideQuotes = !insideQuotes; continue; }
            if (c == ',' && !insideQuotes) { currentRow.Add(currentValue.ToString()); currentValue.Clear(); continue; }
            if ((c == '\n' || c == '\r') && !insideQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n') continue;
                currentRow.Add(currentValue.ToString()); currentValue.Clear();
                if (currentRow.Count > 0) rows.Add(currentRow.ToArray());
                currentRow.Clear();
                continue;
            }
            currentValue.Append(c);
        }
        currentRow.Add(currentValue.ToString());
        if (currentRow.Count > 0) rows.Add(currentRow.ToArray());
        return rows;
    }
}