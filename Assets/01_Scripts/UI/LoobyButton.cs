using UnityEngine;
using UnityEngine.EventSystems;

public class LoobyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float hoverScale = 1.1f; // hover 때 커지는 배율
    [SerializeField] float speed = 12f;       // 커지는 속도

    Vector3 baseScale;
    Vector3 targetScale;

    void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
    }

    void OnDisable()
    {
        transform.localScale = baseScale;
        targetScale = baseScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = baseScale * hoverScale;
    public void OnPointerExit(PointerEventData eventData) => targetScale = baseScale;
}