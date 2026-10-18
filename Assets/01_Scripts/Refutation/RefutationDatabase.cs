using System;
using System.Collections.Generic;
using UnityEngine;

public class RefutationDatabase : MonoBehaviour
{
    [Serializable]
    public class RefutationEntry
    {
        public string id;                // ID (증언 ID 또는 대화 그룹 ID)
        public string character;         // 대화창에 표시할 캐릭터 이름
        public string dialogue;          // 대화 내용
        public string expression;        // 표정 코드
        public string standName;         // 스탠딩/화자 강조용 ID
        public bool isWeakPoint;         // 약점 여부
        public string correctEvidenceId; // 올바른 증거 ID
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
    private readonly Dictionary<string, List<RefutationEntry>> dialogueTable = new(); // 대화 그룹용 딕셔너리
    private readonly Dictionary<string, List<CustomWrongEntry>> wrongTable = new();

    public void Initialize(TextAsset refutationCsv, TextAsset customWrongCsv)
    {
        table.Clear();
        dialogueTable.Clear();
        wrongTable.Clear();

        if (refutationCsv != null) LoadCsv(refutationCsv.text);
        if (customWrongCsv != null) LoadWrongCsv(customWrongCsv.text);
    }

    public RefutationEntry GetRefutationData(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return table.TryGetValue(id, out var entry) ? entry : null;
    }

    public List<RefutationEntry> GetDialogueGroup(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId)) return null;
        return dialogueTable.TryGetValue(groupId, out var list) ? list : null;
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

            string character = row[1].Trim();
            string dialogue = row.Length >= 3 ? row[2].Trim() : string.Empty;
            string expression = row.Length >= 4 ? row[3].Trim() : string.Empty;
            
            // standName이 비어있다면 character 이름을 기본값으로 사용
            string standName = row.Length >= 5 && !string.IsNullOrWhiteSpace(row[4]) 
                ? row[4].Trim() 
                : character;

            bool isWeakPoint = row.Length >= 6 && row[5].Trim().ToUpper() == "TRUE";
            string correctEvidenceId = row.Length >= 7 ? row[6].Trim() : string.Empty;

            RefutationEntry entry = new RefutationEntry
            {
                id = id,
                character = character,
                dialogue = dialogue,
                expression = expression,
                standName = standName,
                isWeakPoint = isWeakPoint,
                correctEvidenceId = correctEvidenceId
            };

            // 1. 단건 조회용 테이블 (논파 증언 등)
            table[id] = entry;

            // 2. 그룹 대화용 테이블 (동일한 ID를 가진 여러 줄을 리스트로 누적)
            if (!dialogueTable.ContainsKey(id))
            {
                dialogueTable.Add(id, new List<RefutationEntry>());
            }
            dialogueTable[id].Add(entry);
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