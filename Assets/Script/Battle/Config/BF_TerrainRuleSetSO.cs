using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条地形规则：地形类型、是否可通行与正整数移动成本。
/// 只描述地形本身，不保存任何关卡坐标。
/// </summary>
[Serializable]
public class BF_TerrainRuleData
{
    public TerrainType TerrainType = TerrainType.Normal;

    [Tooltip("是否允许进入。不可通行的地形不参与移动成本计算。")]
    public bool Passable = true;

    [Tooltip("进入该格的移动成本。可通行时必须为正整数；Blocked 必须不可通行。")]
    public int MoveCost = 1;
}

/// <summary>
/// 共享地形通行与移动成本规则集。
/// 规则集缺失、重复或覆盖不完整时由 Board 初始化报错，不允许静默使用 0 成本。
/// </summary>
[CreateAssetMenu(fileName = "SO_BF_TerrainRules", menuName = "CRPG BF/Battle/Terrain Rules")]
public class BF_TerrainRuleSetSO : ScriptableObject
{
    [SerializeField]
    private List<BF_TerrainRuleData> _rules = new()
    {
        new BF_TerrainRuleData { TerrainType = TerrainType.Normal, Passable = true, MoveCost = 1 },
        new BF_TerrainRuleData { TerrainType = TerrainType.Difficult, Passable = true, MoveCost = 2 },
        new BF_TerrainRuleData { TerrainType = TerrainType.Swamp, Passable = true, MoveCost = 4 },
        new BF_TerrainRuleData { TerrainType = TerrainType.Blocked, Passable = false, MoveCost = 0 },
    };

    public IReadOnlyList<BF_TerrainRuleData> Rules => _rules;

    public bool TryGetRule(TerrainType terrainType, out BF_TerrainRuleData rule)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i].TerrainType == terrainType)
            {
                rule = _rules[i];
                return true;
            }
        }

        rule = null;
        return false;
    }

    /// <summary>
    /// 校验规则集：地形不重复、可通行为正成本、Blocked 不可通行。
    /// 校验失败时通过 out 返回原因，不静默修改配置。
    /// </summary>
    public bool Validate(out string error)
    {
        HashSet<TerrainType> seen = new();

        for (int i = 0; i < _rules.Count; i++)
        {
            BF_TerrainRuleData rule = _rules[i];
            if (rule == null)
            {
                error = $"Terrain rule entry {i} is null.";
                return false;
            }

            if (!seen.Add(rule.TerrainType))
            {
                error = $"Duplicate terrain rule: {rule.TerrainType}.";
                return false;
            }

            if (rule.TerrainType == TerrainType.Blocked)
            {
                if (rule.Passable)
                {
                    error = $"Terrain {rule.TerrainType} must be impassable.";
                    return false;
                }

                continue;
            }

            if (rule.Passable && rule.MoveCost < 1)
            {
                error = $"Passable terrain {rule.TerrainType} must have a positive move cost.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
