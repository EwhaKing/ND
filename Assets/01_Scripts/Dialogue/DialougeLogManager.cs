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

    public void AddLog(
        string speaker,
        string dialogue)
    {
        if (string.IsNullOrWhiteSpace(dialogue))
        {
            return;
        }

        LogEntry newLog = new LogEntry
        {
            speaker = speaker,
            dialogue = dialogue
        };

        logs.Add(newLog);
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
            return;
        }

        foreach (Transform child in logContent)
        {
            Destroy(child.gameObject);
        }

        foreach (LogEntry log in logs)
        {
            TMP_Text newText =
                Instantiate(
                    logTextPrefab,
                    logContent
                );

            if (string.IsNullOrWhiteSpace(log.speaker))
            {
                newText.text = log.dialogue;
            }
            else
            {
                newText.text =
                    $"{log.speaker}\n{log.dialogue}";
            }
        }
    }
}