using UnityEngine;

public class ClueInteract : MonoBehaviour
{
    public ClueData clueData;

    private bool isFirstClickDone = false;
    private bool isUnlocked = false;

    public void OnClickAction()
    {
        if (clueData == null)
        {
            return;
        }

        // 조건부 상호작용
        if (clueData.requiresItem &&
            clueData.requiredClueData != null &&
            !isUnlocked)
        {
            // 필요한 단서를 이미 가지고 있음
            if (InventoryManager.Instance.HasItem(
                    clueData.requiredClueData.clueID))
            {
                isUnlocked = true;
                isFirstClickDone = true;

                InventoryManager.Instance.AddItem(
                    clueData,
                    false
                );

                // 팝업 → 닫기 → 대화
                ShowPopupThenDialogue(
                    () =>
                    {
                        string[] combinedTexts =
                        {
                            clueData.openText,
                            clueData.firstClickText
                        };

                        PointClickDialogueManager.Instance.ShowTexts(
                            combinedTexts,
                            true
                        );
                    }
                );
            }
            else
            {
                // 조건을 충족하지 못했을 때는
                // 단서를 획득하지 않으므로 팝업 없이 대사만
                PointClickDialogueManager.Instance.ShowText(
                    clueData.lockedText
                );
            }

            return;
        }

        // 일반 단서 첫 조사
        if (!isFirstClickDone)
        {
            InventoryManager.Instance.AddItem(
                clueData,
                false
            );

            isFirstClickDone = true;

            // 팝업 → 닫기 → 대화
            ShowPopupThenDialogue(
                () =>
                {
                    PointClickDialogueManager.Instance.ShowText(
                        clueData.firstClickText,
                        true
                    );
                }
            );
        }
        else
        {
            // 이미 조사한 단서는 팝업 없이 재조사 대사만
            PointClickDialogueManager.Instance.ShowText(
                clueData.secondClickText
            );
        }
    }

    private void ShowPopupThenDialogue(
        System.Action dialogueAction)
    {
        // 팝업이 있다면 팝업을 먼저 표시
        if (ClueDetailPopup.Instance != null)
        {
            ClueDetailPopup.Instance.ShowPopup(
                clueData,
                dialogueAction
            );
        }
        else
        {
            // 혹시 팝업 Manager가 없다면
            // 게임 진행이 막히지 않도록 바로 대화 실행
            dialogueAction?.Invoke();
        }
    }
}