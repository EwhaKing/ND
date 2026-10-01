using System;
using System.Collections.Generic;
using UnityEngine;

public class RefutationDialogueDatabase : MonoBehaviour
{
    [Serializable]
    public class DialogueData
    {
        public string speaker;
        public string dialogue;
        public string expression;
    }

    [Header("일반 대화 CSV")]
    [SerializeField] private TextAsset dialogueCsv;

    private readonly Dictionary<string, List<DialogueData>> dialogueTable = new();

    private void Awake()
    {
        LoadCsv();
    }

    public List<DialogueData> GetDialogueGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId)) return null;
        return dialogueTable.TryGetValue(groupId, out var list) ? list : null;
    }

    private void LoadCsv()
    {
        dialogueTable.Clear();
        if (dialogueCsv == null) return;

        List<string[]> rows = CsvParser.Parse(dialogueCsv.text);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 3) continue;

            string groupId = row[0].Trim();
            string speaker = row[1].Trim();
            string dialogue = row[2].Trim();
            string expression = row.Length >= 4 ? row[3].Trim() : string.Empty;

            if (string.IsNullOrEmpty(groupId)) continue;

            DialogueData data = new DialogueData
            {
                speaker = speaker,
                dialogue = dialogue,
                expression = expression
            };

            if (!dialogueTable.ContainsKey(groupId))
            {
                dialogueTable.Add(groupId, new List<DialogueData>());
            }

            dialogueTable[groupId].Add(data);
        }
    }
}