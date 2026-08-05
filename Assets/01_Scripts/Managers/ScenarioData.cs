using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScenarioStepType
{
    Dialogue,
    Wait,
    StandingShow,
    StandingHide,
    Choice
}
public enum ChoiceActionType
{
    NextStep,
    ReactionThenNext,
    LoadScene
}
public enum ReactionStepType
{
    Dialogue,
    StandingChange,
    Wait
}

[Serializable]
public class ScenarioStep
{
    public ScenarioStepType stepType;

    [Header("Dialogue")]
    public string dialogueId;

    [Header("Wait")]
    [Min(0f)]
    public float waitSeconds = 1f;

    [Header("Standing")]
    public StandStep[] stands;

    [Header("Choice")]
    public List<ChoiceData> choices = new();
}
[Serializable]
public class ChoiceData
{
    public string choiceText;

    public ChoiceActionType actionType;

    [Tooltip("ReactionThenNext일 때 실행할 짧은 반응")]
    public List<ReactionStep> reactionSteps =
        new List<ReactionStep>();

    [Tooltip("LoadScene일 때 이동할 씬")]
    public string targetScene;
}
[Serializable]
public class StandStep
{
    [Tooltip("CSV의 speaker 이름과 같아야 합니다.")]
    public string standName;

    public Sprite sprite;
}

[Serializable]
public class ReactionStep
{
    public ReactionStepType stepType;

    [Header("Dialogue")]
    public string dialogueId;

    [Header("Standing")]
    public string standName;
    public Sprite standingSprite;

    [Header("Wait")]
    [Min(0f)]
    public float waitSeconds = 0.5f;
}

[CreateAssetMenu(
    fileName = "ScenarioData",
    menuName = "Scenario/Scenario Data"
)]
public class ScenarioData : ScriptableObject
{
    public string scenarioId;

    public List<ScenarioStep> steps = new();
}
