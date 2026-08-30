using System;
using UnityEngine;

/// <summary>
/// 角色四项基础属性在某一等级下的快照。
/// </summary>
public struct BF_UnitStats
{
    public int MaxHP;
    public int Attack;
    public int Defense;
    public int MaxAP;
}

/// <summary>
/// 单个角色四项属性的成长档位。
/// </summary>
[Serializable]
public class BF_GrowthProfile
{
    public BF_GrowthRank MaxHP;
    public BF_GrowthRank Attack;
    public BF_GrowthRank Defense;
    public BF_GrowthRank MaxAP;
}

/// <summary>
/// 全局属性成长曲线配置：S~D 五档等级倍率，等级 1 时倍率为 1。
/// 仿写 CodePath-Traveler 的 GlobalGrowthConfigSO，只保留本项目用到的四项属性。
/// </summary>
[CreateAssetMenu(fileName = "SO_BF_Growth", menuName = "CRPG BF/Battle/Growth Config")]
public class BF_GrowthConfigSO : ScriptableObject
{
    [Header("成长曲线(X:等级1-10,Y:倍率)")]
    [SerializeField]
    private AnimationCurve _rankS = AnimationCurve.Linear(1, 1, 10, 4f);

    [SerializeField]
    private AnimationCurve _rankA = AnimationCurve.Linear(1, 1, 10, 3.5f);

    [SerializeField]
    private AnimationCurve _rankB = AnimationCurve.Linear(1, 1, 10, 3f);

    [SerializeField]
    private AnimationCurve _rankC = AnimationCurve.Linear(1, 1, 10, 2.5f);

    [SerializeField]
    private AnimationCurve _rankD = AnimationCurve.Linear(1, 1, 10, 2f);

    public float GetMultiplier(BF_GrowthRank rank, int level)
    {
        AnimationCurve curve = rank switch
        {
            BF_GrowthRank.S => _rankS,
            BF_GrowthRank.A => _rankA,
            BF_GrowthRank.C => _rankC,
            BF_GrowthRank.D => _rankD,
            _ => _rankB
        };

        return curve != null ? curve.Evaluate(Mathf.Max(1, level)) : 1f;
    }
}
