using System.Collections;
using UnityEngine;

public class ScenarioRunner : MonoBehaviour
{
    [Header("Scenario")]
    [SerializeField] private ScenarioData scenarioData;
    [SerializeField] private bool playOnStart = true;

    [Header("Dialogue")]
    [SerializeField] private DialogueDatabase dialogueDatabase;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Choice")]
    [SerializeField] private ChoiceController choiceController;

    [Header("Standing")]
    [SerializeField] private StandingController standingController;

    private int currentStepIndex;
    private bool isRunning;
    private bool isWaitingForDialogue;
    private bool isWaitingForChoice;

    private Coroutine scenarioCoroutine;

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
            Debug.LogWarning("이미 시나리오가 실행 중입니다.");
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
                    $"{currentStepIndex}번 Step이 비어 있습니다."
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

            case ScenarioStepType.CharacterAnimation:
                PlayCharacterAnimation(step);
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

            default:
                Debug.LogWarning(
                    $"처리되지 않은 Step 타입: {step.stepType}"
                );
                break;
        }
    }
    private IEnumerator PlayDialogue(string dialogueId)
    {
        DialogueDatabase.DialogueEntry entry =
            dialogueDatabase.GetDialogue(dialogueId);

        if (entry == null)
        {
            yield break;
        }

        if (standingController != null)
        {
            standingController.SetColor(entry.speaker);
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

    private void PlayCharacterAnimation(ScenarioStep step)
    {
        // CharacterController 제작 후 이 부분을 연결한다.
        Debug.Log(
            $"캐릭터 애니메이션 예정: " +
            $"{step.character}, {step.animationTrigger}"
        );
    }
    private void ShowStanding(ScenarioStep step)
    {
        if (standingController == null)
        {
            Debug.LogError(
                "StandingController가 연결되지 않았습니다."
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
                "StandingController가 연결되지 않았습니다."
            );
            return;
        }

        standingController.Hide();
    }

    private bool ValidateReferences()
    {
        if (scenarioData == null)
        {
            Debug.LogError(
                "ScenarioRunner에 ScenarioData가 없습니다."
            );
            return false;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogError(
                "ScenarioRunner에 DialogueDatabase가 연결되지 않았습니다."
            );
            return false;
        }

        if (dialogueManager == null)
        {
            Debug.LogError(
                "ScenarioRunner에 DialogueManager가 연결되지 않았습니다."
            );
            return false;
        }

        if (!dialogueDatabase.IsLoaded)
        {
            Debug.LogError(
                "DialogueDatabase가 아직 CSV를 읽지 못했습니다."
            );
            return false;
        }

        return true;
    }
    private IEnumerator PlayChoice(ScenarioStep step)
    {
        if (choiceController == null)
        {
            Debug.LogError(
                "ChoiceController가 연결되지 않았습니다."
            );

            yield break;
        }

        isWaitingForChoice = true;

        choiceController.ShowChoices(
            step.choices,
            () =>
            {
                isWaitingForChoice = false;
            }
        );

        yield return new WaitUntil(
            () => !isWaitingForChoice
        );
    }
}


