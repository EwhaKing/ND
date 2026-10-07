using System;
using System.Collections.Generic;
using UnityEngine;

public class RefutationDatabase : MonoBehaviour
{
    [Serializable]
    public class RefutationEntry
    {
        public string id;               // 증언 ID
        public string character;        // 캐릭터 이름
        public string dialogue;         // 대화 내용
        public bool isWeakPoint;        // 약점 여부
        public string correctEvidenceId; // 올바른 증거 ID
        public string expression;       // 표정
    }

    [Serializable]
    public class CustomWrongEntry
    {
        public string testimonyId;      // 증언 ID
        public string evidenceId;       // 증거 ID
        public string speaker;          // 발화자
        public string wrongMessage;     // 오답 메시지
    }

    private readonly Dictionary<string, RefutationEntry> table = new();   
    private readonly Dictionary<string, List<CustomWrongEntry>> wrongTable = new();

    public void Initialize(TextAsset refutationCsv, TextAsset customWrongCsv)
    {
        table.Clear();
        wrongTable.Clear();

        if (refutationCsv != null) LoadCsv(refutationCsv.text);
        if (customWrongCsv != null) LoadWrongCsv(customWrongCsv.text);
    }

    public RefutationEntry GetRefutationData(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return table.TryGetValue(id, out var entry) ? entry : null;
    }

    public List<CustomWrongEntry> GetCustomWrongDialogue(string testimonyId, string evidenceId)
    {
        string key = $"{testimonyId}_{evidenceId}";
        return wrongTable.TryGetValue(key, out var list) ? list : null;
    }

    private void LoadCsv(string csvText)
    {
        List<string[]> rows = CsvParser.Parse(csvText);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 3) continue;

            string id = row[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            table[id] = new RefutationEntry
            {
                id = id,
                character = row[1].Trim(),
                dialogue = row[2].Trim(),
                expression = row.Length >= 4 ? row[3].Trim() : string.Empty,
                isWeakPoint = row.Length >= 5 && row[4].Trim().ToUpper() == "TRUE",
                correctEvidenceId = row.Length >= 6 ? row[5].Trim() : string.Empty
            };
        }
    }

    private void LoadWrongCsv(string csvText)
    {
        List<string[]> rows = CsvParser.Parse(csvText);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 3) continue;

            string tId = row[0].Trim();
            string eId = row[1].Trim();
            string speaker = row.Length >= 4 ? row[2].Trim() : string.Empty;
            string msg = row.Length >= 4 ? row[3].Trim() : row[2].Trim();

            if (string.IsNullOrEmpty(tId) || string.IsNullOrEmpty(eId)) continue;

            string key = $"{tId}_{eId}";

            if (!wrongTable.ContainsKey(key))
            {
                wrongTable.Add(key, new List<CustomWrongEntry>());
            }

            wrongTable[key].Add(new CustomWrongEntry
            {
                testimonyId = tId,
                evidenceId = eId,
                speaker = speaker,
                wrongMessage = msg
            });
        }
    }
}