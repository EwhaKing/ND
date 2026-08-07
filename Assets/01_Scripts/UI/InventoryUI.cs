using UnityEngine;
using TMPro;

/// <summary>
/// InventoryUI
///
/// 담당:
/// - 인벤토리에서 선택한 아이템의 상세 설명 패널을 관리
/// - ItemSlot에서 전달받은 ITEM_OBJ 데이터를 기반으로 설명 텍스트를 출력
/// - 같은 아이템을 다시 클릭하면 설명창을 닫는 토글 방식을 처리
/// - 인벤토리 창을 닫거나 다른 아이템을 선택할 때 현재 선택 정보를 초기화
///
/// 사용 위치:
/// - 인벤토리 UI의 설명 패널을 관리하는 오브젝트에 부착
/// - 아이템 슬롯 클릭 시 ItemSlot에서 ShowDescription()을 호출
///
/// 연결:
/// - ItemSlot에서 전달한 ITEM_OBJ 데이터를 사용
/// - InventoryManagerUI1 또는 InventoryWindowController에서 HideDescription()을 호출하여 설명창을 초기화
///
/// TODO:
/// - ITEM_OBJ와 ClueData가 둘 다 존재하므로 아이템/단서 데이터 구조 통합 여부 검토
/// 
/// - descriptionPanel, descriptionText가 null일 경우의 예외 처리 추가
/// - 아이템 이름, 아이콘, 획득 조건 등 추가 정보 표시 기능 확장 검토
/// - Singleton 구조 유지 여부 검토
/// </summary>
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