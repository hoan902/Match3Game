using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Obj/CellType")]
public class CellScriptableObj : ScriptableObject
{
    public string cellName;
    public CellType cellType;
    public Sprite sprite;
}
