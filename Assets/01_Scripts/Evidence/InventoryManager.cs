using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InventoryManager
///
/// 담당:
/// - 플레이어가 획득한 단서/아이템 목록을 관리
/// - ClueData를 인벤토리에 추가하고, 이미 보유한 단서는 중복으로 추가하지 않음
/// - 특정 clueID를 가진 단서를 보유하고 있는지 검사
/// - 단서 획득 시 InvestigationManager에 조사 진행도 갱신을 요청
/// - 단서 획득 시 GameProgressManager에 단서를 등록하여 씬 이동 이후에도 증거 목록을 유지
/// - 현재 보유한 단서를 인벤토리 UI에 표시
/// - 인벤토리 데이터를 초기화할 수 있음
///
/// 사용 위치:
/// - 조사 씬 또는 GameScene의 인벤토리 관리 오브젝트에 붙여 사용
/// - 포인트 앤 클릭 조사 파트에서 단서를 획득할 때 ClueInteract에서 호출
/// - DontDestroyOnLoad로 유지되어 씬 이동 이후에도 획득 단서 목록을 보존
///
/// 연결:
/// - ClueData의 clueID, clueIcon 등 단서 데이터를 사용
/// - InvestigationManager와 연결되어 단서 획득 시 조사 진행도를 갱신
/// - ClueDetailPopup과 연결되어 단서 획득 시 상세 팝업을 출력
/// - NoteInventoryUI와 연결되어 인벤토리 슬롯 UI를 갱신
/// - GameProgressManager.RegisterClue()를 통해 논파 씬에서도 사용할 증거 목록을 저장
///
/// TODO:
/// - NoteInventoryUI는 씬마다 새로 생성될 수 있으므로, 씬 이동 후 null 또는 Missing Reference가 되는지 확인 필요
/// - 자동 조합 시스템을 다시 사용할 경우 CheckAutoCombine 로직 재연결 필요
/// - acquiredItems와 acquiredItemsDict를 하나의 구조로 정리할지 검토
/// - 인벤토리 슬롯 클릭 시 단서 상세 설명 출력 기능과 NoteInventoryUI 역할 정리 필요
/// - 저장/로드 시스템과 연결하여 획득 단서 목록을 저장하도록 확장 필요
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("보유한 단서")]
    public List<ClueData> acquiredItems = new List<ClueData>();

    private readonly Dictionary<string, ClueData> acquiredItemsDict =
        new Dictionary<string, ClueData>();

    [Header("인벤토리 UI")]
    [SerializeField] private NoteInventoryUI noteInventoryUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateInventoryUI();
    }

    /// <summary>
    /// 외부에서 현재 씬의 NoteInventoryUI를 다시 연결할 때 사용
    /// 씬 이동 후 UI 참조가 끊기는 경우를 대비한 함수
    /// </summary>
    public void SetNoteInventoryUI(NoteInventoryUI newNoteInventoryUI)
    {
        noteInventoryUI = newNoteInventoryUI;
        UpdateInventoryUI();
    }

    /// <summary>
    /// 단서를 인벤토리에 추가
    /// 이미 같은 clueID를 가진 단서가 있으면 중복 추가하지 않음
    /// </summary>
    public void AddItem(
        ClueData itemData,
        bool updateUI = true)
    {
        if (itemData == null)
        {
            Debug.LogWarning("추가하려는 ClueData가 null입니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(itemData.clueID))
        {
            Debug.LogWarning($"{itemData.name}의 clueID가 비어 있습니다.");
            return;
        }

        if (acquiredItemsDict.ContainsKey(itemData.clueID))
        {
            return;
        }

        acquiredItems.Add(itemData);
        acquiredItemsDict[itemData.clueID] = itemData;

        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.RegisterClue(itemData);
        }

        if (InvestigationManager.Instance != null)
        {
            InvestigationManager.Instance.UpdateProgress(
                itemData,
                updateUI
            );
        }

        if (ClueDetailPopup.Instance != null)
        {
            ClueDetailPopup.Instance.ShowPopup(itemData);
        }

        if (updateUI)
        {
            UpdateInventoryUI();
        }
    }

    /// <summary>
    /// 특정 clueID를 가진 단서를 보유 중인지 확인
    /// </summary>
    public bool HasItem(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID))
        {
            return false;
        }

        return acquiredItemsDict.ContainsKey(itemID);
    }

    /// <summary>
    /// clueID를 기준으로 보유 단서를 가져옴
    /// </summary>
    public bool TryGetItem(
        string itemID,
        out ClueData clueData)
    {
        clueData = null;

        if (string.IsNullOrWhiteSpace(itemID))
        {
            return false;
        }

        return acquiredItemsDict.TryGetValue(
            itemID,
            out clueData
        );
    }

    /// <summary>
    /// 현재 보유 단서 목록을 기준으로 인벤토리 UI를 갱신
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (noteInventoryUI != null)
        {
            noteInventoryUI.RefreshInventorySlots();
        }
    }

    /// <summary>
    /// 인벤토리를 초기화
    /// 새 게임 시작 또는 테스트 초기화 시 사용할 수 있음
    /// </summary>
    public void ClearInventory()
    {
        acquiredItems.Clear();
        acquiredItemsDict.Clear();

        UpdateInventoryUI();
    }
}