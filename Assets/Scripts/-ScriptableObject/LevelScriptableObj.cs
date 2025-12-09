using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "My Scriptable Obj/Level")]
public class LevelScriptableObj : ScriptableObject
{
    public string levelName;
    public int width;
    public int height;
    public List<GemScriptableObj> availableGemList;
    public GemScriptableObj ramdomGenerateGem;
    public List<CellScriptableObj> availableCellTypeList;
    public GoalType goalType;
    public int moveAmount;
    public int targetScore;
    public List<CellData> levelCellList;
}
