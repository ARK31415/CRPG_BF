using System;

/// <summary>
/// 玩家角色跨场景数据。成长阶段继续扩展此类。
/// </summary>
[Serializable]
public class BF_UnitRuntimeData
{
    public BF_UnitRuntimeData(
        string unitId,
        string configId,
        string skill01,
        string skill02,
        bool isDeployed)
    {
        UnitId = unitId;
        ConfigId = configId;
        Skill01Id = skill01;
        Skill02Id = skill02;
        BattleItemIds = new string[4];
        Level = 1;
        CurrentExp = 0;
        IsDeployed = isDeployed;
    }

    public string UnitId;
    public string ConfigId;
    public string WeaponItemId;
    public string HeadItemId;
    public string ArmorItemId;
    public string ShoesItemId;
    public string Skill01Id;
    public string Skill02Id;
    public string[] BattleItemIds = new string[4];
    public int Level;
    public int CurrentExp;
    public bool IsDeployed;
}
