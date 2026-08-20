using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueDetailPopup : MonoBehaviour
{
    public static ClueDetailPopup Instance { get; private set; }

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject popupPanel;     
    [SerializeField] private Image clueImage;           
    [SerializeField] private TMP_Text clueNameText;     
    [SerializeField] private TMP_Text clueDescText;     

    private bool canClose = false; // 열리자마자 바로 닫히는 것 방지 플래그

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void Update()
    {
        // 팝업이 켜져 있고, 닫힐 준비(canClose)가 되었을 때 클릭하면 닫기
        if (popupPanel != null && popupPanel.activeSelf && canClose)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ClosePopup();
            }
        }
    }

    public void ShowPopup(ClueData clueData)
    {
        if (clueData == null) return;

        // 1. 이미지 설정
        if (clueImage != null)
        {
            if (clueData.clueIcon != null)
            {
                clueImage.sprite = clueData.clueIcon;
                clueImage.gameObject.SetActive(true);
            }
            else
            {
                clueImage.gameObject.SetActive(false);
            }
        }

        // 2. 텍스트 예비 로직 (inventoryDescription -> firstClickText -> secondClickText 순서로 체크)
        if (clueNameText != null) clueNameText.text = clueData.clueName;
        
        if (clueDescText != null)
        {
            if (!string.IsNullOrEmpty(clueData.inventoryDescription))
                clueDescText.text = clueData.inventoryDescription;
            else if (!string.IsNullOrEmpty(clueData.firstClickText))
                clueDescText.text = clueData.firstClickText;
            else if (!string.IsNullOrEmpty(clueData.secondClickText))
                clueDescText.text = clueData.secondClickText;
            else
                clueDescText.text = "설명이 없습니다.";
        }

        // 3. 팝업 켜기
        popupPanel.SetActive(true);
        popupPanel.transform.SetAsLastSibling();

        // 클릭 이벤트 중첩 방지를 위해 다음 프레임부터 닫을 수 있게 설정
        canClose = false;
        Invoke(nameof(EnableClose), 0.1f);
    }

    private void EnableClose()
    {
        canClose = true;
    }

    public void ClosePopup()
    {
        canClose = false;
        if (popupPanel != null) popupPanel.SetActive(false);
    }
}