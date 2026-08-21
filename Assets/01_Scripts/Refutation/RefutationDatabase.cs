using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RefutationDatabase : MonoBehaviour
{
    [Serializable]
    public class RefutationEntry
    {
        public string id;
        public string character;
        public string dialogue;
        public bool isWeakPoint;
        public string correctEvidenceId;
        public string expression;
    }

    [Header("Refutation CSV")]
    [SerializeField] private TextAsset refutationCsv;

    private readonly Dictionary<string, RefutationEntry> table = new();

    private void Awake()
    {
        LoadCsv();
    }

    // 논파 데이터 가져오기
    public RefutationEntry GetRefutationData(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (table.TryGetValue(id, out var entry)) return entry;
        return null;
    }

    // CSV 로드 및 파싱
    private void LoadCsv()
    {
        table.Clear();
        if (refutationCsv == null) return;

        List<string[]> rows = ParseCsv(refutationCsv.text);
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

    // CSV 파싱 메서드
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