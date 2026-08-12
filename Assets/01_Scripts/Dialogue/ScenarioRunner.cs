using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ScenarioRunner
///
/// 담당:
/// - ScenarioData에 등록된 Step들을 순서대로 실행
/// - Dialogue, Wait, StandingShow, StandingHide, Choice 타입의 시나리오 진행을 처리
/// - Dialogue Step에서는 DialogueDatabase에서 dialogueId에 맞는 대사를 가져와 ChatDialogueManager로 출력
/// - Choice Step에서는 ChoiceController를 통해 선택지를 출력하고, 선택 결과에 따라 다음 진행을 처리
/// - 선택지 이후 ReactionStep을 실행하여 짧은 대사, 스탠딩 변경, 대기 연출을 처리
/// - LoadScene 액션을 통해 특정 씬으로 이동 가능
///
/// 사용 위치:
/// - 대화/연출 시나리오를 실행할 씬의 ScenarioRunner 오브젝트에 부착
/// - 챕터 도입 대화, 조사 전후 대화, 선택지 대화 흐름을 실행하는 데 사용
///
/// 연결:
/// - ScenarioData의 steps를 읽어 시나리오 흐름을 실행
/// - DialogueDatabase에서 dialogueId에 해당하는 speaker/dialogue 데이터를 가져옴
/// - ChatDialogueManager를 통해 실제 대화창 UI를 출력
/// - ChoiceController를 통해 선택지 버튼을 생성하고 선택 결과를 전달 받음
/// - StandingController를 통해 캐릭터 스탠딩 표시, 숨김, 표정/스프라이트 변경을 처리
///
/// TODO:
/// - LoadScene 방식 대신 GameFlowManager / ChapterManager와 연결하여 GameState 전환 방식으로 확장 검토
/// - 선택지 결과가 GameFlagManager에 저장되도록 연결 필요
/// - Narration / CG / Command 타입 Step 추가 시 ExecuteStep 분기 확장 필요
/// - 시나리오 종료 후 다음 게임 상태로 넘어가는 콜백 구조 추가 필요

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

    private int currentStepIndex;
    private bool isRunning;
    private bool isWaitingForDialogue;
    private bool isWaitingForChoice;
    private bool sceneLoadRequested;
    private ChoiceData selectedChoice;
    private Coroutine scenarioCoroutine;
    private bool isWaitingForCG;

    private void Start()
    {
        if (playOnStart)
        {
            PlayScenario();
        }
    }

    public void PlayScenario()
    {
        if (isRunning)
        {
            Debug.LogWarning("시나리오 실행.");
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        currentStepIndex = 0;

        scenarioCoroutine =
            StartCoroutine(ScenarioRoutine());
    }

    public void StopScenario()
    {
        if (scenarioCoroutine != null)
        {
            StopCoroutine(scenarioCoroutine);
            scenarioCoroutine = null;
        }

        isRunning = false;
        isWaitingForDialogue = false;
    }

    private IEnumerator ScenarioRoutine()
    {
        isRunning = true;

        while (currentStepIndex < scenarioData.steps.Count)
        {
            ScenarioStep step =
                scenarioData.steps[currentStepIndex];

            if (step == null)
            {
                Debug.LogWarning(
                    $"{currentStepIndex}번 Step 비어 있음."
                );

                currentStepIndex++;
                continue;
            }

            yield return ExecuteStep(step);

            currentStepIndex++;
        }

        isRunning = false;
        scenarioCoroutine = null;

        Debug.Log(
            $"시나리오 종료: {scenarioData.scenarioId}"
        );
    }

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
                HideCG();
                break;

            default:
                Debug.LogWarning(
                    $"처리되지 않은 Step: {step.stepType}"
                );
                break;
        }
    }
    private IEnumerator PlayDialogue(string dialogueId)
    {
        DialogueDatabase.DialogueEntry entry = dialogueDatabase.GetDialogue(dialogueId);

        if (entry == null)
        {   
            yield break;
        }
        var dialogueEntry = dialogueDatabase.GetDialogue(dialogueId);
        if (standingController != null)
        {
            standingController.SetExpression(dialogueEntry.expressionCode);
        }
        /*if (standingController != null)
        {
            standingController.SetColor(entry.speaker);
        }*/

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

    private void ShowStanding(ScenarioStep step)
    {
        if (standingController == null)
        {
            Debug.LogError(
                "StandingController가 연결되지 않음."
            );
            return;
        }

        standingController.SetSprite(step.stands);
    }

    private void HideStanding()
    {
        if (standingController == null)
        {
            Debug.LogError(
                "StandingController가 연결되지 않음."
            );
            return;
        }

        standingController.Hide();
    }

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
        }
    }
    private void LoadTargetScene(string targetScene)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError(
                "이동할 Scene이 설정되지 않았습니다."
            );
            return;
        }

        sceneLoadRequested = true;

        SceneManager.LoadScene(targetScene);
    }
    private IEnumerator ExecuteReactionSteps(List<ReactionStep> reactionSteps)
    {
        if (reactionSteps == null ||
            reactionSteps.Count == 0)
        {
            Debug.LogWarning(
                "선택지의 Reaction Steps가 비어 있습니다."
            );

            yield break;
        }

        foreach (ReactionStep reactionStep
                 in reactionSteps)
        {
            if (reactionStep == null)
            {
                continue;
            }

            switch (reactionStep.stepType)
            {
                case ReactionStepType.Dialogue:
                    Debug.Log(
                        $"리액션 대사 실행: {reactionStep.dialogueId}"
                    );

                    yield return PlayDialogue(
                        reactionStep.dialogueId
                    );
                    break;

                /*case ReactionStepType.StandingChange:
                    standingController.ChangeSprite(
                        reactionStep.standName,
                        reactionStep.standingSprite
                    );
                    break;*/

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

        // 플레이어가 선택지를 누를 때까지 대기
        yield return new WaitUntil(
            () => !isWaitingForChoice
        );

        if (selectedChoice == null)
        {
            Debug.LogError("선택한 ChoiceData를 전달받지 못했습니다.");
            yield break;
        }

        // 선택한 항목의 Reaction Steps 실행
        yield return ExecuteChoiceAction(
            selectedChoice
        );

        selectedChoice = null;
    }
    private void OnChoiceSelected(ChoiceData choice)
    {
        selectedChoice = choice;
        isWaitingForChoice = false;
    }
    private IEnumerator PlayScenario(ScenarioData targetScenario)
    {
        sceneLoadRequested = false;

        for (int i = 0; i < targetScenario.steps.Count; i++)
        {
            ScenarioStep step = targetScenario.steps[i];

            if (step == null)
            {
                continue;
            }

            yield return ExecuteStep(step);

            if (sceneLoadRequested)
            {
                yield break;
            }
        }
    }
    private bool ValidateReferences()
    {
        if (scenarioData == null)
        {
            Debug.LogError(
                "ScenarioRunner에 ScenarioData가 없음."
            );
            return false;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogError(
                "ScenarioRunner에 DialogueDatabase가 연결되지 않음"
            );
            return false;
        }

        if (dialogueManager == null)
        {
            Debug.LogError(
                "ScenarioRunne에r DialogueManager가 연결되지 않음."
            );
            return false;
        }

        if (!dialogueDatabase.IsLoaded)
        {
            Debug.LogError(
                "DialogueDatabase가 CSV를 불러오지 못함."
            );
            return false;
        }

        return true;
    }
    private IEnumerator ShowCG(
    ScenarioStep step)
    {
        if (cgController == null)
        {
            Debug.LogError(
                "CGController가 연결되지 않았습니다."
            );

            yield break;
        }

        if (step.cgSprite == null)
        {
            Debug.LogError(
                "CGShow Step에 CG Sprite가 없습니다."
            );

            yield break;
        }

        // 대화창 숨기기
        if (dialogueManager != null)
        {
            dialogueManager.HideDialogueUI();
        }

        isWaitingForCG = true;

        // CG 표시
        cgController.Show(
            step.cgSprite,
            OnCGClicked
        );

        // 플레이어 클릭까지 대기
        yield return new WaitUntil(
            () => !isWaitingForCG
        );

        // 클릭하면 대화창 다시 표시
        if (dialogueManager != null)
        {
            dialogueManager.ShowDialogueUI();
        }
    }

    public void OnCGClicked()
    {
        isWaitingForCG=false;
    }
    private void HideCG()
    {
        if (cgController == null)
        {
            Debug.LogError("CGController가 연결되지 않았습니다.");
            return;
        }
        cgController.Hide();
    }
}


