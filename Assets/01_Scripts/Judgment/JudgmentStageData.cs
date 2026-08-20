using System;
using UnityEngine;

[Serializable]
public class JudgmentStageData
{
    [Header("Stage")]
    public string stageName;

    [Header("Choice UI")]
    [TextArea(2, 4)]
    public string choiceDescription;
}
