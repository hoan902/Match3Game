using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Obj/Gem")]
public class GemScriptableObj : ScriptableObject
{
    public string gemName;
    public GemType gemType;
    public GemSpecialType gemSpecialType = GemSpecialType.None;
    public Sprite sprite;
}
