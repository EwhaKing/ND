using System.Collections.Generic;
using System.Text;

public static class CsvParser
{
    public static List<string[]> Parse(string csvText)
    {
        List<string[]> rows = new();
        List<string> currentRow = new();
        StringBuilder currentValue = new();
        bool insideQuotes = false;

        csvText = csvText.TrimStart('\uFEFF'); // BOM 제거

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (c == '"')
            {
                // 연속된 큰따옴표("") 이스케이프 처리
                if (insideQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
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

            if (c == ',' && !insideQuotes)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            if ((c == '\n' || c == '\r') && !insideQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n') continue;
                
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
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