using UnityEngine;

public class BF_UnitSelectedEvent : IGameEvent
{
    public BF_BattleUnit Unit;

    public BF_UnitSelectedEvent(BF_BattleUnit unit)
    {
        Unit = unit;
    }
}

public class BF_UnitStatsChangedEvent : IGameEvent
{
    public BF_BattleUnit Unit;

    public BF_UnitStatsChangedEvent(BF_BattleUnit unit)
    {
        Unit = unit;
    }
}

public class BF_DamagePopupEvent : IGameEvent
{
    public BF_BattleUnit Unit;
    public int Damage;

    public BF_DamagePopupEvent(BF_BattleUnit unit, int damage)
    {
        Unit = unit;
        Damage = damage;
    }
}

public class BF_PathCostChangedEvent : IGameEvent
{
    public int Cost;
    public int RemainingAP;

    public BF_PathCostChangedEvent(int cost, int remainingAP)
    {
        Cost = cost;
        RemainingAP = remainingAP;
    }
}

/// <summary>
/// 指针进入/离开棋盘格时发布。HasCell 为 false 表示离开棋盘或进入 UI，用于隐藏地形信息。
/// 数据直接来自运行时 BF_BoardCell 已解析结果，不重新解释 TerrainRuleSet。
/// </summary>
public class BF_BoardCellHoveredEvent : IGameEvent
{
    public bool HasCell;
    public Vector2Int Position;
    public TerrainType Terrain;
    public int MoveCost;
    public bool Passable;
    public bool IsOccupied;

    public BF_BoardCellHoveredEvent()
    {
        HasCell = false;
    }

    public BF_BoardCellHoveredEvent(
        Vector2Int position,
        TerrainType terrain,
        int moveCost,
        bool passable,
        bool isOccupied)
    {
        HasCell = true;
        Position = position;
        Terrain = terrain;
        MoveCost = moveCost;
        Passable = passable;
        IsOccupied = isOccupied;
    }
}

public class BF_SkillRequestEvent : IGameEvent
{
    public BF_SkillConfigSO Skill;

    public BF_SkillRequestEvent(BF_SkillConfigSO skill)
    {
        Skill = skill;
    }
}

public class BF_EndUnitRequestEvent : IGameEvent
{
}

public class BF_ItemRequestEvent : IGameEvent
{
    public int Slot;

    public BF_ItemRequestEvent(int slot)
    {
        Slot = slot;
    }
}

public class BF_BattleResultEvent : IGameEvent
{
    public BF_BattleResult Result;

    public BF_BattleResultEvent(BF_BattleResult result)
    {
        Result = result;
    }
}

public class BF_ConfirmBattleResultRequestEvent : IGameEvent
{
}
