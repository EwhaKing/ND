using UnityEngine;

/// <summary>
/// MiniGameNext
///
/// 담당:
/// - 미니게임 성공 후 다음 게임 진행 단계로 넘어가는 버튼/이벤트를 처리합니다.
/// - 기존처럼 직접 ChatScene을 로드하지 않고, GameProgressManager에 미니게임 클리어를 알립니다.
///
/// 사용 위치:
/// - MiniGameScene의 다음 버튼, 성공 버튼, 또는 미니게임 클리어 이벤트 오브젝트에 붙여 사용합니다.
/// - 미니게임 성공 후 OnClick 이벤트에서 LoadScene()을 호출합니다.
///
/// 연결:
/// - GameProgressManager.OnMiniGameCleared()를 호출하여 Stage2Intro 단계로 이동합니다.
/// - GameProgressManager가 ChatScene을 로드하고, DialogueSceneController가 Stage2IntroScenario를 실행합니다.
///
/// TODO:
/// - 실패 조건이 있는 미니게임이라면 OnMiniGameFailed() 같은 별도 흐름 추가 검토
/// - 함수명 LoadScene은 실제 역할과 다르므로 추후 GoNextAfterMiniGame 또는 CompleteMiniGame으로 변경 검토
/// </summary>
public class MiniGameNext : MonoBehaviour
{
    public void LoadScene()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnMiniGameCleared();
        }
        else
        {
            Debug.LogError("GameProgressManager가 없습니다.");
        }
    }
}
