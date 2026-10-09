using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Intro
///
/// 담당:
/// - 타이틀/인트로 화면의 버튼 동작을 처리
/// - 게임 시작 버튼 클릭 시 FadeOut 연출 후 GameProgressManager를 통해 새 게임을 시작
/// - 설정 팝업과 도감/컬렉션 팝업을 열고 닫는 UI 기능을 담당
/// - 게임 종료 버튼 기능을 처리
///
/// 사용 위치:
/// - LobbyScene 또는 타이틀 씬의 메뉴 버튼 관리 오브젝트에 붙여 사용
/// - New Game, Settings, Collection, Quit 버튼의 OnClick 이벤트와 연결
///
/// 연결:
/// - Fade 컴포넌트를 통해 씬 전환 전 페이드 아웃 연출을 실행
/// - GameProgressManager.StartNewGame()을 호출하여 첫 진행 단계인 PrologueDialogue로 이동
/// - settingPopup, collectionPopup 오브젝트를 활성/비활성 처리
///
/// TODO:
/// - Continue 버튼과 SaveLoad.Load 기능 연결 필요
/// - GameProgressManager.Instance가 null일 때의 예외 화면 또는 안전 처리 추가 검토
/// </summary>
public class Intro : MonoBehaviour
{
    [SerializeField] private Fade fade;
    [SerializeField] private GameObject settingPopup;
    [SerializeField] private GameObject collectionPopup;

    [SerializeField] private GameObject saveLoadPanel; // 씬의 SaveLoad 패널 오브젝트
    [SerializeField] private SaveLoad saveLoadUI;       // SaveLoad 스크립트 컴포넌트

    // [이어하기] 버튼 클릭 시 호출할 메서드
    public void GameContinue()
    {
        if (saveLoadPanel != null && saveLoadUI != null)
        {
            // 1. 패널 활성화
            saveLoadPanel.SetActive(true);

            // 2. Load 모드로 UI 초기화 및 데이터 갱신
            saveLoadUI.Initialize(SaveLoadType.Load);
        }
        else
        {
            Debug.LogWarning("SaveLoadPanel 또는 SaveLoad 컴포넌트가 연결되지 않았습니다.");
        }
    }



    public void GameStart()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("GameProgressManager가 없습니다.");
            return;
        }

        fade.FadeOut(
            1.0f,
            1.0f,
            () => GameProgressManager.Instance.StartNewGame()
        );
    }

    public void OpenSettingPopup()
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(true);
        }
    }

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

    public void CloseCollectionPopup()
    {
        if (collectionPopup != null)
        {
            collectionPopup.SetActive(false);
        }
    }

    public void GameExit()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}