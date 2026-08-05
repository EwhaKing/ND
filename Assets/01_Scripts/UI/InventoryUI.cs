using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    public GameObject descriptionPanel;
    public TextMeshProUGUI descriptionText;

    private ITEM_OBJ currentItem; // 현재 열려있는 아이템 기억

    private void Awake()
    {
        Instance = this;
    }

    // 아이템 클릭했을 때 호출 (토글 방식)
    public void ShowDescription(ITEM_OBJ item)
    {
        if (item == null) return;

        // 같은 아이템을 다시 클릭했고, 지금 설명창이 열려있는 상태라면 → 닫기
        if (currentItem == item && descriptionPanel.activeSelf)
        {
            HideDescription();
            return;
        }

        // 다른 아이템이거나, 닫혀있던 상태라면 → 열기
        descriptionPanel.SetActive(true);
        descriptionText.text = item.Description;
        currentItem = item;
    }

    // 설명창 숨기기 (인벤토리 닫을 때, 토글로 닫을 때 둘 다 사용)
    public void HideDescription()
    {
        descriptionPanel.SetActive(false);
        currentItem = null; // ← 이게 핵심: 선택된 아이템 정보도 같이 초기화
    }
}