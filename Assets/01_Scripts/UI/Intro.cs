using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Intro
///
/// 담당:
/// - 타이틀/인트로 화면의 버튼 동작을 처리
/// - 게임 시작 버튼 클릭 시 FadeOut 연출 후 지정된 씬으로 이동
/// - 설정 팝업과 도감/컬렉션 팝업을 열고 닫는 UI 기능을 담당
///
/// 사용 위치:
/// - 타이틀 씬 또는 로비 씬의 메뉴 버튼 관리 오브젝트에 부착
/// - New Game, Settings, Collection 버튼의 OnClick 이벤트와 연결
///
/// 연결:
/// - Fade 컴포넌트를 통해 씬 전환 전 페이드 아웃 연출을 실행
/// - SceneManager를 통해 게임 시작 씬으로 이동
/// - settingPopup, collectionPopup 오브젝트를 활성/비활성 처리
/// 
/// TODO:
/// - GameStart()에서 이동하는 씬 이름을 ChatScene 하드코딩 대신 상수 또는 GameFlowManager로 관리 검토
/// - Continue 버튼과 SaveLoad.Load 기능 연결 필요
/// </summary>

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
        if (collectionPopup != null)
        {
            collectionPopup.SetActive(true);
        }
    }

    // 도감 팝업 닫기 (X 버튼용)
    public void CloseCollectionPopup()
    {
        if (collectionPopup != null)
        {
            collectionPopup.SetActive(false);
        }
    }

    /// 게임종료. 전처리기를 이용해 에디터 아닐때 종료.
    public void GameExit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}
