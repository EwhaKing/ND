using System;
using System.Collections.Generic;
using UnityEngine;

public enum RefutationStepType
{
    Fade,
    StandingShow,
    DialogueGroup,
    RefutationGame,
    CGShow,
    MiniGame
}

[Serializable]
public class RefutationStep
{
    public RefutationStepType stepType;

    [Header("Fade 옵션")]
    public float fadeDuration = 0.5f;

    [Header("Standing 옵션")]
    public StandStep[] stands;

    [Header("Dialogue Group ID")]
    public string dialogueGroupId;

    [Header("Refutation (증언 ID 목록)")]
    public List<string> testimonyIds = new();

    [Header("CG Sprite")]
    public Sprite cgSprite;
}

[CreateAssetMenu(fileName = "NewRefutationData", menuName = "Refutation/Refutation Data")]
public class RefutationData : ScriptableObject
{
    public string stageId;

    [Header("1. 통합 대화 및 논파 CSV")]
    public TextAsset refutationCsv;   
    public TextAsset customWrongCsv; 

    [Header("2. 연출 순서 조립")]
    public List<RefutationStep> steps = new();

    [Header("3. 논파 실패 처리")]
    [Tooltip("목숨을 모두 잃었을 때 재생할 대화 그룹 ID (\"유스티티아: 그만.\" …)\n비워두면 대사 없이 바로 재도전")]
    public string failDialogueGroupId;
}