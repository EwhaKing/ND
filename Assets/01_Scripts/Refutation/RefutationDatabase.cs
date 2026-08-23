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
        public string evidenceId; // 증거 ID
        public string wrongMessage; // 오답 대사
    }

    [Header("논파 CSV")]
    [SerializeField] private TextAsset refutationCsv;
    
    [Header("오답 CSV")]
    [SerializeField] private TextAsset customWrongCsv;

    private readonly Dictionary<string, RefutationEntry> table = new();   
    private readonly Dictionary<string, CustomWrongEntry> wrongTable = new();

    private void Awake()
    {
        LoadCsv();
        LoadWrongCsv();
    }

    // 논파 데이터 가져오기
    public RefutationEntry GetRefutationData(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (table.TryGetValue(id, out var entry)) return entry;
        return null;
    }

    // 오답 대사 가져오기
    public string GetCustomWrongMessage(string testimonyId, string evidenceId)
    {
        string key = $"{testimonyId}_{evidenceId}"; 
        
        if (wrongTable.TryGetValue(key, out var entry))
        {
            return entry.wrongMessage;
        }
        return null;
    }

    // CSV 파일 로드 및 파싱
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

    // 오답 CSV 파일 로드 및 파싱
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
            string msg = row[2].Trim();

            if (string.IsNullOrEmpty(tId) || string.IsNullOrEmpty(eId)) continue;

            string key = $"{tId}_{eId}";
            wrongTable[key] = new CustomWrongEntry
            {
                testimonyId = tId,
                evidenceId = eId,
                wrongMessage = msg
            };
        }
    }

    // CSV 텍스트를 파싱하여 2차원 배열로 반환
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