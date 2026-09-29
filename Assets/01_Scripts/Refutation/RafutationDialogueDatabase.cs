using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RafutationDialogueDatabase : MonoBehaviour
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
        if (dialogueTable.TryGetValue(groupId, out var list)) return list;
        return null;
    }

    private void LoadCsv()
    {
        dialogueTable.Clear();
        if (dialogueCsv == null) return;

        List<string[]> rows = ParseCsv(dialogueCsv.text);
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