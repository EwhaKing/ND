using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// InvestigationManager
///
/// 담당:
/// - 포인트 앤 클릭 조사 씬의 전체 조사 진행도를 관리
/// - 현재 씬에 존재하는 ClueInteract 오브젝트들을 수집하여 ClueData와 실제 맵 오브젝트를 매핑
/// - 조사 가능한 전체 단서 수와 핵심 단서 수를 계산합니다.
/// - 플레이어가 단서를 획득할 때마다 전체 조사율과 핵심 단서 발견 수를 갱신
/// - 핵심 단서를 모두 발견하면 조사 종료 버튼을 활성화
/// - ClueData를 기준으로 맵에 배치된 단서 오브젝트를 찾아 반환
///
/// 사용 위치:
/// - 포인트 앤 클릭 조사 씬의 조사 관리 오브젝트에 붙여 사용
/// - cluesParent 아래에 배치된 ClueInteract 오브젝트들을 기준으로 해당 조사 씬의 단서 목록을 구성
///
/// 연결:
/// - ClueInteract 오브젝트들을 찾아 조사 대상 단서 목록과 맵 오브젝트 Dictionary를 구성
/// - InventoryManager에서 단서 획득 시 UpdateProgress를 호출하여 조사 진행도를 갱신
/// - PointClickDialogueManager에서 대화 종료 후 UI 갱신이 필요할 때 UpdateUI를 호출
/// - ClueData의 clueType을 기준으로 핵심 단서 여부를 판단
///
/// TODO:
/// **
/// - Finish()에서 LobbyScene으로 이동하는 임시 흐름을 ChapterManager 또는 GameFlowManager와 연결하는 방식으로 수정 필요
/// - 이미 획득한 단서를 다시 획득했을 때 조사율이 중복 증가하지 않도록 방어 로직 추가 검토
/// **
/// - 조합으로 사라진 단서와 새로 생긴 단서가 조사율에 어떻게 반영될지 정책 정리 필요
/// - 핵심 단서 외에 보조 단서, 함정 단서, 숨겨진 단서의 진행도 반영 방식 정리 필요
/// - 조사 종료 조건을 “핵심 단서 전부 획득” 외에도 챕터/스테이지별로 설정할 수 있도록 확장 검토
/// - Singleton 구조 유지 여부 검토
/// </summary>
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
    private List<ClueData> countedClues = new List<ClueData>(); // 이미 조사율 계산에 포함된 단서 목록 (중복 방지)

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
        if (acquiredClue == null) return;

        if (stageClues.Contains(acquiredClue) && !countedClues.Contains(acquiredClue))
        {
            countedClues.Add(acquiredClue); 

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