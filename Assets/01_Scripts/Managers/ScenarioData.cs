using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScenarioStepType
{
    Dialogue,
    Wait,
    CharacterAnimation,
    StandingShow,
    StandingHide
}

public enum CharacterType
{
    None,
    Tester
}

public enum StandingPosition
{
    Left,
    Center,
    Right
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
    public Sprite standingSprite;
    public StandingPosition standingPosition;
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