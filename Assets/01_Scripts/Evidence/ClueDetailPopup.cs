using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClueDetailPopup : MonoBehaviour
{
    public static ClueDetailPopup Instance { get; private set; }

    [Header("연결할 대화 UI")]
    [SerializeField] private GameObject dialoguePanel;

    private bool dialogueWasActive;

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private Image clueImage;
    [SerializeField] private TMP_Text clueNameText;
    [SerializeField] private TMP_Text clueDescText;

    private bool canClose = false;

    // 팝업이 닫힌 뒤 실행할 함수
    private Action onCloseCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (popupPanel != null &&
            popupPanel.activeSelf &&
            canClose)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ClosePopup();
            }
        }
    }

    public void ShowPopup(
        ClueData clueData,
        Action onClose = null)
    {
        if (clueData == null)
        {
            return;
        }

        // 이번 팝업이 닫힌 뒤 실행할 함수 저장
        onCloseCallback = onClose;

        // 이미지 설정
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

        // 이름
        if (clueNameText != null)
        {
            clueNameText.text = clueData.clueName;
        }

        // 설명
        if (clueDescText != null)
        {
            if (!string.IsNullOrEmpty(
                    clueData.inventoryDescription))
            {
                clueDescText.text =
                    clueData.inventoryDescription;
            }
            else if (!string.IsNullOrEmpty(
                         clueData.firstClickText))
            {
                clueDescText.text =
                    clueData.firstClickText;
            }
            else if (!string.IsNullOrEmpty(
                         clueData.secondClickText))
            {
                clueDescText.text =
                    clueData.secondClickText;
            }
            else
            {
                clueDescText.text =
                    "설명이 없습니다.";
            }
        }

        if (dialoguePanel != null)
        {
            dialogueWasActive = dialoguePanel.activeSelf;
            dialoguePanel.SetActive(false);
        }

        // 팝업 켜기
        popupPanel.SetActive(true);
        popupPanel.transform.SetAsLastSibling();

        // 열자마자 바로 닫히는 것 방지
        canClose = false;

        CancelInvoke(nameof(EnableClose));
        Invoke(nameof(EnableClose), 0.1f);
    }

    private void EnableClose()
    {
        canClose = true;
    }

    public void ClosePopup()
    {
        if (popupPanel == null ||
            !popupPanel.activeSelf)
        {
            return;
        }

        canClose = false;

        // 팝업 닫기
        popupPanel.SetActive(false);

        // 팝업 뒤에 숨겨두었던 대화창 다시 표시
        if (dialoguePanel != null && dialogueWasActive)
        {
            dialoguePanel.SetActive(true);
        }

        Action callback = onCloseCallback;
        onCloseCallback = null;

        callback?.Invoke();
    }
}