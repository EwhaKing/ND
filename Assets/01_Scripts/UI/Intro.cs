using UnityEngine;
using UnityEngine.SceneManagement;

public class Intro : MonoBehaviour
{
    [SerializeField] private Fade fade;
    [SerializeField] private GameObject settingPopup;
    [SerializeField] private GameObject collectionPopup;

    public void GameStart()
    {
        fade.FadeOut(1.0f, 1.0f, ()=>SceneManager.LoadScene("ChatScene"));
    }

    // 설정 팝업 열기
    public void OpenSettingPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(true);
        }
    }

    // 설정 팝업 닫기 (X 버튼용)
    public void CloseSettingPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(false);
        }
    }


    public void OpenCollectionPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(true);
        }
    }

    // 설정 팝업 닫기 (X 버튼용)
    public void CloseCollectionPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(false);
        }
    }





}
