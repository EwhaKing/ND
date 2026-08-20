using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NoteInventoryUI : MonoBehaviour
{
    [Header("인벤토리 위치 설정")]
    public RectTransform inventoryPanel;
    public Vector2 closedPosition = new Vector2(0, -800f);
    public Vector2 openPosition = new Vector2(0, 0f);
    public float slideSpeed = 8f;

    [Header("슬롯 관련")]
    public Transform slotContainer;
    public GameObject slotPrefab;
    public int maxSlotCount = 12;

    [Header("왼쪽 상세 설명창")]
    public GameObject detailPanel;
    public TMP_Text itemNameText;
    public TMP_Text itemDescText;

    [Header("연상하기(조합) 관련 UI 및 데이터")]
    public Button combineButton;                      // 연상하기 버튼
    public List<ClueCombination> combinationRecipes;   // Step 2에서 만든 레시피 파일들

    private List<ItemSlotUI> selectedSlots = new List<ItemSlotUI>();
    private List<ClueData> selectedClues = new List<ClueData>();
    private ClueCombination activeRecipe = null;      // 현재 조합 가능한 레시피 저장
    private bool isOpen = false;
    private Coroutine moveCoroutine;

    public bool IsDetailPanelOpen => detailPanel != null && detailPanel.activeSelf;

    private void Start()
    {
        if (inventoryPanel == null) inventoryPanel = GetComponent<RectTransform>();
        inventoryPanel.anchoredPosition = closedPosition;
        if (detailPanel != null) detailPanel.SetActive(false);

        // 연상하기 버튼 초기화 및 클릭 이벤트 연결
        if (combineButton != null)
        {
            combineButton.onClick.AddListener(OnCombineButtonClicked);
            combineButton.interactable = false; // 기본 상태: 비활성화
        }
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
        moveCoroutine = StartCoroutine(AnimatePanel(isOpen ? openPosition : closedPosition));
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

        for (int i = 0; i < maxSlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();

            if (i < items.Count)
            {
                slotUI.SetupSlot(items[i], this);
            }
            else
            {
                slotUI.SetupSlot(null, this);
            }
        }
    }

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

        // NoteInventoryUI.cs 내부의 OnSlotDoubleClicked 함수 수정
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

    private IEnumerator AnimatePanel(Vector2 targetPos)
    {
        while (Vector2.Distance(inventoryPanel.anchoredPosition, targetPos) > 0.5f)
        {
            inventoryPanel.anchoredPosition = Vector2.Lerp(inventoryPanel.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
            yield return null;
        }
        inventoryPanel.anchoredPosition = targetPos;
    }
}