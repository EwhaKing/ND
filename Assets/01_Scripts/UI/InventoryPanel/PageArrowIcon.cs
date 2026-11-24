using UnityEngine;
using UnityEngine.UI;

// 페이지 화살표 아이콘 (◁ ▷ 가 한 장에 그려진 이미지용)
// 마우스를 올리거나 누르고 있는 쪽 화살표가 초록색 이미지로 바뀝니다.
// ArrowIcon(Image) 오브젝트에 붙이세요.
public class PageArrowIcon : MonoBehaviour
{
    [SerializeField] Image icon;

    [Header("스프라이트 3장")]
    [SerializeField] Sprite normalSprite;      // 평소: 둘 다 검정
    [SerializeField] Sprite prevGreenSprite;   // 왼쪽(◁)만 초록
    [SerializeField] Sprite nextGreenSprite;   // 오른쪽(▷)만 초록

    [Header("클릭 영역 (아이콘 위에 올린 투명 버튼 2개)")]
    [SerializeField] Button prevButton;
    [SerializeField] Button nextButton;

    Canvas rootCanvas;

    void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();

        Canvas c = GetComponentInParent<Canvas>();
        rootCanvas = c != null ? c.rootCanvas : null;
    }

    void OnEnable()
    {
        if (icon != null && normalSprite != null) icon.sprite = normalSprite;
    }

    void Update()
    {
        if (icon == null) return;

        Sprite target = normalSprite;

        if (IsOver(prevButton)) target = prevGreenSprite;
        else if (IsOver(nextButton)) target = nextGreenSprite;

        if (target == null) target = normalSprite;
        if (icon.sprite != target) icon.sprite = target;
    }

    // 마우스가 버튼 영역 안에 있고, 버튼이 눌릴 수 있는 상태일 때만 true
    // (첫/마지막 페이지에서 못 넘기는 쪽은 초록색이 되지 않음)
    bool IsOver(Button button)
    {
        if (button == null || !button.interactable || !button.gameObject.activeInHierarchy)
            return false;

        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)button.transform, Input.mousePosition, cam);
    }
}