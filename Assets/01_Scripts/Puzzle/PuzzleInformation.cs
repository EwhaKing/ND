using UnityEngine;

[CreateAssetMenu(fileName = "Puzzle", menuName = "Puzzle/Puzzle Information")]
public class PuzzleInformation : ScriptableObject
{
    [Header("순서")]
    [SerializeField] private int sequenceIndex;

    [Tooltip("퍼즐 이름")]
    [SerializeField] private string puzzleName;

    [Header("장면 설명")]
    [TextArea(3, 5)]
    [SerializeField] private string sceneDescription;

}
