using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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