using System;

[Serializable]
public class BF_InventoryEntry
{
    public BF_InventoryEntry(BF_ItemConfigSO item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public BF_ItemConfigSO Item;
    public int Quantity;
}
