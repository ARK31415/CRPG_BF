using System.Collections.Generic;

public class BF_BattleReward
{
    public int Gold;
    public readonly List<BF_InventoryEntry> Items = new();

    public void Clear()
    {
        Gold = 0;
        Items.Clear();
    }
}
