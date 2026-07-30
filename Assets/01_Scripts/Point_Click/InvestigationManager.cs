using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class InvestigationManager : MonoBehaviour
{
    public static InvestigationManager Instance;

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI progressRateText; // 조사율(%) 텍스트
    public TextMeshProUGUI coreCountText; // 핵심 단서 발견 개수/총 개수 텍스트
    public GameObject finishButton; // 조사 종료 버튼

    [Header("현재 장소에 존재하는 모든 단서")]
    public List<ClueData> stageClues = new List<ClueData>();

    private int totalCoreCount = 0; // 핵심 단서의 총 개수
    private int totalProgressCount = 0; // 전체 단서의 총 개수

    private int foundCoreCount = 0; // 발견한 핵심 단서의 개수
    private int foundProgressCount = 0; // 발견한 전체 단서의 개수

    private int progressRate = 0; // 조사율 (0~100)
    private bool isFinishabled = false; // 조사 종료 활성화 여부

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (finishButton != null)
        {
            finishButton.SetActive(false);
        }

        // 전체 단서 개수와 핵심 단서 개수 초기화
        foreach (ClueData clue in stageClues)
        {
            if (clue.clueType == ClueType.Core) 
            {
                totalCoreCount++;
            }
            
            totalProgressCount++;
        }

        UpdateUI();
    }

    // UI 업데이트 메서드
    public void UpdateUI()
    {
        if (progressRateText != null) 
        {
            progressRateText.text = $"조사율: {progressRate}%";
        }
        
        if (coreCountText != null) 
        {
            coreCountText.text = $"핵심 단서: {foundCoreCount} / {totalCoreCount}";
        }

        if (foundCoreCount >= totalCoreCount && !isFinishabled)
        {
            EnableToFinish();
        }
    }
    // 조사율 업데이트 메서드
    public void UpdateProgress(ClueData acquiredClue, bool updateUI = true)
    {
        if (stageClues.Contains(acquiredClue))
        {
            if (acquiredClue.clueType == ClueType.Core) 
            {
                foundCoreCount++;
            }
            foundProgressCount++;

            progressRate = Mathf.FloorToInt(((float)foundProgressCount / totalProgressCount) * 100f);

            if (updateUI)
            {
                UpdateUI();
            }
        }
    }
    

    // 조사 종료 활성화 메서드
    private void EnableToFinish()
    {
        if (!isFinishabled)
        {
            isFinishabled = true;

            if (finishButton != null)
            {
                finishButton.SetActive(true);
            }
        }
    }

    public void Finish()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}