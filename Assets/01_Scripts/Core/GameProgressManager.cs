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
    [SerializeField] private string findRoomSceneName = "FindScene_Room";
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

        BuildFlowTable();
        LoadProgress();
    }
    
    // ───────────── 진행 흐름표 ─────────────

    /// scene    : 이 단계가 실행되는 씬
    /// next     : 성공 시 다음 단계 (None = 로비로 복귀)
    /// failNext : 실패 시 다음 단계 (None = 같은 단계 재시작)
    private struct FlowNode
    {
        public string scene;
        public GameFlowStep next;
        public GameFlowStep failNext;

        public FlowNode(string scene, GameFlowStep next, GameFlowStep failNext = GameFlowStep.None)
        {
            this.scene = scene;
            this.next = next;
            this.failNext = failNext;
        }
    }

    private readonly Dictionary<GameFlowStep, FlowNode> flowTable = new();

    /// 기획 문서 기준 진행 순서. 흐름을 바꿀 때는 여기만 수정한다.
    private void BuildFlowTable()
    {
        flowTable.Clear();

        // 챕터0 / 챕터1 프롤로그
        flowTable[GameFlowStep.PrologueDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.Chapter1PrologueDialogue);
        flowTable[GameFlowStep.Chapter1PrologueDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.InvestigationRoof);

        // 스테이지1 탐색
        flowTable[GameFlowStep.InvestigationRoof] =
            new FlowNode(findSceneName, GameFlowStep.InvestigationGround);
        flowTable[GameFlowStep.InvestigationGround] =
            new FlowNode(findSceneName, GameFlowStep.Chapter1Stage1Dialogue);

        // <Ch1-1-a> 심판장. 선택지로 분기 (심판한다 → Judgment / 더 파고든다 → Refutation)
        flowTable[GameFlowStep.Chapter1Stage1Dialogue] =
            new FlowNode(chatSceneName, GameFlowStep.Refutation);

        // 심판 후 Chapter2 예정 → 지금은 로비
        flowTable[GameFlowStep.Judgment] =
            new FlowNode(judgeSceneName, GameFlowStep.None);

        // 논파 1-1 (실패는 RefutationRunner가 씬 안에서 재도전)
        flowTable[GameFlowStep.Refutation] =
            new FlowNode(refutationSceneName, GameFlowStep.Chapter1Stage1Refutation1SuccessDialogue);

        // <Ch1-1-b> → 스마트폰 미니게임
        flowTable[GameFlowStep.Chapter1Stage1Refutation1SuccessDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.MiniGame);
        flowTable[GameFlowStep.MiniGame] =
            new FlowNode(miniGameSceneName,
                GameFlowStep.Chapter1Stage1ConclusionSDialogue,
                GameFlowStep.Chapter1Stage1ConclutionFDialogue);

        // 미니게임 성공 대사 → 스테이지2 / 실패 대사 → 미니게임 재도전
        flowTable[GameFlowStep.Chapter1Stage1ConclusionSDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.Chapter1Stage2Dialogue);
        flowTable[GameFlowStep.Chapter1Stage1ConclutionFDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.MiniGame);

        // 스테이지2: <Ch1-2-a> → 방 탐색 → 논파 2-1 → <Ch1-2-b>
        flowTable[GameFlowStep.Chapter1Stage2Dialogue] =
            new FlowNode(chatSceneName, GameFlowStep.Chapter1Stage2InvestigationRoom);
        flowTable[GameFlowStep.Chapter1Stage2InvestigationRoom] =
            new FlowNode(findSceneName, GameFlowStep.Chapter1Stage2Refutation);
        flowTable[GameFlowStep.Chapter1Stage2Refutation] =
            new FlowNode(refutationSceneName, GameFlowStep.Chapter1Stage2RefutationSuccessDialogue);

        // Stage3 미정 → 지금은 로비
        flowTable[GameFlowStep.Chapter1Stage2RefutationSuccessDialogue] =
            new FlowNode(chatSceneName, GameFlowStep.None);

        // 예전 세이브 호환용
        flowTable[GameFlowStep.Stage2Intro] =
            new FlowNode(chatSceneName, GameFlowStep.Chapter1Stage2InvestigationRoom);
    }

    /// 지정한 단계로 이동. None이면 로비로.
    public void GoToStep(GameFlowStep step)
    {
        if (step == GameFlowStep.None)
        {
            ReturnToLobby();
            return;
        }

        if (!flowTable.TryGetValue(step, out FlowNode node))
        {
            Debug.LogError($"[GameProgressManager] 흐름표에 없는 단계입니다: {step}");
            return;
        }

        Debug.Log($"[GameProgressManager] {currentStep} → {step} ({node.scene})");

        currentStep = step;
        SceneManager.LoadScene(node.scene);
    }

    /// 현재 단계 성공 → 다음 단계
    public void GoNext()
    {
        if (!flowTable.TryGetValue(currentStep, out FlowNode node))
        {
            Debug.LogError($"[GameProgressManager] 다음 단계가 정의되지 않았습니다: {currentStep}");
            return;
        }

        GoToStep(node.next);
    }

    /// 현재 단계 실패 → 실패 분기, 없으면 같은 단계 재시작
    public void GoFail()
    {
        if (flowTable.TryGetValue(currentStep, out FlowNode node) &&
            node.failNext != GameFlowStep.None)
        {
            GoToStep(node.failNext);
            return;
        }

        GoToStep(currentStep);
    }

    /// 로비로 복귀 (단서/판결 결과는 유지)
    public void ReturnToLobby()
    {
        Debug.Log("[GameProgressManager] 로비로 복귀");

        currentStep = GameFlowStep.None;
        pendingResumeStepIndex = -1;
        SceneManager.LoadScene(lobbySceneName);
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

    public void OnDialogueFinished()
    {
        GoNext();
    }

    public void OnInvestigationFinished()
    {
        GoNext();
    }

    // 실패는 RefutationRunner가 씬 안에서 재도전하므로, 여기로 실패가 오면 같은 논파를 다시 시작
    public void OnRefutationFinished(bool isSuccess)
    {
        if (isSuccess) GoNext();
        else GoFail();
    }

    // 심판 종료 → Chapter2 연결 전까지는 로비
    public void OnJudgmentFinished(JudgmentVerdict verdict)
    {
        FinalVerdict = verdict;
        Debug.Log($"최종 심판 결과: {verdict}");
        GoNext();
    }

    // MiniGameNext(LoadScene 버튼)에서 호출 = 미니게임 성공과 동일
    public void OnMiniGameCleared()
    {
        MiniGame1Success();
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
        // 다음 단계도 대화 씬이면 씬을 다시 로드하지 않고 단계만 바꾼다.
        if (!flowTable.TryGetValue(currentStep, out FlowNode node)) return false;
        if (node.next == GameFlowStep.None) return false;
        if (!flowTable.TryGetValue(node.next, out FlowNode nextNode)) return false;
        if (node.scene != chatSceneName || nextNode.scene != chatSceneName) return false;

        currentStep = node.next;
        return true;
    }

    // MiniGameScene 버튼에서 호출 (단독 테스트 때도 동작하도록 목적지를 직접 지정)
    public void MiniGame1Success()
    {
        GoToStep(GameFlowStep.Chapter1Stage1ConclusionSDialogue);
    }

    public void MiniGame1Fail()
    {
        GoToStep(GameFlowStep.Chapter1Stage1ConclutionFDialogue);
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
        return flowTable.TryGetValue(step, out FlowNode node) ? node.scene : chatSceneName;
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
            case GameFlowStep.Chapter1Stage2Dialogue:
            case GameFlowStep.Chapter1Stage2RefutationSuccessDialogue:
                return "챕터 1 - 스테이지 2";
            case GameFlowStep.Chapter1Stage2InvestigationRoom:
                return "챕터 1 - 유정의 방";
            case GameFlowStep.Chapter1Stage2Refutation:
                return "챕터 1 - 스테이지 2 논파";
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


// ⚠ Unity는 enum을 숫자로 저장한다. 기존 번호는 바꾸지 말고, 새 단계는 맨 뒤에 새 번호로 추가할 것.
public enum GameFlowStep
{
    None = 0,

    PrologueDialogue = 1,                         // 챕터0 프롤로그
    Chapter1PrologueDialogue = 2,                 // 챕터1 프롤로그
    Chapter1Stage1Dialogue = 3,                   // <Ch1-1-a> 심판장 + 선택지
    Chapter1Stage1Refutation1SuccessDialogue = 4, // <Ch1-1-b>
    Chapter1Stage1ConclusionSDialogue = 5,        // 스마트폰 최종 논파 성공
    Chapter1Stage1ConclutionFDialogue = 6,        // 논파 실패 공통 스크립트
    Chapter1Stage2Dialogue = 7,                   // <Ch1-2-a>
    Chapter1Stage2RefutationSuccessDialogue = 8,  // <Ch1-2-b>

    InvestigationRoof = 9,
    InvestigationGround = 10,
    Refutation = 11,                              // 논파 1-1
    Judgment = 12,
    MiniGame = 13,                                // 스마트폰 미니게임
    Stage2Intro = 14,                             // (사용 안 함, 호환용)

    // ── 새로 추가 ──
    Chapter1Stage2InvestigationRoom = 15,         // 유정의 방 탐색
    Chapter1Stage2Refutation = 16,                // 논파 2-1
}