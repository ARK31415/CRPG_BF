using UnityEngine;

/// <summary>
/// 商店交易规则。商店不持有玩家库存副本。
/// </summary>
public class BF_ShopService : MonoBehaviour
{
    [SerializeField] private BF_ShopConfigSO _config;

    public BF_ShopConfigSO Config => _config;

    public int GetAvailableCount(BF_ItemConfigSO item)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService unitRuntime = BF_UnitRuntimeService.Instance;
        return item != null
            ? Mathf.Max(0, inventory.GetCount(item.Id) - unitRuntime.GetReservedCount(item.Id))
            : 0;
    }

    public bool TryBuy(BF_ItemConfigSO item)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (item == null || inventory == null || !inventory.CanAdd(item, 1) || !inventory.TrySpendGold(item.BuyPrice))
        {
            return false;
        }

        return inventory.TryAdd(item, 1);
    }

    public bool TrySell(BF_ItemConfigSO item)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (item == null || inventory == null)
        {
            return false;
        }

        if (GetAvailableCount(item) <= 0 || !inventory.TryRemove(item.Id, 1))
        {
            return false;
        }

        inventory.AddGold(item.SellPrice);
        return true;
    }
}
