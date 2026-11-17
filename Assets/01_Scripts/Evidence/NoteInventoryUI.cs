using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class NoteInventoryUI : MonoBehaviour, IPointerClickHandler
{
    [Header("인벤토리 위치 설정")]
    public RectTransform inventoryPanel;
    public Vector2 closedPosition = new Vector2(0, -800f);
    public Vector2 openPosition = new Vector2(0, 0f);

    [Header("열기/닫기 연출")]
    public float bounceHeight = 40f;    // 목표 위치보다 더 올라가는 높이
    public float riseDuration = 0.25f;  // 1단계: 위로 올라가는 시간
    public float settleDuration = 0.15f; // 2단계: 제자리로 내려앉는 시간

    [Header("슬롯 관련")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public int slotsPerPage = 6;        // 한 페이지에 보이는 슬롯 수 (3x2)

    [Header("페이지 전환 (화살표)")]
    public CanvasGroup slotGroup;       
    public Button prevButton;
    public Button nextButton;
    public float pageFadeTime = 0.15f;

    [Header("왼쪽 상세 설명창")]
    public GameObject detailPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemDescText;

    [Header("연상하기(조합) 관련 UI 및 데이터")]
    public Button combineButton;                      // 연상하기 버튼
    public List<ClueCombination> combinationRecipes;   // 레시피 파일들

    private List<ItemSlotUI> selectedSlots = new List<ItemSlotUI>();
    private List<ClueData> selectedClues = new List<ClueData>();
    private ClueCombination activeRecipe = null;      // 현재 조합 가능한 레시피 저장
    private bool isOpen = false;
    private Coroutine moveCoroutine;

    private int pageIndex = 0;
    private bool isPageChanging = false;

    public bool IsDetailPanelOpen => detailPanel != null && detailPanel.activeSelf;

    private void Start()
    {
        if (inventoryPanel == null) inventoryPanel = GetComponent<RectTransform>();
        inventoryPanel.anchoredPosition = closedPosition;
        if (detailPanel != null) detailPanel.SetActive(false);

        if (slotGroup == null && slotContainer != null)
            slotGroup = slotContainer.GetComponent<CanvasGroup>();

        // 씬이 바뀌어도 InventoryManager 가 현재 씬의 UI를 가리키도록 다시 연결
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetNoteInventoryUI(this);
        }

        // 연상하기 버튼 초기화 및 클릭 이벤트 연결
        if (combineButton != null)
        {
            combineButton.onClick.AddListener(OnCombineButtonClicked);
            combineButton.interactable = false; 
        }

        // 페이지 화살표 연결
        if (prevButton != null) prevButton.onClick.AddListener(() => ChangePage(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => ChangePage(1));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (IsDetailPanelOpen && Input.GetMouseButtonDown(0))
        {
            CloseDetailPanel();
        }
    }

    // 클립보드(빈 곳)를 눌렀을 때: 닫혀 있으면 올라오게 함
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleInventory();
    }
    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            RefreshInventorySlots();
        }
        else
        {
            CloseDetailPanel();
        }

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimatePanel(isOpen));
    }

    public void RefreshInventorySlots()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        ClearSelections(); // 슬롯 갱신 시 선택 상태 초기화

        if (InventoryManager.Instance == null) return;

        List<ClueData> items = InventoryManager.Instance.acquiredItems;

        int pageCount = GetPageCount(items.Count);
        pageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
        int startIndex = pageIndex * slotsPerPage;

        for (int i = 0; i < slotsPerPage; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();

            int itemIndex = startIndex + i;
            slotUI.SetupSlot(itemIndex < items.Count ? items[itemIndex] : null, this);
        }

        UpdatePageButtons(pageCount);
    }

    // ───────────── 페이지 전환 ─────────────

    private int GetPageCount(int itemCount)
    {
        return Mathf.Max(1, Mathf.CeilToInt(itemCount / (float)slotsPerPage));
    }

    private void UpdatePageButtons(int pageCount)
    {
        if (prevButton != null) prevButton.interactable = pageIndex > 0;
        if (nextButton != null) nextButton.interactable = pageIndex < pageCount - 1;
    }

    public void ChangePage(int delta)
    {
        if (isPageChanging || InventoryManager.Instance == null) return;

        int pageCount = GetPageCount(InventoryManager.Instance.acquiredItems.Count);
        int next = Mathf.Clamp(pageIndex + delta, 0, pageCount - 1);
        if (next == pageIndex) return;

        StartCoroutine(PageRoutine(next));
    }

    // 현재 목록이 점점 사라지고 → 다음 목록이 fade in
    private IEnumerator PageRoutine(int nextPage)
    {
        isPageChanging = true;

        yield return FadeSlots(1f, 0f);
        pageIndex = nextPage;
        RefreshInventorySlots();
        yield return FadeSlots(0f, 1f);

        isPageChanging = false;
    }

    private IEnumerator FadeSlots(float from, float to)
    {
        if (slotGroup == null) yield break;

        float t = 0f;
        while (t < pageFadeTime)
        {
            t += Time.unscaledDeltaTime;
            slotGroup.alpha = Mathf.Lerp(from, to, t / pageFadeTime);
            yield return null;
        }
        slotGroup.alpha = to;
    }

    // ───────────── 슬롯 선택 / 조합 ─────────────

    // 슬롯 클릭 시 (최대 2개 다중 선택 로직)
    public void OnSlotClicked(ClueData clue, ItemSlotUI slot)
    {
        if (clue == null) return;

        // 이미 선택된 슬롯을 다시 누른 경우 -> 선택 해제
        if (selectedSlots.Contains(slot))
        {
            slot.SetHighlight(false);
            selectedSlots.Remove(slot);
            selectedClues.Remove(clue);
        }
        else
        {
            // 이미 2개가 선택되어 있다면 가장 먼저 선택했던 단서 1개를 해제
            if (selectedSlots.Count >= 2)
            {
                selectedSlots[0].SetHighlight(false);
                selectedSlots.RemoveAt(0);
                selectedClues.RemoveAt(0);
            }

            // 새로운 선택 추가
            selectedSlots.Add(slot);
            selectedClues.Add(clue);
            slot.SetHighlight(true);
        }

        // 2개가 선택되었을 때 조합 가능한지 체크
        CheckCombinationState();
    }

    // 선택된 2개의 단서가 레시피와 맞는지 확인하는 함수
    private void CheckCombinationState()
    {
        activeRecipe = null;

        if (selectedClues.Count == 2)
        {
            foreach (var recipe in combinationRecipes)
            {
                if (recipe != null && recipe.Matches(selectedClues[0], selectedClues[1]))
                {
                    activeRecipe = recipe;
                    break;
                }
            }
        }

        // 맞는 레시피가 있다면 연상하기 버튼 활성화
        if (combineButton != null)
        {
            combineButton.interactable = (activeRecipe != null);
        }
    }

    private void OnCombineButtonClicked()
    {
        if (activeRecipe == null || InventoryManager.Instance == null) return;

        //(딕셔너리 등록, 진행도 갱신, 팝업 처리를 한 번에 수행)
        InventoryManager.Instance.AddItem(activeRecipe.resultClue);

        // 선택 해제 및 우측 인벤토리 슬롯 새로고침
        ClearSelections();
        RefreshInventorySlots();
    }

    private void ClearSelections()
    {
        foreach (var slot in selectedSlots)
        {
            if (slot != null) slot.SetHighlight(false);
        }
        selectedSlots.Clear();
        selectedClues.Clear();
        activeRecipe = null;

        if (combineButton != null) combineButton.interactable = false;
    }

    //더블 클릭 시 ClueDetailPopup 팝업을 띄움
    public void OnSlotDoubleClicked(ClueData clue, ItemSlotUI slot)
    {
        if (clue == null) return;

        if (ClueDetailPopup.Instance != null)
        {
            ClueDetailPopup.Instance.ShowPopup(clue);
        }
    }

    public void CloseDetailPanel()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    // ───────────── 열기/닫기 연출 ─────────────

    // 열 때: 목표 위치보다 살짝 더 올라갔다가 안착 → 조사목록 fade in
    // 닫을 때: 현재 위치에서 살짝 위로 올라갔다가 아래로 내려감
    private IEnumerator AnimatePanel(bool open)
    {
        Vector2 start = inventoryPanel.anchoredPosition;
        Vector2 end = open ? openPosition : closedPosition;
        Vector2 peak = (open ? end : start) + Vector2.up * bounceHeight;

        if (open && slotGroup != null) slotGroup.alpha = 0f;

        yield return MoveTo(start, peak, riseDuration);
        yield return MoveTo(peak, end, settleDuration);

        if (open) yield return FadeSlots(0f, 1f);
    }

    private IEnumerator MoveTo(Vector2 from, Vector2 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            inventoryPanel.anchoredPosition = Vector2.Lerp(from, to, k);
            yield return null;
        }
        inventoryPanel.anchoredPosition = to;
    }
}
