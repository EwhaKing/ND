using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RefutationDatabase : MonoBehaviour
{
    [Serializable]
    public class RefutationEntry
    {
        public string id; // 증언 ID
        public string character; // 캐릭터 이름
        public string dialogue; // 증언 내용
        public bool isWeakPoint; // 논파 포인트 여부
        public string correctEvidenceId; // 논파에 필요한 증거 ID
        public string expression; // 캐릭터 표정
    }

    [Serializable]
    public class CustomWrongEntry
    {
        public string testimonyId; // 증언 ID
        public string evidenceId;  // 증거 ID
        public string speaker;     // 화자 이름
        public string wrongMessage; // 오답 대사
    }

    [Header("논파 CSV")]
    [SerializeField] private TextAsset refutationCsv;
    
    [Header("오답 CSV")]
    [SerializeField] private TextAsset customWrongCsv;

    private readonly Dictionary<string, RefutationEntry> table = new();   
    private readonly Dictionary<string, List<CustomWrongEntry>> wrongTable = new(); // 💡 List로 변경

    private void Awake()
    {
        LoadCsv();
        LoadWrongCsv();
    }

    public RefutationEntry GetRefutationData(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (table.TryGetValue(id, out var entry)) return entry;
        return null;
    }

    /// <summary>
    /// 특정 증언 및 증거에 지정된 오답 대사 목록(단일 또는 다중)을 가져옵니다.
    /// </summary>
    public List<CustomWrongEntry> GetCustomWrongDialogue(string testimonyId, string evidenceId)
    {
        string key = $"{testimonyId}_{evidenceId}";
        if (wrongTable.TryGetValue(key, out var list))
        {
            return list;
        }
        return null;
    }

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

    private void LoadWrongCsv()
    {
        wrongTable.Clear();
        if (customWrongCsv == null) return;

        List<string[]> rows = ParseCsv(customWrongCsv.text);
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];
            if (row.Length < 3) continue;

            string tId = row[0].Trim();
            string eId = row[1].Trim();

            // 열 길이가 4 이상일 경우: testimonyId, evidenceId, speaker, wrongMessage
            // 열 길이가 3일 경우: testimonyId, evidenceId, wrongMessage (speaker 없음 호환)
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