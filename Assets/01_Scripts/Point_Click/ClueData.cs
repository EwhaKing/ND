using UnityEngine;

[CreateAssetMenu(fileName = "NewClueData", menuName = "ScriptableObjects/Clue Data", order = 1)]
public class ClueData : ScriptableObject

{
    [Header("기본 정보")]
    public string clueID;   // 단서 고유 ID (예: Desk_Key, Wall_Memo)
    public string clueName; // 게임에 표시될 단서 이름 (예: 낡은 열쇠)
    public Sprite clueIcon; // 인벤토리에 들어갈 아이콘 이미지

    [Header("대사")]
    [TextArea(3, 5)]
    public string firstClickText; // 처음 조사했을 때 나오는 대사
    [TextArea(3, 5)]
    public string secondClickText; // 두 번째 이후 조사했을 때 나오는 대사 ("더 이상 볼 것은 없어 보인다.")

    [Header("조건부 상호작용")]
    public bool requiresItem; // 특정 아이템이 있어야만 조사/획득 가능한지 여부
    public ClueData requiredClueData; // 어떤 아이템이 필요한 지 참조
    [TextArea(3, 5)]
    public string lockedText; // 조건 아이템이 없을 때 출력할 대사 (예: "굳게 잠겨있다.")
    public string openText; // 조건 아이템이 있을 때 출력할 대사 (예: "열쇠로 잠금을 해제했다.")
} 

