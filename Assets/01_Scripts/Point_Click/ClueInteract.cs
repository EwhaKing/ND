using UnityEngine;

public class ClueInteract : MonoBehaviour
{
    public ClueData clueData;
    private bool isFirstClickDone = false;
    
    private bool isUnlocked = false; 

    public void OnClickAction()
    {
        if (clueData == null) return;

        // 조건부 상호작용 처리 (선행 아이템 확인, 조사 대사 출력)
        if (clueData.requiresItem && clueData.requiredClueData != null && !isUnlocked)
        {
            // 선행 아이템 확인
            if (InventoryManager.Instance.HasItem(clueData.requiredClueData.clueID))
            {
                isUnlocked = true;
                isFirstClickDone = true;

                InventoryManager.Instance.AddItem(clueData, false);

                string[] combinedTexts = new string[] { clueData.openText, clueData.firstClickText };
                PointClickDialogueManager.Instance.ShowTexts(combinedTexts, true);
            }
            else
            {
                PointClickDialogueManager.Instance.ShowText(clueData.lockedText);
            }
            return;
        }

        // 일반 상호작용 처리 (조건부 상호작용이 없거나 이미 해제된 경우)
        if (!isFirstClickDone)
        {
            InventoryManager.Instance.AddItem(clueData, false);
            PointClickDialogueManager.Instance.ShowText(clueData.firstClickText, true);
            isFirstClickDone = true;
        }
        else
        {
            PointClickDialogueManager.Instance.ShowText(clueData.secondClickText);
        }
    }
}