using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 평소엔 반투명 → 마우스를 올리면 불투명 + 외곽선이 나타남.
// MainInventoryPanel(루트)에 붙이세요. CanvasGroup 이 자동으로 추가됩니다.
[RequireComponent(typeof(CanvasGroup))]
public class NoteFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Range(0f, 1f)] float idleAlpha = 0.6f;   // 평소 투명도
    [SerializeField, Range(0f, 1f)] float hoverAlpha = 1f;    // hover 시 투명도
    [SerializeField] float speed = 8f;                        // 변하는 속도
    [SerializeField] Outline outline;                         // 클립보드 배경 Image에 붙인 Outline

    CanvasGroup group;
    bool hovering;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = idleAlpha;
        SetOutlineAlpha(0f);
    }

    void OnDisable()
    {
        hovering = false;
    }

    void Update()
    {
        float target = hovering ? hoverAlpha : idleAlpha;
        group.alpha = Mathf.MoveTowards(group.alpha, target, speed * Time.unscaledDeltaTime);

        // 투명도가 풀리는 정도에 맞춰 외곽선도 서서히 나타남
        SetOutlineAlpha(Mathf.InverseLerp(idleAlpha, hoverAlpha, group.alpha));
    }

    void SetOutlineAlpha(float a)
    {
        if (outline == null) return;
        Color c = outline.effectColor;
        c.a = a;
        outline.effectColor = c;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;
    public void OnPointerExit(PointerEventData eventData) => hovering = false;
}