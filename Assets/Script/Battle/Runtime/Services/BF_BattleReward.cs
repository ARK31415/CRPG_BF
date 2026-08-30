using System.Collections.Generic;

public class BF_BattleReward
{
    public int Gold;
    public int Exp;
    public readonly List<BF_InventoryEntry> Items = new();
    public readonly List<BF_UnitExpGain> UnitGains = new();

    public void Clear()
    {
        Gold = 0;
        Exp = 0;
        Items.Clear();
        UnitGains.Clear();
    }
}

/// <summary>
/// 单个角色本场获得的经验与等级变化快照。
/// </summary>
public class BF_UnitExpGain
{
    public string UnitId;
    public string UnitName;
    public int GainedExp;
    public int OldLevel;
    public int NewLevel;
}
