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
