using System.Collections;
using UnityEngine;

/// <summary>
/// ChatScene에서 현재 게임 진행 단계에 맞는
/// ScenarioData를 선택하고 실행합니다.
///
/// 연속된 대화 Scenario의 경우 Scene을 다시 로드하지 않고
/// Fade 연출 후 다음 ScenarioData를 실행합니다.
/// </summary>
public class DialogueSceneController : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private ScenarioRunner scenarioRunner;
    [SerializeField] private Fade fadeController;
    [SerializeField] private StandingController standingController;

    [Header("Scenario Data")]
    [SerializeField] private ScenarioData prologueScenario;
    [SerializeField] private ScenarioData chapter1Stage1Scenario;
    [SerializeField] private ScenarioData chapter1Stage2Scenario;

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float blackHoldDuration = 0.3f;

    private void Start()
    {
        Debug.Log("[DialogueSceneController] Start 실행됨");

        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("GameProgressManager가 없습니다.");
            return;
        }

        if (scenarioRunner == null)
        {
            Debug.LogError("ScenarioRunner가 연결되지 않았습니다.");
            return;
        }

        ScenarioData scenarioToPlay = GetScenarioByCurrentStep();

        if (scenarioToPlay == null)
        {
            Debug.LogError(
                $"현재 단계에 맞는 ScenarioData가 없습니다: " +
                $"{GameProgressManager.Instance.CurrentStep}"
            );
            return;
        }

        Debug.Log(
            $"[DialogueSceneController] 실행 시나리오 = " +
            $"{scenarioToPlay.scenarioId} / " +
            $"CurrentStep = {GameProgressManager.Instance.CurrentStep}"
        );

        scenarioRunner.RunScenario(
            scenarioToPlay,
            OnScenarioFinished
        );
    }

    /// <summary>
    /// 현재 GameFlowStep에 맞는 Scenario를 실행합니다.
    /// </summary>
    private void PlayCurrentScenario()
    {
        ScenarioData scenarioToPlay =
            GetScenarioByCurrentStep();

        if (scenarioToPlay == null)
        {
            Debug.LogError(
                $"현재 단계에 맞는 ScenarioData가 없습니다: " +
                $"{GameProgressManager.Instance.CurrentStep}"
            );
            return;
        }

        scenarioRunner.RunScenario(
            scenarioToPlay,
            OnScenarioFinished
        );
    }

    /// <summary>
    /// 현재 Scenario가 끝났을 때 호출됩니다.
    /// </summary>
    private void OnScenarioFinished()
    {
        Debug.Log(
            $"[DialogueSceneController] 종료 콜백 / " +
            $"CurrentStep = {GameProgressManager.Instance.CurrentStep}"
        );

        bool hasNextDialogue =
            GameProgressManager.Instance.TryAdvanceDialogueStep();

        Debug.Log(
            $"[DialogueSceneController] 다음 대화 = {hasNextDialogue} / " +
            $"변경된 Step = {GameProgressManager.Instance.CurrentStep}"
        );

        if (hasNextDialogue)
        {
            // Fade 테스트를 위해 일단 바로 다음 Scenario 실행
            PlayCurrentScenario();
            return;
        }

        GameProgressManager.Instance.OnDialogueFinished();
    }

    /// <summary>
    /// Fade로 화면을 가린 뒤
    /// 같은 ChatScene에서 다음 Scenario를 실행합니다.
    /// </summary>
    private IEnumerator TransitionToNextScenario()
    {
        if (fadeController == null)
        {
            Debug.LogError(
                "FadeController가 연결되지 않았습니다."
            );

            PlayCurrentScenario();
            yield break;
        }

        // 1. 화면 검게
        bool fadeOutComplete = false;

        fadeController.FadeOut(
            fadeDuration,
            0f,
            () => fadeOutComplete = true
        );

        yield return new WaitUntil(
            () => fadeOutComplete
        );

        // 2. 이전 스탠딩 정리
        if (standingController != null)
        {
            standingController.Hide();
        }

        // 3. 검은 화면 잠시 유지
        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                blackHoldDuration
            );
        }

        // 4. 다음 Scenario 실행
        PlayCurrentScenario();

        // 5. 검은 화면 걷기
        bool fadeInComplete = false;

        fadeController.FadeIn(
            fadeDuration,
            0f,
            () => fadeInComplete = true
        );

        yield return new WaitUntil(
            () => fadeInComplete
        );
    }

    private ScenarioData GetScenarioByCurrentStep()
    {
        switch (GameProgressManager.Instance.CurrentStep)
        {
            case GameFlowStep.PrologueDialogue:
                return prologueScenario;

            case GameFlowStep.Chapter1Stage1Dialogue:
                return chapter1Stage1Scenario;

            case GameFlowStep.Chapter1Stage2Dialogue:
                return chapter1Stage2Scenario;

            default:
                return null;
        }
    }
}