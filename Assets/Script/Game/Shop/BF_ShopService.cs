using UnityEngine;

/// <summary>
/// 商店交易规则。商店不持有玩家库存副本。
/// </summary>
public class BF_ShopService : MonoBehaviour
{
    [SerializeField] private BF_ShopConfigSO _config;
    [SerializeField] private BF_InventoryService _inventory;
    [SerializeField] private BF_UnitRuntimeService _unitRuntime;

    public BF_ShopConfigSO Config => _config;

    public int GetAvailableCount(BF_ItemConfigSO item)
    {
        return item != null
            ? Mathf.Max(0, _inventory.GetCount(item.Id) - _unitRuntime.GetReservedCount(item.Id))
            : 0;
    }

    public bool TryBuy(BF_ItemConfigSO item)
    {
        if (item == null || !_inventory.CanAdd(item, 1) || !_inventory.TrySpendGold(item.BuyPrice))
        {
            return false;
        }

        return _inventory.TryAdd(item, 1);
    }

    public bool TrySell(BF_ItemConfigSO item)
    {
        if (item == null)
        {
            return false;
        }

        if (GetAvailableCount(item) <= 0 || !_inventory.TryRemove(item.Id, 1))
        {
            return false;
        }

        _inventory.AddGold(item.SellPrice);
        return true;
    }
}
