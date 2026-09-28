using UnityEngine;
using UnityEngine.UI;

public class PhotoSlot : MonoBehaviour
{
    [Header("슬롯 순서 ID (1~8)")]
    public int slotID;

    [Header("시각 연출 설정")]
    public Image slotBorderImage;
    public Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 비어있을 때
    public Color filledColor = new Color(0.2f, 0.8f, 1.0f, 0.9f); // 사진 들어왔을 때

    private void Start()
    {
        if (slotBorderImage == null)
            slotBorderImage = GetComponent<Image>();

        UpdateSlotVisual();
    }

    public void UpdateSlotVisual()
    {
        if (slotBorderImage == null) return;

        // 슬롯 자식으로 PhotoItem이 들어와 있으면 filledColor 적용
        if (transform.childCount > 0)
        {
            slotBorderImage.color = filledColor;
        }
        else
        {
            slotBorderImage.color = normalColor;
        }
    }
}