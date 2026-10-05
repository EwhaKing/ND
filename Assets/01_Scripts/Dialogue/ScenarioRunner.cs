using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ScenarioRunner
///
/// 담당:
/// - ScenarioData에 등록된 Step들을 순서대로 실행합니다.
/// - Dialogue, Wait, StandingShow, StandingHide, Choice, CGShow, CGHide 타입의 시나리오 진행을 처리합니다.
/// - Dialogue Step에서는 DialogueDatabase에서 dialogueId에 맞는 대사를 가져와 ChatDialogueManager로 출력합니다.
/// - Choice Step에서는 ChoiceController를 통해 선택지를 출력하고, 선택 결과에 따라 다음 진행을 처리합니다.
/// - 선택지 이후 ReactionStep을 실행하여 짧은 대사, 대기 연출 등을 처리합니다.
/// - CGShow / CGHide Step을 통해 CG 이미지를 표시하거나 숨깁니다.
/// - 시나리오가 끝났을 때 외부 콜백을 호출하여 다음 게임 흐름으로 넘어갈 수 있게 합니다.
///
/// 사용 위치:
/// - ChatScene의 ScenarioRunner 오브젝트에 붙여 사용합니다.
/// - DialogueSceneController가 현재 진행 단계에 맞는 ScenarioData를 넘겨 실행할 때 사용합니다.
/// - 단독 테스트가 필요할 경우 playOnStart를 true로 두고 Inspector의 scenarioData를 실행할 수 있습니다.
///
/// 연결:
/// - ScenarioData의 steps를 읽어 시나리오 흐름을 실행합니다.
/// - DialogueDatabase에서 dialogueId에 해당하는 speaker/dialogue/expression 정보를 가져옵니다.
/// - ChatDialogueManager를 통해 실제 대화창 UI를 출력합니다.
/// - ChoiceController를 통해 선택지 버튼을 생성하고 선택 결과를 전달받습니다.
/// - StandingController를 통해 캐릭터 스탠딩 표시, 숨김, 표정, 발화자 강조를 처리합니다.
/// - CGController를 통해 CG 이미지 출력과 클릭 대기를 처리합니다.
/// - DialogueSceneController에서 RunScenario()를 호출하고, 종료 콜백으로 GameProgressManager.OnDialogueFinished()를 연결합니다.
///
/// TODO:
/// - ChoiceActionType.LoadScene은 현재 직접 SceneManager.LoadScene을 사용하므로, 추후 GameProgressManager 기반 이동으로 통일 검토
/// - Narration / Command 타입 Step이 추가될 경우 ExecuteStep 분기 확장 필요
/// - 선택지 결과가 GameProgressManager 또는 별도 FlagManager에 저장되도록 확장 필요
/// - 스킵 기능이 CG, Wait, Choice에서 의도대로 동작하는지 추가 테스트 필요
/// </summary>
public class ScenarioRunner : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioData scenarioData;
    [SerializeField] private bool playOnStart = true;

    [Header("Dialogue")]
    [SerializeField] private DialogueDatabase dialogueDatabase;
    [SerializeField] private ChatDialogueManager dialogueManager;

    [Header("Choice")]
    [SerializeField] private ChoiceController choiceController;

    [Header("Standing")]
    [SerializeField] private StandingController standingController;

    [Header("CG")]
    [SerializeField] private CGController cgController;

    [Header("Fade")]
    [SerializeField] private Fade fadeController;

    [Header("Animation")]
    [SerializeField] private AnimationController animController;

    private int currentStepIndex;
    private bool isRunning;
    private bool isWaitingForDialogue;
    private bool isWaitingForChoice;
    private bool sceneLoadRequested;
    private ChoiceData selectedChoice;
    private Coroutine scenarioCoroutine;
    private bool isWaitingForCG;
    private bool isSkipping;
    private bool jumpRequested = false;

    public ScenarioData CurrentScenarioData => scenarioData;
    

    private System.Action onScenarioFinished;

    private void Start()
    {
        if (playOnStart)
        {
            RunScenario(scenarioData);
        }
    }

    public int GetCurrentStepIndex()
    {
        return currentStepIndex;
    }

        public void SetCurrentStepIndex(int index)
    {
        currentStepIndex = index;
        // 필요 시 해당 인덱스의 대화/연출을 즉시 출력하도록 갱신 로직 호출
    }

    public void PlayFromCurrentIndex()
    {
        if (scenarioData == null) return;

        // 실행 중이던 이전 코루틴이 있다면 정지
        if (isRunning)
        {
            StopScenario();
        }

        // JumpToStep 방식과 동일하게 인덱스-1로 맞춘 후 코루틴 재시작
        currentStepIndex = Mathf.Clamp(currentStepIndex - 1, -1, scenarioData.steps.Count - 1);
        jumpRequested = true;

        scenarioCoroutine = StartCoroutine(ScenarioRoutine(scenarioData));
    }

    /// <summary>
    /// Inspector에 연결된 기본 ScenarioData를 실행합니다.
    /// 버튼 테스트나 단독 테스트용으로 사용할 수 있습니다.
    /// </summary>
    public void PlayScenario()
    {
        RunScenario(scenarioData);
    }

    /// <summary>
    /// 지정한 ScenarioData를 실행하고, 시나리오가 끝나면 콜백을 호출합니다.
    /// ChatScene을 여러 진행 단계에서 재사용하기 위해 외부 컨트롤러가 시나리오를 선택해서 실행할 때 사용합니다.
    /// </summary>
    public void RunScenario(
        ScenarioData targetScenario,
        System.Action finishedCallback = null)
    {
        if (targetScenario == null)
        {
            Debug.LogError("실행할 ScenarioData가 없습니다.");
            finishedCallback?.Invoke();
            return;
        }

        scenarioData = targetScenario;

        if (!ValidateReferences())
        {
            finishedCallback?.Invoke();
            return;
        }

        if (isRunning)
        {
            StopScenario();
        }

        currentStepIndex = 0;
        sceneLoadRequested = false;
        isSkipping = false;
        onScenarioFinished = finishedCallback;

        scenarioCoroutine = StartCoroutine(
            ScenarioRoutine(targetScenario)
        );
    }

    /// <summary>
    /// 현재 실행 중인 시나리오를 중단합니다.
    /// </summary>
    public void StopScenario()
    {
        if (scenarioCoroutine != null)
        {
            StopCoroutine(scenarioCoroutine);
            scenarioCoroutine = null;
        }

        isRunning = false;
        isWaitingForDialogue = false;
        isWaitingForChoice = false;
        isWaitingForCG = false;
        selectedChoice = null;
        onScenarioFinished = null;
    }

    /// <summary>
    /// ScenarioData의 Step을 처음부터 끝까지 순서대로 실행합니다.
    /// </summary>
    private IEnumerator ScenarioRoutine(ScenarioData targetScenario)
    {
        isRunning = true;

        for (currentStepIndex = 0;
             currentStepIndex < targetScenario.steps.Count;
             currentStepIndex++)
        {
            ScenarioStep step = targetScenario.steps[currentStepIndex];

            if (step == null)
            {
                Debug.LogWarning($"{currentStepIndex}번 Step이 비어 있습니다.");
                continue;
            }

            if (isSkipping &&
                step.stepType == ScenarioStepType.Choice)
            {
                isSkipping = false;
                Debug.Log("선택지 도착 - 스킵 종료");
            }

            yield return ExecuteStep(step);

            if (jumpRequested)
            {
                jumpRequested = false;
                continue; // 바뀐 currentStepIndex 위치부터 루프 재개
            }

            if (sceneLoadRequested)
            {
                isRunning = false;
                scenarioCoroutine = null;
                yield break;
            }
        }

        isRunning = false;
        scenarioCoroutine = null;

        Debug.Log($"시나리오 종료: {targetScenario.scenarioId}");

        System.Action callback = onScenarioFinished;
        onScenarioFinished = null;
        callback?.Invoke();
    }

    /// <summary>
    /// Step 타입에 따라 실제 실행 함수를 호출합니다.
    /// </summary>
    private IEnumerator ExecuteStep(ScenarioStep step)
    {
        switch (step.stepType)
        {
            case ScenarioStepType.Dialogue:
                yield return PlayDialogue(step.dialogueId);
                break;

            case ScenarioStepType.Wait:
                yield return new WaitForSecondsRealtime(
                    Mathf.Max(0f, step.waitSeconds)
                );
                break;

            case ScenarioStepType.StandingShow:
                ShowStanding(step);
                break;

            case ScenarioStepType.StandingHide:
                HideStanding();
                break;

            case ScenarioStepType.Choice:
                yield return PlayChoice(step);
                break;

            case ScenarioStepType.CGShow:
                yield return ShowCG(step);
                break;

            case ScenarioStepType.CGHide:
                yield return HideCG(step);
                break;

            case ScenarioStepType.Fade:
                PlayFade(step);
                break;

            case ScenarioStepType.Animation:
                yield return ShowAnimation(step);
                break;

            default:
                Debug.LogWarning($"처리되지 않은 Step: {step.stepType}");
                break;
        }
    }

    /// <summary>
    /// dialogueId에 해당하는 대사를 출력합니다.
    /// </summary>
    private IEnumerator PlayDialogue(string dialogueId)
    {
        DialogueDatabase.DialogueEntry entry =
            dialogueDatabase.GetDialogue(dialogueId);

        if (entry == null)
        {
            Debug.LogWarning($"DialogueEntry를 찾을 수 없습니다: {dialogueId}");
            yield break;
        }

        if (standingController != null)
        {
            if (!string.IsNullOrWhiteSpace(entry.expressionCode))
            {
                standingController.SetExpression(entry.expressionCode);
            }

            standingController.SetColor(entry.standName);
        }

        if (isSkipping)
        {
            yield return null;
            yield break;
        }

        isWaitingForDialogue = true;

        dialogueManager.ShowSingleLine(
            entry.speaker,
            entry.dialogue,
            OnDialogueFinished
        );

        yield return new WaitUntil(
            () => !isWaitingForDialogue
        );
    }

    private void OnDialogueFinished()
    {
        isWaitingForDialogue = false;
    }

    /// <summary>
    /// 스탠딩 이미지를 표시합니다.
    /// </summary>
    private void ShowStanding(ScenarioStep step)
    {
        if (standingController == null)
        {
            Debug.LogError("StandingController가 연결되지 않았습니다.");
            return;
        }

        standingController.SetSprite(step.stands);
    }

    /// <summary>
    /// 스탠딩 이미지를 숨깁니다.
    /// </summary>
    private void HideStanding()
    {
        if (standingController == null)
        {
            Debug.LogError("StandingController가 연결되지 않았습니다.");
            return;
        }

        standingController.Hide();
    }

    /// <summary>
    /// 선택지를 출력하고, 선택 결과에 따른 액션을 실행합니다.
    /// </summary>
    private IEnumerator PlayChoice(ScenarioStep step)
    {
        if (choiceController == null)
        {
            Debug.LogError("ChoiceController가 연결되지 않았습니다.");
            yield break;
        }

        if (step.choices == null ||
            step.choices.Count == 0)
        {
            Debug.LogWarning("Choice Step에 선택지가 없습니다.");
            yield break;
        }

        selectedChoice = null;
        isWaitingForChoice = true;

        choiceController.ShowChoices(
            step.choices,
            OnChoiceSelected
        );

        yield return new WaitUntil(
            () => !isWaitingForChoice
        );

        if (selectedChoice == null)
        {
            Debug.LogError("선택한 ChoiceData를 전달받지 못했습니다.");
            yield break;
        }

        yield return ExecuteChoiceAction(selectedChoice);

        selectedChoice = null;
    }

    private void OnChoiceSelected(ChoiceData choice)
    {
        selectedChoice = choice;
        isWaitingForChoice = false;
    }

    /// <summary>
    /// 선택지 선택 후 지정된 액션을 실행합니다.
    /// </summary>
    private IEnumerator ExecuteChoiceAction(ChoiceData choice)
    {
        switch (choice.actionType)
        {
            case ChoiceActionType.NextStep:
                yield break;

            case ChoiceActionType.ReactionThenNext:
                yield return ExecuteReactionSteps(
                    choice.reactionSteps
                );
                break;

            case ChoiceActionType.LoadScene:
                LoadTargetScene(choice.targetScene);
                break;

            case ChoiceActionType.JumpToStep:
                yield return ExecuteReactionSteps(choice.reactionSteps); // 필요 시 반응 대사 먼저 출력
                currentStepIndex = choice.targetStepIndex - 1; // for문 증감(++)을 고려해 -1 처리
                jumpRequested = true;
                break;
        }
    }

    /// <summary>
    /// 선택지에서 지정한 씬으로 이동합니다.
    /// </summary>
    private void LoadTargetScene(string targetScene)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("이동할 Scene이 설정되지 않았습니다.");
            return;
        }

        sceneLoadRequested = true;

        SceneManager.LoadScene(targetScene);
    }

    /// <summary>
    /// 선택지 선택 후 실행되는 반응 Step들을 처리합니다.
    /// </summary>
    private IEnumerator ExecuteReactionSteps(
        List<ReactionStep> reactionSteps)
    {
        if (reactionSteps == null ||
            reactionSteps.Count == 0)
        {
            Debug.LogWarning("선택지의 Reaction Steps가 비어 있습니다.");
            yield break;
        }

        foreach (ReactionStep reactionStep in reactionSteps)
        {
            if (reactionStep == null)
            {
                continue;
            }

            switch (reactionStep.stepType)
            {
                case ReactionStepType.Dialogue:
                    yield return PlayDialogue(
                        reactionStep.dialogueId
                    );
                    break;

                case ReactionStepType.Wait:
                    yield return new WaitForSecondsRealtime(
                        Mathf.Max(
                            0f,
                            reactionStep.waitSeconds
                        )
                    );
                    break;
            }
        }
    }

    /// <summary>
    /// CG 이미지를 표시하고 클릭 입력을 기다립니다.
    /// </summary>
    /*private IEnumerator ShowCG(ScenarioStep step)
    {
        if (cgController == null)
        {
            Debug.LogError("CGController가 연결되지 않았습니다.");
            yield break;
        }

        if (step.cgSprite == null)
        {
            Debug.LogError("CGShow Step에 CG Sprite가 없습니다.");
            yield break;
        }

        if (isSkipping)
        {
            yield break;
        }

        if (dialogueManager != null)
        {
            dialogueManager.HideDialogueUI();
        }

        if (fadeController != null)
        {
            bool fadeOutComplete = false;
            fadeController.FadeOut(-1f, 0f, () => fadeOutComplete = true);
            yield return new WaitUntil(() => fadeOutComplete);
        }

        isWaitingForCG = true;

        cgController.Show(
            step.cgSprite,
            OnCGClicked
        );

        if (fadeController != null)
        {
            if (step.fadeDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.fadeDuration);
            }

            bool fadeInComplete = false;
            fadeController.FadeIn(-1f, 0f, () => fadeInComplete = true);
            yield return new WaitUntil(() => fadeInComplete);
        }

        yield return new WaitUntil(
            () => !isWaitingForCG
        );

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogueUI();
        }
    }*/
    private IEnumerator ShowCG(ScenarioStep step)
    {
        if (cgController == null)
        {
            Debug.LogError("CGController가 연결되지 않았습니다.");
            yield break;
        }

        if (step.cgSprite == null)
        {
            Debug.LogError("CGShow Step에 CG Sprite가 없습니다.");
            yield break;
        }

        if (isSkipping)
        {
            yield break;
        }

        if (dialogueManager != null)
        {
            dialogueManager.HideDialogueUI();
        }

        // 이미 이전 애니메이션의 FadeOut으로 화면이 완전히 어두워진 상태가 아니라면 FadeOut 실행
        if (fadeController != null && fadeController.fadeImage.color.a < 0.99f)
        {
            bool fadeOutComplete = false;
            fadeController.FadeOut(-1f, 0f, () => fadeOutComplete = true);
            yield return new WaitUntil(() => fadeOutComplete);
        }

        isWaitingForCG = true;

        // 어두워진 검은 화면 뒤에서 CG 켜기 (배경 비침 100% 차단)
        cgController.Show(
            step.cgSprite,
            OnCGClicked
        );

        // [FadeIn] CG가 배치된 후 서서히 화면을 밝게 만듦
        if (fadeController != null)
        {
            if (step.fadeDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.fadeDuration);
            }

            bool fadeInComplete = false;
            fadeController.FadeIn(-1f, 0f, () => fadeInComplete = true);
            yield return new WaitUntil(() => fadeInComplete);
        }

        // 클릭 입력을 기다림
        yield return new WaitUntil(
            () => !isWaitingForCG
        );

        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogueUI();
        }
    }

    public void OnCGClicked()
    {
        isWaitingForCG = false;
    }

    /// <summary>
    /// 현재 표시 중인 CG를 숨깁니다.
    /// </summary>
    private IEnumerator HideCG(ScenarioStep step)
    {
        if (cgController == null)
        {
            Debug.LogError("CGController가 연결되지 않았습니다.");
            yield break;
        }

        if (fadeController != null)
        {
            bool fadeOutComplete = false;
            fadeController.FadeOut(-1f, 0f, () => fadeOutComplete = true);
            yield return new WaitUntil(() => fadeOutComplete);
        }

        cgController.Hide();

        if (fadeController != null)
        {
            if (step.fadeDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(step.fadeDuration);
            }

            bool fadeInComplete = false;
            fadeController.FadeIn(-1f, 0f, () => fadeInComplete = true);
            yield return new WaitUntil(() => fadeInComplete);
        }
    }

    private IEnumerator PlayFade(ScenarioStep step)
    {
        if (fadeController == null)
        {
            Debug.LogError("Fade가 ScenarioRunner에 연결되지 않았습니다.");
            yield break;
        }

        // 1. 화면을 검게 덮기
        bool fadeOutComplete = false;

        fadeController.FadeOut(
            step.fadeDuration,
            0f,
            () => fadeOutComplete = true
        );

        yield return new WaitUntil(
            () => fadeOutComplete
        );

        // 2. 검은 화면 유지
        if (step.fadeHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                step.fadeHoldDuration
            );
        }

        // 3. 검은 화면 걷기
        bool fadeInComplete = false;

        fadeController.FadeIn(
            step.fadeDuration,
            0f,
            () => fadeInComplete = true
        );

        yield return new WaitUntil(
            () => fadeInComplete
        );
    }

    /// <summary>
    /// 다음 선택지까지 대사를 스킵합니다.
    /// </summary>
    public void StartSkipToNextChoice()
    {
        isSkipping = true;

        if (isWaitingForDialogue)
        {
            isWaitingForDialogue = false;
        }

        if (isWaitingForCG)
        {
            isWaitingForCG = false;
        }

        Debug.Log("다음 선택지까지 스킵 시작");
    }

    /// <summary>
    /// ScenarioRunner 실행에 필요한 참조가 연결되어 있는지 검사합니다.
    /// </summary>
    private bool ValidateReferences()
    {
        if (scenarioData == null)
        {
            Debug.LogError("ScenarioRunner에 ScenarioData가 없습니다.");
            return false;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogError("ScenarioRunner에 DialogueDatabase가 연결되지 않았습니다.");
            return false;
        }

        if (dialogueManager == null)
        {
            Debug.LogError("ScenarioRunner에 DialogueManager가 연결되지 않았습니다.");
            return false;
        }

        if (!dialogueDatabase.IsLoaded)
        {
            Debug.LogError("DialogueDatabase가 CSV를 불러오지 못했습니다.");
            return false;
        }

        return true;
    }


    private IEnumerator ShowAnimation(ScenarioStep step)
    {
        if (animController == null)
        {
            Debug.LogError("AnimationController가 연결되지 않았습니다.");
            yield break;
        }

        if (step.animClip == null)
        {
            Debug.LogError("Animation Step에 AnimationClip이 없습니다.");
            yield break;
        }

        if (isSkipping)
        {
            yield break;
        }

        // 대화 UI 숨기기
        if (dialogueManager != null)
        {
            dialogueManager.HideDialogueUI();
        }

        bool isAnimFinished = false;

        // 1. 애니메이션 재생 시작 (페이드 없이 즉시 실행)
        animController.PlayClip(
            step.animClip,
            () => isAnimFinished = true
        );

        // 2. 애니메이션 재생 완료 대기
        if (step.waitForCompletion)
        {
            yield return new WaitUntil(() => isAnimFinished);
        }

        // 3. 애니메이션 종료 후 잠시 대기 (마지막 프레임 유지)
        if (step.waitSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(step.animwaitSeconds);
        }

        // 4. FadeOut (서서히 암전)
        if (fadeController != null)
        {
            float duration = step.animfadeDuration > 0f ? step.animfadeDuration : 0.5f;
            bool fadeOutComplete = false;

            fadeController.FadeOut(duration, 0f, () => fadeOutComplete = true);

            // 화면이 완전히 검게 될 때까지 대기
            yield return new WaitUntil(() => fadeOutComplete);
        }

        // 5. 화면이 완전히 어두워졌을 때 애니메이션 오브젝트 비활성화
        animController.gameObject.SetActive(false);
    }
}