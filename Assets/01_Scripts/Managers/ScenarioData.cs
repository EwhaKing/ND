using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScenarioStepType
{
    Dialogue,
    Wait,
    CharacterAnimation,
    StandingShow,
    StandingHide,
    Choice
}

public enum CharacterType
{
    None,
    Tester
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

    [Header("Character Animation")]
    public CharacterType character;
    public string animationTrigger;

    [Header("Standing")]
    public StandStep[] stands;

    [Header("Choice")]
    public List<ChoiceData> choices = new();
}
[Serializable]
public class ChoiceData
{
    public string choiceText;

    [Tooltip("버튼을 눌렀을 때 이동할 씬 이름")]
    public string targetScene;
}
[Serializable]
public class StandStep
{
    [Tooltip("CSV의 speaker 이름과 같아야 합니다.")]
    public string standName;

    public Sprite sprite;
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
