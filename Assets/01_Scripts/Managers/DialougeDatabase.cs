using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DialogueDatabase : MonoBehaviour
{
    [Serializable]
    public class DialogueEntry
    {
        public string id;
        public string speaker;
        public string dialogue;
    }

    [Header("CSV")]
    [SerializeField] private TextAsset dialogueCsv;

    private readonly Dictionary<string, DialogueEntry> table = new();

    public bool IsLoaded { get; private set; }

    private void Awake()
    {
        LoadCsv();
    }

    public DialogueEntry GetDialogue(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("대사 ID가 비어 있습니다.");
            return null;
        }

        if (table.TryGetValue(id, out DialogueEntry entry))
        {
            return entry;
        }

        Debug.LogError($"CSV에서 대사 ID를 찾지 못했습니다: {id}");
        return null;
    }

    public bool TryGetDialogue(
        string id,
        out DialogueEntry entry)
    {
        return table.TryGetValue(id, out entry);
    }

    private void LoadCsv()
    {
        table.Clear();
        IsLoaded = false;

        if (dialogueCsv == null)
        {
            Debug.LogError(
                "DialogueDatabase에 CSV 파일이 연결되지 않았습니다."
            );
            return;
        }

        List<string[]> rows = ParseCsv(dialogueCsv.text);

        if (rows.Count <= 1)
        {
            Debug.LogError(
                $"CSV에 대사 데이터가 없습니다: {dialogueCsv.name}"
            );
            return;
        }

        // 첫 번째 행은 id,speaker,dialogue 헤더이므로 건너뛴다.
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];

            if (row.Length < 3)
            {
                Debug.LogWarning(
                    $"{dialogueCsv.name}의 {i + 1}행 열 개수가 부족합니다."
                );
                continue;
            }

            string id = row[0].Trim();
            string speaker = row[1].Trim();
            string dialogue = row[2].Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning(
                    $"{dialogueCsv.name}의 {i + 1}행 ID가 비어 있습니다."
                );
                continue;
            }

            if (table.ContainsKey(id))
            {
                Debug.LogError(
                    $"중복된 대사 ID가 있습니다: {id}"
                );
                continue;
            }

            table.Add(
                id,
                new DialogueEntry
                {
                    id = id,
                    speaker = speaker,
                    dialogue = dialogue
                }
            );
        }

        IsLoaded = true;

        Debug.Log(
            $"대사 CSV 로드 완료: {table.Count}개"
        );
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
            char character = csvText[i];

            if (character == '"')
            {
                if (insideQuotes &&
                    i + 1 < csvText.Length &&
                    csvText[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            if ((character == '\n' || character == '\r') &&
                !insideQuotes)
            {
                if (character == '\r' &&
                    i + 1 < csvText.Length &&
                    csvText[i + 1] == '\n')
                {
                    continue;
                }

                currentRow.Add(currentValue.ToString());
                currentValue.Clear();

                if (!IsEmptyRow(currentRow))
                {
                    rows.Add(currentRow.ToArray());
                }

                currentRow = new List<string>();
                continue;
            }

            currentValue.Append(character);
        }

        currentRow.Add(currentValue.ToString());

        if (!IsEmptyRow(currentRow))
        {
            rows.Add(currentRow.ToArray());
        }

        return rows;
    }

    private bool IsEmptyRow(List<string> row)
    {
        foreach (string value in row)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
        }

        return true;
    }
}