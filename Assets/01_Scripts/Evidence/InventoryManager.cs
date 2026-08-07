using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryManager
///
/// 담당:
/// - 플레이어가 획득한 단서/아이템 목록을 관리
/// - ClueData를 인벤토리에 추가하고, 이미 보유한 단서는 중복으로 추가하지 않음
/// - 특정 clueID를 가진 단서를 보유하고 있는지 검사
/// - 단서 획득 시 InvestigationManager에 조사 진행도 갱신을 요청
/// - 조합 가능한 단서가 함께 존재할 경우 자동 조합을 처리
/// - 현재 보유한 단서를 인벤토리 슬롯 UI에 표시
///
/// 사용 위치:
/// - 조사 씬 또는 GameScene의 인벤토리 관리 오브젝트에 부착
/// - 포인트 앤 클릭 조사 파트에서 단서를 획득할 때 호출
///
/// 연결:
/// - ClueData의 clueID, clueIcon, canCombine, combineTarget, combineResult, combineText 정보를 사용
/// - InvestigationManager와 연결되어 단서 획득 및 조합 결과를 조사 진행도에 반영
/// - PointClickDialogueManager와 함께 단서 획득 후 UI 갱신 흐름에 사용
/// - InventoryUI 또는 ItemSlot UI와 연결되어 보유 단서를 화면에 표시
///
/// TODO:
/// - acquiredItems를 List가 아니라 clueID 기반 Dictionary 또는 HashSet으로 관리할지 검토
/// - 핵심 단서 / 보조 단서 / 함정 단서 구분을 EvidenceManager와 분리할지 통합할지 결정 필요
/// - 자동 조합 결과가 이미 인벤토리에 있는 경우의 중복 처리 추가
/// - 인벤토리 슬롯 클릭 시 단서 상세 설명 출력 기능 연결
/// - Singleton 구조 유지 여부 검토
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("보유한 단서")]
    public List<ClueData> acquiredItems = new List<ClueData>();

    [Header("인벤토리 아이콘")]
    public Image[] slotIcons; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 인벤토리 UI 초기화
        UpdateInventoryUI();
    }

    // 아이템 추가 기능
    public string AddItem(ClueData itemData, bool updateUI = true)
    {
        string combineMessage = null;
        
        if (!acquiredItems.Contains(itemData))
        {
            acquiredItems.Add(itemData);

            if (InvestigationManager.Instance != null)
            {
                InvestigationManager.Instance.UpdateProgress(itemData, updateUI);
            }

            combineMessage = CheckAutoCombine(itemData, updateUI); // 합성 가능 여부 확인 및 처리
            
            if (updateUI)
            {
                UpdateInventoryUI();
            }
        }

        return combineMessage;
    }

    // 아이템 보유 검사 기능
    public bool HasItem(string itemID)
    {
        foreach (ClueData item in acquiredItems)
        {
            if (item.clueID == itemID) return true;
        }
        return false;
    }

    private string CheckAutoCombine(ClueData newItem, bool updateUI)
    {
        ClueData partnerItem = null;

        foreach (ClueData existingItem in acquiredItems)
        {
            if (existingItem == newItem) 
            {
                continue;
            }

            if (newItem.canCombine && newItem.combineTarget == existingItem)
            {
                partnerItem = existingItem;
                break;
            }
        }

        if (partnerItem != null)
        {
            ClueData resultItem = newItem.combineResult;

            if (resultItem != null)
            {
                acquiredItems.Remove(newItem);
                acquiredItems.Remove(partnerItem);

                acquiredItems.Add(resultItem);

                if (InvestigationManager.Instance != null)
                {
                    InvestigationManager.Instance.UpdateProgress(resultItem, updateUI);
                }

                string customText = newItem.combineText;                                

                return customText;
            }
        }

        return null;
    }

    // 인벤토리 UI 업데이트 기능
    public void UpdateInventoryUI()
    {
        if (slotIcons == null || slotIcons.Length == 0) 
        {
            return;
        }

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i < acquiredItems.Count)
            {
                slotIcons[i].sprite = acquiredItems[i].clueIcon;
                slotIcons[i].gameObject.SetActive(true);
            }
            else
            {
                slotIcons[i].gameObject.SetActive(false);
            }
        }
    }
}