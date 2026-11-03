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
    // 씬 이동 후 적용할 임시 세이브 데이터
    private SaveGameData pendingSaveData = null;

    // 로드 직후 ChatScene에서 이어서 시작할 Step 번호 (-1 = 없음)
    // DialogueSceneController.Start()가 한 번 읽고 소모한다.
    private int pendingResumeStepIndex = -1;
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

    // (이제 로드에서는 사용하지 않음: 시나리오는 currentStep 으로 결정한다. 기존 Inspector 값 보존용)
    [SerializeField] private List<ScenarioData> scenarioList;

    [Header("Clue Database (세이브 복원용)")]
    [Tooltip("게임에 나오는 모든 ClueData(조합 결과 단서 포함)를 넣어주세요. 비워두면 Resources 폴더에서 찾습니다.")]
    [SerializeField] private List<ClueData> clueDatabase = new();
    private bool clueDatabaseFromResourcesLoaded = false;
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

    /// <summary>
    /// 세이브 데이터를 들고 목적지 씬으로 이동한다.
    /// 같은 씬(예: FindScene 옥상 -> 땅)이라도 항상 씬을 다시 로드해서
    /// 각 씬의 컨트롤러가 Start()에서 새 CurrentStep 을 읽도록 한다.
    /// </summary>
    public void LoadGameAndChangeScene(SaveGameData data, string playSceneName = "ChatScene")
    {
        pendingSaveData = data;

        // 중복 등록 방지 후 등록
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        SceneManager.LoadScene(playSceneName);
    }

    // 씬 로드가 완료되었을 때(Awake/OnEnable 이후, Start 이전) 실행되는 콜백
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (pendingSaveData != null)
        {
            SaveGameData data = pendingSaveData;
            pendingSaveData = null;
            ApplySaveData(data);
        }
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
        pendingResumeStepIndex = -1;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
        }

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

        if (!acquiredClues.Exists(c => c != null && c.clueID == clueData.clueID))
        {
            acquiredClues.Add(clueData);
        }
    }

    /// <summary>
    /// 해당 clueID 의 단서를 이미 획득했는지 (세이브 로드 후 조사 오브젝트 복원에 사용)
    /// </summary>
    public bool HasClue(string clueID)
    {
        if (string.IsNullOrWhiteSpace(clueID)) return false;
        return acquiredClues.Exists(c => c != null && c.clueID == clueID);
    }

    /// <summary>
    /// 시나리오 선택지(LoadScene)처럼 GameProgressManager 를 거치지 않고 씬을 직접 이동할 때,
    /// 이동할 씬에 맞게 진행 단계(currentStep)를 맞춰준다.
    /// (안 맞추면 논파 씬이 "Chapter1Stage1Dialogue"로 시작해서 데이터를 못 찾는다)
    /// </summary>
    public void SetStepByScene(string sceneName)
    {
        if (sceneName == refutationSceneName) currentStep = GameFlowStep.Refutation;
        else if (sceneName == judgeSceneName) currentStep = GameFlowStep.Judgment;
        else if (sceneName == miniGameSceneName) currentStep = GameFlowStep.MiniGame;
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


    //SaveLoad관련 코드
    // 현재 게임 상태를 SaveGameData 객체로 만들어 반환하는 함수
    public SaveGameData CreateSaveData()
    {
        SaveGameData data = new SaveGameData();

        // 1. 현재 메인 진행 단계 저장
        data.currentProgressStep = this.CurrentStep;

        // 저장 시점의 현재 활성 씬 이름 기록
        data.targetSceneName = SceneManager.GetActiveScene().name;

        // 2. 현재 대화/연출 위치 저장 
        ScenarioRunner runner = Object.FindAnyObjectByType<ScenarioRunner>();
        if (runner != null)
        {
            data.scenarioStepIndex = runner.GetCurrentStepIndex();

            if (runner.CurrentScenarioData != null)
            {
                data.scenarioId = runner.CurrentScenarioData.scenarioId;
                // ※ ScenarioData 내 변수명이 scenarioId가 아니라 name이면 runner.CurrentScenarioData.name으로 작성
            }
        }
        else
        {
            data.scenarioStepIndex = 0;
        }

        // 3. 획득한 단서 목록 저장 (clueID 기준)
        foreach (var clue in acquiredClues)
        {
            if (clue != null && !string.IsNullOrWhiteSpace(clue.clueID))
            {
                data.acquiredClues.Add(clue.clueID);
            }
        }

        // 4. 최종 판결 상태 저장 (값이 존재하는 경우에만 저장)
        if (this.FinalVerdict.HasValue)
        {
            data.finalVerdict = this.FinalVerdict.Value;
        }

        // 5. 저장 시각 및 데이터 존재 여부
        data.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.hasData = true;

        Debug.Log($"[Save] step={data.currentProgressStep} / scene={data.targetSceneName} / " +
                  $"stepIndex={data.scenarioStepIndex} / clues={data.acquiredClues.Count}");

        return data;
    }


    // CurrentStep을 통해 이동해야 할 적절한 Target Scene 이름을 찾아주는 함수
    public string GetTargetSceneByStep(GameFlowStep step)
    {
        switch (step)
        {
            case GameFlowStep.PrologueDialogue:
            case GameFlowStep.Chapter1PrologueDialogue:
            case GameFlowStep.Chapter1Stage1Dialogue:
            case GameFlowStep.Chapter1Stage1Refutation1SuccessDialogue:
            case GameFlowStep.Chapter1Stage1ConclusionSDialogue:
            case GameFlowStep.Chapter1Stage1ConclutionFDialogue:
            case GameFlowStep.Chapter1Stage2Dialogue:
            case GameFlowStep.Chapter1Stage2RefutationSuccessDialogue:
            case GameFlowStep.Stage2Intro:
                return chatSceneName; // "ChatScene"

            case GameFlowStep.InvestigationRoof:
            case GameFlowStep.InvestigationGround:
                return findSceneName; // "FindScene"

            case GameFlowStep.Refutation:
                return refutationSceneName; // "RefutationScene"

            case GameFlowStep.Judgment:
                return judgeSceneName; // "JudgeScene"

            case GameFlowStep.MiniGame:
                return miniGameSceneName; // "MiniGameScene"

            default:
                return chatSceneName;
        }
    }



    // ClueID(또는 예전 세이브의 clueName)로 ClueData 를 찾는다.
    private ClueData FindClueByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        ClueData found = clueDatabase.Find(c => c != null && (c.clueID == key || c.clueName == key));
        if (found != null) return found;

        // Inspector 에 등록 안 된 단서는 Resources 폴더에서 한 번만 불러와 보충
        if (!clueDatabaseFromResourcesLoaded)
        {
            clueDatabaseFromResourcesLoaded = true;
            foreach (ClueData c in Resources.LoadAll<ClueData>(""))
            {
                if (c != null && !clueDatabase.Contains(c)) clueDatabase.Add(c);
            }
            return clueDatabase.Find(c => c != null && (c.clueID == key || c.clueName == key));
        }

        return null;
    }

    /// <summary>
    /// 저장 화면에 표시할 챕터 문구. 현재 진행 단계(currentStep)로 결정한다.
    /// (조사 씬에는 ScenarioRunner 가 없으므로 시나리오 이름으로는 알 수 없음)
    /// </summary>
    public string GetChapterLabel()
    {
        switch (currentStep)
        {
            case GameFlowStep.PrologueDialogue:
                return "프롤로그";
            case GameFlowStep.InvestigationRoof:
                return "챕터 1 - 옥상";
            case GameFlowStep.InvestigationGround:
                return "챕터 1 - 땅";
            case GameFlowStep.Refutation:
                return "챕터 1 - 논파";
            case GameFlowStep.Judgment:
                return "챕터 1 - 심판";
            case GameFlowStep.MiniGame:
                return "챕터 1 - 미니게임";
            case GameFlowStep.None:
                return "프롤로그";
            default:
                return "챕터 1";
        }
    }

    /// <summary>
    /// 대화 씬에서 이어서 시작할 Step 번호를 한 번만 돌려준다. 없으면 0.
    /// </summary>
    public int ConsumeResumeStepIndex()
    {
        int index = Mathf.Max(0, pendingResumeStepIndex);
        pendingResumeStepIndex = -1;
        return index;
    }

    // SaveGameData 데이터를 받아서 게임 상태를 복원하는 함수
    // (씬 로드 직후, 각 오브젝트의 Start() 이전에 호출된다)
    public void ApplySaveData(SaveGameData data)
    {
        if (data == null) return;

        // 1. 진행 단계 복원
        currentStep = data.currentProgressStep;
        FinalVerdict = data.finalVerdict;

        // 2. 단서 복원: GameProgressManager 목록을 먼저 갱신
        acquiredClues.Clear();
        foreach (string key in data.acquiredClues)
        {
            ClueData clue = FindClueByKey(key);
            if (clue == null)
            {
                Debug.LogWarning($"[Load] 단서를 찾을 수 없습니다: {key} (clueDatabase에 ClueData 등록 필요)");
                continue;
            }

            if (!acquiredClues.Contains(clue)) acquiredClues.Add(clue);
        }

        // 2-1. 이미 살아있는 InventoryManager 가 있다면 내용을 교체 (없으면 Start()에서 스스로 동기화)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReplaceAll(acquiredClues);
        }

        // 3. 대화 위치: 씬 로드 후 DialogueSceneController.Start()가 읽어서 이어서 재생한다.
        //    (여기서 ScenarioRunner 를 직접 건드리면 곧바로 실행되는 Start()에 덮어써진다)
        bool isDialogueStep = GetTargetSceneByStep(currentStep) == chatSceneName;
        pendingResumeStepIndex = isDialogueStep ? Mathf.Max(0, data.scenarioStepIndex) : -1;

        Debug.Log($"[LoadSuccess] step={currentStep} / 단서 {acquiredClues.Count}개 / resumeIndex={pendingResumeStepIndex}");
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