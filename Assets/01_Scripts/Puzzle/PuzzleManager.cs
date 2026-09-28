using UnityEngine;
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("슬롯 8개 등록")]
    public PhotoSlot[] slots;

    [Header("사진 확대 미리보기 UI")]
    public GameObject photoPreviewPanel;
    public Image previewImage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (photoPreviewPanel != null)
            photoPreviewPanel.SetActive(false);
    }

    // 사진 확대 팝업 열기
    public void ShowPhotoPreview(Sprite photoSprite, Color photoColor)
    {
        if (photoPreviewPanel != null && previewImage != null)
        {
            previewImage.sprite = photoSprite;

            if (photoSprite == null)
            {
                previewImage.color = photoColor;
            }
            else
            {
                previewImage.color = Color.white;
            }

            photoPreviewPanel.SetActive(true);
            photoPreviewPanel.transform.SetAsLastSibling(); // UI 최상단 표시
        }
    }

    // 사진 확대 팝업 닫기
    public void ClosePhotoPreview()
    {
        if (photoPreviewPanel != null && photoPreviewPanel.activeSelf)
        {
            photoPreviewPanel.SetActive(false);
        }
    }

    // 미리보기 패널이 열려 있는지 여부 반환
    public bool IsPreviewActive()
    {
        return photoPreviewPanel != null && photoPreviewPanel.activeSelf;
    }

    public void CheckPuzzleComplete()
    {
        int correctCount = 0;

        foreach (PhotoSlot slot in slots)
        {
            if (slot != null && slot.transform.childCount > 0)
            {
                PhotoItem photo = slot.transform.GetChild(0).GetComponent<PhotoItem>();
                if (photo != null && photo.photoID == slot.slotID)
                {
                    correctCount++;
                }
            }
        }

        if (correctCount == 8)
        {
            Debug.Log("퍼즐 성공! 8개의 기억 조각이 시간순으로 올바르게 배열되었습니다.");
            OnPuzzleSolved();
        }
    }

    private void OnPuzzleSolved()
    {
        // 추후 타임라인 연출 지점
    }
}