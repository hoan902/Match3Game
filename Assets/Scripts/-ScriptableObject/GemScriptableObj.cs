using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Obj/Gem")]
public class GemScriptableObj : ScriptableObject
{
    public string gemName;
    public GemType gemType;
    public Sprite sprite;
}
