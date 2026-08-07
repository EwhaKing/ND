using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// DialogueDatabase
///
/// 담당:
/// - CSV 파일에 작성된 대화 데이터를 읽어와 ID 기반 Dictionary로 저장
/// - dialogueId를 통해 화자 이름과 대사 내용을 조회할 수 있도록 관리
/// - 따옴표가 포함된 CSV, 쉼표가 포함된 대사, 빈 줄 등을 처리하는 기본 CSV 파서를 포함
///
/// 사용 위치:
/// - 대화 데이터베이스 역할을 하는 오브젝트에 부착
/// - ScenarioRunner가 dialogueId를 기반으로 실제 대사 내용을 가져올 때 사용
///
/// 연결:
/// - ScenarioData의 dialogueId와 연결
/// - ChatDialogueManager에 전달할 speaker/dialogue 데이터 제공
/// - 추후 구글 스프레드시트에서 내려받은 CSV 데이터와 연결 가능
///
/// TODO:
/// - Type 컬럼을 추가하여 Dialogue / Narration / CG / Command 타입 구분 지원
/// - CG ID, Effect, Wait, Standing, Position, Expression 등 추가 컬럼 파싱 지원
/// - 구글 스프레드시트 연동 시 CSV 갱신/로드 방식 정리
/// 
/// - 한글 깨짐이 있는 Debug 문자열 정리 필요
/// </summary>
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
            Debug.LogError("��� ID�� ��� �ֽ��ϴ�.");
            return null;
        }

        if (table.TryGetValue(id, out DialogueEntry entry))
        {
            return entry;
        }

        Debug.LogError($"CSV���� ��� ID�� ã�� ���߽��ϴ�: {id}");
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
                "DialogueDatabase�� CSV ������ ������� �ʾҽ��ϴ�."
            );
            return;
        }

        List<string[]> rows = ParseCsv(dialogueCsv.text);

        if (rows.Count <= 1)
        {
            Debug.LogError(
                $"CSV�� ��� �����Ͱ� �����ϴ�: {dialogueCsv.name}"
            );
            return;
        }

        // ù ��° ���� id,speaker,dialogue ����̹Ƿ� �ǳʶڴ�.
        for (int i = 1; i < rows.Count; i++)
        {
            string[] row = rows[i];

            if (row.Length < 3)
            {
                Debug.LogWarning(
                    $"{dialogueCsv.name}�� {i + 1}�� �� ������ �����մϴ�."
                );
                continue;
            }

            string id = row[0].Trim();
            string speaker = row[1].Trim();
            string dialogue = row[2].Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning(
                    $"{dialogueCsv.name}�� {i + 1}�� ID�� ��� �ֽ��ϴ�."
                );
                continue;
            }

            if (table.ContainsKey(id))
            {
                Debug.LogError(
                    $"�ߺ��� ��� ID�� �ֽ��ϴ�: {id}"
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
            $"��� CSV �ε� �Ϸ�: {table.Count}��"
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