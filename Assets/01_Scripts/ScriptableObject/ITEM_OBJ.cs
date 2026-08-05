using UnityEngine;

[CreateAssetMenu(fileName = "ITEM_OBJ", menuName = "Scriptable Objects/ITEM_OBJ")]
public class ITEM_OBJ : ScriptableObject
{
    public string Name;
    [TextArea] public string Description;   // ← 이 줄 추가
}
