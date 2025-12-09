using System;
using UnityEngine;

[System.Serializable]
public class CellData
{
    public int x;
    public int y;
    public CellScriptableObj cellTypeInfo;
    public GemScriptableObj currentContainedGem;
}
