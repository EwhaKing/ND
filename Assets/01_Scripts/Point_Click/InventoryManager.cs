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
    public void AddItem(ClueData itemData, bool updateUI = true)
    {
        if (!acquiredItems.Contains(itemData))
        {
            acquiredItems.Add(itemData);

            if (InvestigationManager.Instance != null)
            {
                InvestigationManager.Instance.UpdateProgress(itemData, updateUI);
            }
            
            if (updateUI)
            {
                UpdateInventoryUI();
            }
        }
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