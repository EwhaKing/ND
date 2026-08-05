using UnityEngine;
using UnityEngine.UI;

public class InventoryManagerUI1 : MonoBehaviour
{

private void Start()
    {
        CloseWindow();
    }

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        InventoryUI.Instance.HideDescription();
        
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);
    }

}