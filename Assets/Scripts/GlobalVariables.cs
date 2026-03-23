using System.Collections.Generic;
using UnityEngine;

public class GlobalVariables
{

}

public class GlobalFunction
{
    //----------------------------------------
    // Utility helpers to compare GemScriptableObj gemtype
    //----------------------------------------
    public static bool CompareGemType(GemScriptableObj gem1, GemScriptableObj gem2)
    {
        if (gem1 == null || gem2 == null)
            return false;
        return gem1.gemType == gem2.gemType;
    }
    public static void RemoveGemTypeFromList(List<GemScriptableObj> list, GemScriptableObj sample)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (CompareGemType(list[i], sample))
                list.RemoveAt(i);
        }
    }
    //----------------------------------------
}
public enum GameState
{
    Busy,
    PlayerMove,
    GameOver
}
public enum GoalType
{
    Score
}
public enum GemType
{
    None,
    Star,
    Heart,
    Diamond,
    Hexa,
    Circle,
    RandomGenerate
}
public enum GemSpecialType
{
    None,
    BombSquare,
    BombVertical,
    BombHorizontal,
    BombClearSameGem
}
public enum CellType
{
    Normal,
    Blocked
}
