using System;
using System.Collections.Generic;

[Serializable]
public class BF_SaveData
{
    public int Version = 1;
    public string SavedAt;
    public int HighestUnlockedLevel;
    public bool IsDemoCompleted;
    public int Gold;
    public List<BF_InventorySaveEntry> Inventory = new();
    public List<BF_UnitRuntimeData> Units = new();
}

[Serializable]
public class BF_InventorySaveEntry
{
    public string ItemId;
    public int Quantity;
}

[Serializable]
public class BF_SaveSlotInfo
{
    public int Slot;
    public bool HasData;
    public bool IsValid;
    public int HighestUnlockedLevel;
    public int UnitCount;
    public string SavedAt;
}
