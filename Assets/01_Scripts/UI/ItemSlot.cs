using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public ITEM_OBJ itemData;

    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryUI.Instance.ShowDescription(itemData);
    }
}
