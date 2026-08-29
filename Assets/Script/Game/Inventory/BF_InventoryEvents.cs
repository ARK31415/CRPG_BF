public class BF_InventoryChangedEvent : IGameEvent
{
}

public class BF_UnitRuntimeChangedEvent : IGameEvent
{
    public string UnitId;

    public BF_UnitRuntimeChangedEvent(string unitId)
    {
        UnitId = unitId;
    }
}
