using System;

/// <summary>
/// 玩家角色跨场景数据。成长阶段继续扩展此类。
/// </summary>
[Serializable]
public class BF_UnitRuntimeData
{
    // 身份
    public string UnitId;
    public string ConfigId;

    // 成长
    public int Level;
    public int CurrentExp;

    // 装备
    public string WeaponItemId;
    public string HeadItemId;
    public string ArmorItemId;
    public string ShoesItemId;

    // 技能
    public string Skill01Id;
    public string Skill02Id;

    // 快捷栏
    public string[] BattleItemIds = new string[BF_GameConstants.BattleItemSlotCount];

    // 出战状态
    public bool IsDeployed;

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
        BattleItemIds = new string[BF_GameConstants.BattleItemSlotCount];
        Level = 1;
        CurrentExp = 0;
        IsDeployed = isDeployed;
    }

    public BF_UnitRuntimeData Clone()
    {
        return new BF_UnitRuntimeData(UnitId, ConfigId, Skill01Id, Skill02Id, IsDeployed)
        {
            WeaponItemId = WeaponItemId,
            HeadItemId = HeadItemId,
            ArmorItemId = ArmorItemId,
            ShoesItemId = ShoesItemId,
            BattleItemIds = BattleItemIds != null
                ? (string[])BattleItemIds.Clone()
                : new string[BF_GameConstants.BattleItemSlotCount],
            Level = Level,
            CurrentExp = CurrentExp
        };
    }
}
