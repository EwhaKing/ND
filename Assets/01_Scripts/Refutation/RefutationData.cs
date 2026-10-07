using System;
using System.Collections.Generic;
using UnityEngine;

public enum RefutationStepType
{
    Fade,
    StandingShow,
    DialogueGroup,
    RefutationGame,
    CGShow
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

    [Header("CG Sprite")]
    public Sprite cgSprite;
}

[CreateAssetMenu(fileName = "NewRefutationData", menuName = "Refutation/Data")]
public class RefutationData : ScriptableObject
{
    public string stageId;

    [Header("1. 일반 대화 CSV")]
    public TextAsset stageDialogueCsv; // 일반 대화 CSV

    [Header("2. 논파 파트 CSV")]
    public TextAsset refutationCsv;   // 메인 논파 CSV
    public TextAsset customWrongCsv; // 커스텀 오답 CSV

    [Header("3. 논파 증언 ID 목록")]
    public List<string> testimonyIds = new();

    [Header("4. 연출 순서 조립")]
    public List<RefutationStep> steps = new();
}