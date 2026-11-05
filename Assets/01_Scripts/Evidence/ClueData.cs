using UnityEngine;


public enum ClueType
{
    Core,       // 핵심 단서
    Sub,  // 보조 단서 (합성 재료 등)
    Trap,       // 함정 단서
    Hidden      // 숨겨진 단서
}

/// <summary>
/// 대사 한 줄의 데이터.
/// - characterName이 비어 있으면 이름창을 숨긴다 (지문/나레이션 용도)
/// - standingSprite가 null이면 스탠딩 이미지를 숨긴다
/// </summary>
[System.Serializable]
public struct DialogueEntry
{
    public string characterName;            // 화자 이름 (비워두면 지문 처리)
    public Sprite standingSprite;           // 스탠딩 이미지 (비워두면 이미지 숨김)
    [TextArea] public string text;          // 대사 내용

    public DialogueEntry(string characterName, Sprite standingSprite, string text)
    {
        this.characterName = characterName;
        this.standingSprite = standingSprite;
        this.text = text;
    }

    // 이름/스탠딩 없이 텍스트만 있는 대사(지문)를 간편하게 만들기 위한 헬퍼
    public static DialogueEntry Narration(string text)
    {
        return new DialogueEntry(string.Empty, null, text);
    }
}

[CreateAssetMenu(fileName = "NewClueData", menuName = "ScriptableObjects/Clue Data", order = 1)]
public class ClueData : ScriptableObject
{
    [Header("기본 정보")]
    public string clueID;                   // 단서 고유 ID (예: Desk_Key, Wall_Memo)
    public string clueName;                 // 게임에 표시될 단서 이름 (예: 낡은 열쇠)
    public Sprite clueIcon;                 // 인벤토리 아이콘 이미지
    public ClueType clueType;               // 단서 유형

    [Header("인벤토리 및 팝업")]
    [TextArea] public string inventoryDescription; // 팝업창 하단에 들어갈 상세 설명

    [Header("대사")]
    public DialogueEntry[] firstClickText;      // 처음 조사했을 때 나오는 대사 (캐릭터 이름 / 스탠딩 / 대사)
    [TextArea] public string secondClickText;   // 두 번째 이후 조사했을 때 나오는 대사 (예: "더 이상 볼 것은 없어 보인다.")

    [Header("조건부 상호작용")]
    public bool requiresItem;               // 특정 아이템이 있어야만 조사/획득 가능한지 여부
    public ClueData requiredClueData;       // 어떤 아이템이 필요한 지 참조
    [TextArea] public string lockedText;    // 조건 아이템이 없을 때 출력할 대사 (예: "굳게 잠겨있다.")
    [TextArea] public string openText;      // 조건 아이템이 있을 때 출력할 대사 (예: "열쇠로 잠금을 해제했다.")

    [Header("증거 합성")]
    public bool canCombine;                 // 합성 가능 여부
    public ClueData combineTarget;          // 합성할 다른 보조 단서
    public ClueData combineResult;          // 합성 결과로 새로 생성될 단서
    [TextArea] public string combineText;   // 합성 시 출력할 대사 (예: "두 단서를 합쳐 새로운 단서를 만들었다.")
}