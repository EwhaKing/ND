using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Outline highlightOutline; // Outline 컴포넌트 연결
    [SerializeField] private Button slotButton;         // Button 컴포넌트 연결

    private ClueData currentClueData;
    private NoteInventoryUI mainUI;
    private float lastClickTime;
    private const float doubleClickThreshold = 0.25f;

    private void Awake()
    {
        // 스크립트에서 Button 컴포넌트를 자동으로 가져옵니다.
        if (slotButton == null) slotButton = GetComponent<Button>();
    }

    public void SetupSlot(ClueData clueData, NoteInventoryUI ui)
    {
        currentClueData = clueData;
        mainUI = ui;

        if (currentClueData != null && currentClueData.clueIcon != null)
        {
            iconImage.sprite = currentClueData.clueIcon;
            iconImage.gameObject.SetActive(true);

            // 아이템이 있으면 버튼 상호작용 활성화
            if (slotButton != null) slotButton.interactable = true;
        }
        else
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);

            // 빈 슬롯이면 버튼 상호작용 비활성화 (누름 효과/회색 변형 방지)
            if (slotButton != null) slotButton.interactable = false;
        }

        SetHighlight(false); // 기본은 테두리 끄기
    }

    public void SetHighlight(bool isSelected)
    {
        if (highlightOutline != null)
        {
            highlightOutline.enabled = isSelected; // 선택 시 테두리 컴포넌트만 켜기/끄기
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 아이템 데이터가 없는 빈 슬롯은 클릭 로직을 즉시 무시합니다.
        if (currentClueData == null) return;

        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            mainUI.OnSlotDoubleClicked(currentClueData, this);
        }
        else
        {
            mainUI.OnSlotClicked(currentClueData, this);
        }

        lastClickTime = Time.time;
    }
}