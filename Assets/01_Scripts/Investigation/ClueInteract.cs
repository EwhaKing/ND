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

                string combineResultText = InventoryManager.Instance.AddItem(clueData, false);

                if (!string.IsNullOrEmpty(combineResultText))
                {
                    string[] combinedTexts = new string[] { clueData.openText, clueData.firstClickText, combineResultText };
                    PointClickDialogueManager.Instance.ShowTexts(combinedTexts, true);
                    
                    ReserveMapObjectUpdate();
                }
                else
                {
                    string[] combinedTexts = new string[] { clueData.openText, clueData.firstClickText };
                    PointClickDialogueManager.Instance.ShowTexts(combinedTexts, true);
                }
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
            string combineResultText = InventoryManager.Instance.AddItem(clueData, false); // 아이템 추가 및 합성 여부 확인

            if (!string.IsNullOrEmpty(combineResultText))
            {
                string[] combinedTexts = new string[] { clueData.firstClickText, combineResultText };
                PointClickDialogueManager.Instance.ShowTexts(combinedTexts, true);

                ReserveMapObjectUpdate();
            }
            else
            {
                PointClickDialogueManager.Instance.ShowText(clueData.firstClickText, true);
            }
            
            isFirstClickDone = true;
        }
        else
        {
            PointClickDialogueManager.Instance.ShowText(clueData.secondClickText);
        }
    }

    // 맵 오브젝트 상태 업데이트 예약
    private void ReserveMapObjectUpdate()
    {
        PointClickDialogueManager.Instance.onDialogueClosedCallback = () =>
        {
            GameObject partnerObj = InvestigationManager.Instance.GetClueObject(clueData.combineTarget);
            GameObject resultObj = InvestigationManager.Instance.GetClueObject(clueData.combineResult);

            if (partnerObj != null) partnerObj.SetActive(false);
            if (resultObj != null) resultObj.SetActive(true);

            gameObject.SetActive(false);
        };
    }
}