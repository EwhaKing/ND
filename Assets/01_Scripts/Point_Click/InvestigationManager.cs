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

    [Header("단서모음 부모 오브젝트")]
    public Transform cluesParent;

    // ClueData와 맵 오브젝트를 매핑하는 딕셔너리
    private Dictionary<ClueData, GameObject> clueObjectMap = new Dictionary<ClueData, GameObject>();
    private List<ClueData> stageClues = new List<ClueData>();


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

        // 맵에 존재하는 모든 ClueInteract 오브젝트를 찾아 clueObjectMap에 등록
        if (cluesParent != null)
        {
            ClueInteract[] allCluesOnMap = cluesParent.GetComponentsInChildren<ClueInteract>(true);
            
            foreach (ClueInteract clue in allCluesOnMap)
            {
                if (clue.clueData != null)
                {
                    // 1. 오브젝트 매핑 사전 등록
                    clueObjectMap[clue.clueData] = clue.gameObject;

                    // 2. 단서 자동 수집 및 전체/핵심 단서 개수 세기 (중복 방지)
                    if (!stageClues.Contains(clue.clueData))
                    {
                        stageClues.Add(clue.clueData);

                        if (clue.clueData.clueType == ClueType.Core) 
                        {
                            totalCoreCount++;
                        }
                        
                        totalProgressCount++;
                    }
                }
            }
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

    // ClueData를 기반으로 맵 오브젝트를 찾아 반환하는 메서드
    public GameObject GetClueObject(ClueData targetData)
    {
        if (targetData != null && clueObjectMap.ContainsKey(targetData))
        {
            return clueObjectMap[targetData];
        }
        return null; 
    }

    // 조사 종료 버튼 클릭 시 호출되는 메서드
    public void Finish()
    {
        SceneManager.LoadScene("LobbyScene");
    }
    
}