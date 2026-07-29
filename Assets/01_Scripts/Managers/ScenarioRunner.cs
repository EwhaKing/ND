using System.Collections;
using UnityEngine;

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
            Debug.LogWarning("�̹� �ó������� ���� ���Դϴ�.");
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
                    $"{currentStepIndex}�� Step�� ��� �ֽ��ϴ�."
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
            $"�ó����� ����: {scenarioData.scenarioId}"
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
                    $"ó������ ���� Step Ÿ��: {step.stepType}"
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
        // CharacterController ���� �� �� �κ��� �����Ѵ�.
        Debug.Log(
            $"ĳ���� �ִϸ��̼� ����: " +
            $"{step.character}, {step.animationTrigger}"
        );
    }
    private void ShowStanding(ScenarioStep step)
    {
        if (standingController == null)
        {
            Debug.LogError(
                "StandingController�� ������� �ʾҽ��ϴ�."
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
                "StandingController�� ������� �ʾҽ��ϴ�."
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
                "ScenarioRunner�� ScenarioData�� �����ϴ�."
            );
            return false;
        }

        if (dialogueDatabase == null)
        {
            Debug.LogError(
                "ScenarioRunner�� DialogueDatabase�� ������� �ʾҽ��ϴ�."
            );
            return false;
        }

        if (dialogueManager == null)
        {
            Debug.LogError(
                "ScenarioRunner�� DialogueManager�� ������� �ʾҽ��ϴ�."
            );
            return false;
        }

        if (!dialogueDatabase.IsLoaded)
        {
            Debug.LogError(
                "DialogueDatabase�� ���� CSV�� ���� ���߽��ϴ�."
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
                "ChoiceController�� ������� �ʾҽ��ϴ�."
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


