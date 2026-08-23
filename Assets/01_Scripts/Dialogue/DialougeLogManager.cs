using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueLogManager : MonoBehaviour
{
    [System.Serializable]
    public class LogEntry
    {
        public string speaker;
        public string dialogue;
    }

    [Header("UI")]
    [SerializeField] private GameObject logPanel;
    [SerializeField] private Transform logContent;
    [SerializeField] private TMP_Text logTextPrefab;

    private readonly List<LogEntry> logs =
        new List<LogEntry>();

    public void AddLog(string speaker, string dialogue)
    {
        if (string.IsNullOrWhiteSpace(dialogue))
        {
            return;
        }

        logs.Add(
            new LogEntry
            {
                speaker = speaker, dialogue = dialogue
            }
        );

    }

    public void ShowLog()
    {
        RefreshLog();
        if (logPanel != null)
        {
            logPanel.SetActive(true);
        }
    }

    public void HideLog()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }
    }

    private void RefreshLog()
    {
        if (logContent == null ||
            logTextPrefab == null)
        {
            Debug.LogError(
                "LogContent 또는 LogTextPrefab이 연결되지 않았습니다."
            );

            return;
        }

        // 기존 생성 로그 삭제
        foreach (Transform child in logContent)
        {
            Destroy(child.gameObject);
        }

        // 저장된 로그 다시 생성
        foreach (LogEntry log in logs)
        {
            TMP_Text newText =
                Instantiate(logTextPrefab,logContent);

            if (string.IsNullOrWhiteSpace(log.speaker))
            {
                newText.text = log.dialogue;
            }
            else
            {
                newText.text =
                    $"{log.speaker} | {log.dialogue}";
            }
        }
    }
}