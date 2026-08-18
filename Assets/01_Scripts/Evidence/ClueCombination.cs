using UnityEngine;

[CreateAssetMenu(fileName = "ClueCombination", menuName = "Scriptable Objects/ClueCombination")]
public class ClueCombination : ScriptableObject
{
    [Header("조합에 필요한 단서 2개")]
    public ClueData ingredientA; // 단서 A
    public ClueData ingredientB; // 단서 B

    [Header("조합 결과로 얻을 새로운 단서")]
    public ClueData resultClue;  // 새로운 단서

    // 2개의 단서가 들어왔을 때 레시피와 일치하는지 검사 (순서 상관없음)
    public bool Matches(ClueData a, ClueData b)
    {
        return (ingredientA == a && ingredientB == b) || (ingredientA == b && ingredientB == a);
    }
}
