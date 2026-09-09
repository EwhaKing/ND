using UnityEngine;

[CreateAssetMenu(
    fileName = "InvestigationStageData",
    menuName = "Investigation/Stage Data"
)]
public class InvestigationStageData : ScriptableObject
{
    [Header("Stage")]
    public string stageId;

    [Header("Background")]
    public Sprite backgroundSprite;

    [Header("Objects")]
    public GameObject stagePrefab;
}