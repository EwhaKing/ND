using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryPanelController
///
/// 담당:
/// - 인벤토리 창 UI의 열기/닫기 상태를 제어
/// - 인벤토리 창을 열 때 기존 아이템 설명 패널을 숨겨 초기 상태로 정리
///
/// 사용 위치:
/// - 인벤토리 창 패널 또는 인벤토리 UI 컨트롤러 오브젝트에 부착
/// - 인벤토리 버튼, 닫기 버튼의 OnClick 이벤트와 연결
///
/// 연결:
/// - InventoryUI.Instance.HideDescription()을 호출하여 인벤토리 설명창을 닫음
/// - InventoryUI와 함께 인벤토리 창 표시 및 아이템 설명 표시 흐름을 구성
///
/// TODO:
/// - InventoryManager와 혼동되지 않도록 데이터 관리와 UI 창 제어 역할을 분리
/// - gameObject 자체를 비활성화하는 구조 대신, 별도의 inventoryPanel GameObject를 제어하는 방식 검토
/// </summary>

public class InventoryPanelController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private void Start()
    {
        CloseWindow();
    }

    public void OpenWindow()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }

        // 인벤토리 열 때 설명창 초기화
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.HideDescription();
        }
    }

    public void CloseWindow()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }
}