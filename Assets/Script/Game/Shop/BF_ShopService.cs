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
        return item != null && inventory != null ? inventory.GetCount(item.Id) : 0;
    }

    /// <summary>
    /// 购买结果判定：商品有效性、金币检查由商店负责，容量与堆叠失败映射自库存层唯一结果。
    /// 失败不扣金币、不增加库存。
    /// </summary>
    public BF_ShopBuyResult GetBuyResult(BF_ItemConfigSO item)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (item == null || inventory == null)
        {
            return BF_ShopBuyResult.InvalidItem;
        }

        if (inventory.Gold < item.BuyPrice)
        {
            return BF_ShopBuyResult.NotEnoughGold;
        }

        return inventory.GetAddResult(item, 1) switch
        {
            BF_InventoryAddResult.InventoryFull => BF_ShopBuyResult.InventoryFull,
            BF_InventoryAddResult.StackFull => BF_ShopBuyResult.StackFull,
            BF_InventoryAddResult.InvalidItem => BF_ShopBuyResult.InvalidItem,
            _ => BF_ShopBuyResult.Success
        };
    }

    /// <summary>
    /// 执行一次购买并返回实际结果。失败不扣金币、不增加库存；
    /// 成功时金币与库存由库存层原子入口一次完成并只广播一次。
    /// </summary>
    public BF_ShopBuyResult Buy(BF_ItemConfigSO item)
    {
        BF_ShopBuyResult result = GetBuyResult(item);
        if (result != BF_ShopBuyResult.Success)
        {
            return result;
        }

        // 预览与执行之间没有事件发布；TryPurchase 失败仅可能是金币在同步段内被回调修改。
        BF_InventoryService inventory = BF_InventoryService.Instance;
        return inventory != null && inventory.TryPurchase(item, item.BuyPrice)
            ? BF_ShopBuyResult.Success
            : BF_ShopBuyResult.NotEnoughGold;
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
