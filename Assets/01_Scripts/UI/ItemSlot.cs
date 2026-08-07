using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ItemSlot
///
/// 담당:
/// - 인벤토리 슬롯 UI의 클릭 이벤트를 처리
/// - 슬롯에 연결된 ITEM_OBJ 데이터를 InventoryUI에 전달하여 아이템 설명을 표시
///
/// 사용 위치:
/// - 인벤토리 슬롯 버튼 또는 슬롯 이미지 오브젝트에 부착
/// - EventSystem의 IPointerClickHandler를 통해 마우스 클릭을 감지
///
/// 연결:
/// - InventoryUI.Instance.ShowDescription()을 호출
/// - ITEM_OBJ 데이터를 통해 아이템 설명 정보를 전달
///
/// TODO:
/// - itemData가 null일 경우 클릭 처리 방어 로직 추가
/// - InventoryUI.Instance가 null일 경우의 예외 처리 추가
/// - ClueData 기반 인벤토리와 ITEM_OBJ 기반 인벤토리 구조 통합 여부 검토
/// </summary>
public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public ITEM_OBJ itemData;

    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryUI.Instance.ShowDescription(itemData);
    }
}
