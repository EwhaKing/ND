using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("사진 고유 ID (1~8)")]
    public int photoID;

    [Header("스냅 및 범위 설정")]
    public float snapSpeed = 25f;
    public float dropDistanceThreshold = 100f;

    [HideInInspector] public Transform parentAfterDrag;

    [Header("퍼즐 정보")]
    [SerializeField] private PuzzleInformation puzzleInformation;

    
    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;
    private Coroutine moveCoroutine;
    private Image myImage;
    
    private Transform initialCardArea;
    private Vector3 initialPosition;

    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.35f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        mainCanvas = GetComponentInParent<Canvas>();
        myImage = GetComponent<Image>();
    }

    private void Start()
    {
        initialCardArea = transform.parent;
        initialPosition = transform.localPosition;
    }

    // 더블클릭 감지 (IPointerClickHandler)
    public void OnPointerClick(PointerEventData eventData)
    {
        // 확대 패널이 열려 있을 때는 카드 클릭 처리 제외
        if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsPreviewActive()) return;

        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            if (myImage != null)
            {
                PuzzleManager.Instance.ShowPhotoPreview(myImage.sprite, myImage.color);
            }
        }
        lastClickTime = Time.time;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 확대 패널이 열려 있을 때는 드래그 불가
        if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsPreviewActive()) return;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

        PhotoSlot oldSlot = transform.parent.GetComponent<PhotoSlot>();

        parentAfterDrag = transform.parent;
        transform.SetParent(mainCanvas.transform);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        if (oldSlot != null)
        {
            oldSlot.UpdateSlotVisual();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (PuzzleManager.Instance != null && PuzzleManager.Instance.IsPreviewActive()) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        PhotoSlot nearestSlot = FindNearestSlot();
        PhotoSlot oldSlot = parentAfterDrag.GetComponent<PhotoSlot>();

        if (nearestSlot != null)
        {
            // 드롭하려는 슬롯에 이미 사진이 있는 경우 위치 교체(Swap)
            if (nearestSlot.transform.childCount > 0 && nearestSlot.transform.GetChild(0) != transform)
            {
                Transform existingPhoto = nearestSlot.transform.GetChild(0);
                PhotoItem existingItem = existingPhoto.GetComponent<PhotoItem>();

                existingPhoto.SetParent(parentAfterDrag);
                if (existingItem != null)
                {
                    existingItem.AnimateToParentCenter();
                }
            }

            parentAfterDrag = nearestSlot.transform;
            transform.SetParent(nearestSlot.transform);
        }
        else
        {
            parentAfterDrag = initialCardArea;
            transform.SetParent(initialCardArea);
        }

        // 출발지 및 목적지 슬롯 테두리 시각 업데이트 (중복 제거)
        if (oldSlot != null) oldSlot.UpdateSlotVisual();
        if (nearestSlot != null) nearestSlot.UpdateSlotVisual();

        AnimateToParentCenter();
        PuzzleManager.Instance.CheckPuzzleComplete();
    }

    private PhotoSlot FindNearestSlot()
    {
        PhotoSlot[] allSlots = PuzzleManager.Instance.slots;
        PhotoSlot closestSlot = null;
        float minDistance = float.MaxValue;

        foreach (PhotoSlot slot in allSlots)
        {
            if (slot == null) continue;

            float dist = Vector3.Distance(transform.position, slot.transform.position);

            if (dist < dropDistanceThreshold && dist < minDistance)
            {
                minDistance = dist;
                closestSlot = slot;
            }
        }

        return closestSlot;
    }

    public void AnimateToParentCenter()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(SmoothMoveToTarget());
    }

    private IEnumerator SmoothMoveToTarget()
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector3 targetPosition = (transform.parent == initialCardArea) ? initialPosition : Vector3.zero;

        while (Vector3.Distance(rect.localPosition, targetPosition) > 0.5f)
        {
            rect.localPosition = Vector3.Lerp(rect.localPosition, targetPosition, Time.deltaTime * snapSpeed);
            yield return null;
        }

        rect.localPosition = targetPosition;
        rect.localRotation = Quaternion.identity;
    }
}