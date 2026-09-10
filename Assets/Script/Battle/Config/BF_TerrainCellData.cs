using System;
using UnityEngine;

/// <summary>
/// 关卡内单个坐标到地形类型的覆盖配置。
/// 只保存坐标与地形类型，不重复保存移动成本；成本由共享地形规则唯一提供。
/// </summary>
[Serializable]
public class BF_TerrainCellData
{
    public Vector2Int Position;

    public TerrainType TerrainType = TerrainType.Normal;
}
