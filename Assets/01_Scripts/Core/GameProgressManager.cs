using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameProgressManager
///
/// 담당:
/// - 게임 전체 진행 단계와 씬 전환 흐름을 관리
/// - 현재 플레이가 프롤로그, 챕터1 대화, 조사, 논파, 심판, 미니게임 중 어느 단계인지 저장
/// - 각 씬이 끝났을 때 다음 씬을 결정하고 로드
/// - 조사 파트에서 획득한 단서를 씬 이동 이후에도 유지
/// - 논파 성공/실패, 심판 결과, 회차 수를 저장
/// - 2회차 이상일 때 스킵 가능 여부를 제공
///
/// 사용 위치:
/// - LobbyScene의 Managers 오브젝트에 붙여 사용
/// - DontDestroyOnLoad로 씬이 바뀌어도 유지
/// - Intro, DialogueSceneController, InvestigationManager, RefutationManager, JudgmentFlowManager, MiniGameNext에서 호출
///
/// 연결:
/// - Intro에서 StartNewGame()을 호출해 첫 대화 씬으로 이동
/// - DialogueSceneController에서 대화 종료 시 OnDialogueFinished()를 호출
/// - InvestigationManager에서 조사 종료 시 OnInvestigationFinished()를 호출
/// - RefutationManager에서 논파 결과에 따라 OnRefutationFinished()를 호출
/// - JudgmentFlowManager에서 AND/END 선택 결과를 OnJudgmentFinished()로 전달
/// - MiniGameNext에서 미니게임 성공 시 OnMiniGameCleared()를 호출
/// - InventoryManager에서 획득 단서를 RegisterClue()로 등록
///
/// TODO:
/// - 저장/로드 시스템과 연결하여 currentStep, 획득 단서, 심판 결과를 저장하도록 확장 필요
/// - Stage2 이후 흐름 추가 필요
/// - 여러 챕터를 지원할 경우 ChapterData 또는 StageData 기반 구조로 확장 검토
/// - 현재는 발표용으로 씬 이름을 문자열로 관리하므로, 추후 상수/ScriptableObject 기반 관리 검토
/// </summary>
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private const string PlayCountKey = "PlayCount";

    [Header("Debug")]
    [SerializeField] private bool forceSkipForTesting = false;

    [Header("Progress")]
    [SerializeField] private int playCount = 1;
    [SerializeField] private GameFlowStep currentStep = GameFlowStep.None;

    [Header("Scene Names")]
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string chatSceneName = "ChatScene";
    [SerializeField] private string findSceneName = "FindScene";
    [SerializeField] private string refutationSceneName = "RefutationScene";
    [SerializeField] private string judgeSceneName = "JudgeScene";
    [SerializeField] private string miniGameSceneName = "MiniGameScene";

    private readonly List<ClueData> acquiredClues = new();

    public int PlayCount => playCount;
    public bool CanSkip => forceSkipForTesting || playCount >= 2;
    public GameFlowStep CurrentStep => currentStep;
    public IReadOnlyList<ClueData> AcquiredClues => acquiredClues;

    public JudgmentVerdict? FinalVerdict { get; private set; }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
    }

    private void LoadProgress()
    {
        playCount = PlayerPrefs.GetInt(
            PlayCountKey,
            1
        );
    }

    public void StartNewGame()
    {
        acquiredClues.Clear();
        FinalVerdict = null;

        currentStep = GameFlowStep.PrologueDialogue;

        SceneManager.LoadScene(chatSceneName);
    }

    public void GoNext()
    {
        switch (currentStep)
        {
            case GameFlowStep.PrologueDialogue:
                currentStep = GameFlowStep.Chapter1PrologueDialogue;
                SceneManager.LoadScene(chatSceneName);
                break;

            case GameFlowStep.Chapter1PrologueDialogue:
                currentStep = GameFlowStep.InvestigationRoof;
                SceneManager.LoadScene(findSceneName);
                break;

            case GameFlowStep.InvestigationRoof:
                currentStep = GameFlowStep.InvestigationGround;
                SceneManager.LoadScene(findSceneName);
                break;

            case GameFlowStep.InvestigationGround:
                currentStep = GameFlowStep.Chapter1Stage1Dialogue;

                SceneManager.LoadScene(chatSceneName);
                break;

            case GameFlowStep.Chapter1Stage1Dialogue:
                currentStep = GameFlowStep.Refutation;

                SceneManager.LoadScene(refutationSceneName);
                break;

            case GameFlowStep.Chapter1Stage1Refutation1SuccessDialogue:
                currentStep = GameFlowStep.MiniGame;

                SceneManager.LoadScene(miniGameSceneName);
                break;

            case GameFlowStep.Chapter1Stage1ConclusionSDialogue:
                currentStep = GameFlowStep.Chapter1Stage2Dialogue;
                SceneManager.LoadScene(chatSceneName);
                break;

                // 나머지...
        }
    }

    public void OnDialogueFinished()
    {
        GoNext();
    }

    public void OnInvestigationFinished()
    {
        GoNext();
    }

    public void OnRefutationFinished(bool isSuccess)
    {
        if (isSuccess)
        {
            currentStep = GameFlowStep.Chapter1Stage1Refutation1SuccessDialogue;
            SceneManager.LoadScene(chatSceneName);
        }
        else
        {
            currentStep = GameFlowStep.Judgment;
            SceneManager.LoadScene(judgeSceneName);
        }
    }

    public void OnJudgmentFinished(JudgmentVerdict verdict)
    {
        FinalVerdict = verdict;

        Debug.Log($"최종 심판 결과: {verdict}");

        currentStep = GameFlowStep.Stage2Intro;
        SceneManager.LoadScene(chatSceneName);
    } //스테이지1 마지막에 심판이 없는데 왜 심판에서 스테이지2로 이어지는가?

    public void OnMiniGameCleared()
    {
        currentStep = GameFlowStep.Stage2Intro;
        SceneManager.LoadScene(chatSceneName);
    }

    public void RegisterClue(ClueData clueData)
    {
        if (clueData == null)
        {
            return;
        }

        if (!acquiredClues.Contains(clueData))
        {
            acquiredClues.Add(clueData);
        }
    }

    public void CompletePlaythrough()
    {
        playCount++;

        PlayerPrefs.SetInt(
            PlayCountKey,
            playCount
        );

        PlayerPrefs.Save();

        Debug.Log($"회차 증가: {playCount}회차");
    }

    // 테스트용 PlayCount 감소
    public void DiscountPlay()
    {
        playCount--;

        PlayerPrefs.SetInt(
            PlayCountKey,
            playCount
        );

        PlayerPrefs.Save();

        Debug.Log($"회차 감소: {playCount}회차");
    }

    /// <summary>
    /// ChatScene을 다시 로드하지 않고
    /// 다음 대화 단계로 진행합니다.
    /// 연속된 대화 Scenario 전환에 사용합니다.
    /// </summary>
    public bool TryAdvanceDialogueStep()
    {
        switch (currentStep)
        {
            case GameFlowStep.PrologueDialogue:
                currentStep = GameFlowStep.Chapter1PrologueDialogue;
                return true;

            case GameFlowStep.Chapter1Stage1ConclusionSDialogue:
                currentStep = GameFlowStep.Chapter1Stage2Dialogue;
                return true;
            /*case GameFlowStep.Chapter1PrologueDialogue:
                currentStep =
                    GameFlowStep.InvestigationRoof;
                return true;*/

            // Stage2(->챕터1프롤로그) 다음은 조사씬이므로
            // 여기서 CurrentStep을 바꾸지 않는다.
            default:
                return false;
        }
    }

    public void MiniGame1Success()
    {
        currentStep = GameFlowStep.Chapter1Stage1ConclusionSDialogue;
        SceneManager.LoadScene(chatSceneName);
    }

    public void MiniGame1Fail()
    {
        currentStep = GameFlowStep.Chapter1Stage1ConclutionFDialogue;
        SceneManager.LoadScene(chatSceneName);
    }

}


public enum GameFlowStep
{
    None,

    PrologueDialogue,
    Chapter1PrologueDialogue,
    Chapter1Stage1Dialogue, 
    Chapter1Stage1Refutation1SuccessDialogue,
    Chapter1Stage1ConclusionSDialogue,
    Chapter1Stage1ConclutionFDialogue,

    Chapter1Stage2Dialogue,
    Chapter1Stage2RefutationSuccessDialogue,

    InvestigationRoof,
    InvestigationGround,
    //스테이지 증가함에따라 논파, 심판, 미니게임도 늘어남
    Refutation,
    Judgment,
    MiniGame,
    Stage2Intro
}