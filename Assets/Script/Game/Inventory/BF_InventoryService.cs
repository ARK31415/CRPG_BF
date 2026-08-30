using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent 场景中的运行时金币与库存唯一入口。
/// 装备不可堆叠，每件独立占一个仓库条目；消耗品按 MaxStack 堆叠。
/// </summary>
[DefaultExecutionOrder(-80)]
public class BF_InventoryService : MonoBehaviour
{
    [SerializeField] private BF_InventoryConfigSO _config;

    private readonly List<BF_InventoryEntry> _items = new();

    public int Gold { get; private set; }
    public int Capacity => _config != null ? _config.Capacity : 0;
    public IReadOnlyList<BF_InventoryEntry> Items => _items;

    private void Awake()
    {
        Gold = _config != null ? _config.StartingGold : 0;

        if (_config == null)
        {
            return;
        }

        foreach (BF_StartingItem entry in _config.StartingItems)
        {
            TryAdd(entry.Item, entry.Quantity, false);
        }
    }

    public BF_ItemConfigSO GetItem(string itemId)
    {
        if (_config == null || string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        foreach (BF_ItemConfigSO item in _config.ItemCatalog)
        {
            if (item != null && item.Id == itemId)
            {
                return item;
            }
        }

        return null;
    }

    public int GetCount(string itemId)
    {
        int count = 0;
        foreach (BF_InventoryEntry entry in _items)
        {
            if (entry.Item != null && entry.Item.Id == itemId)
            {
                count += entry.Quantity;
            }
        }

        return count;
    }

    public bool CanAdd(BF_ItemConfigSO item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            return false;
        }

        if (item.ItemType == BF_ItemType.Equipment)
        {
            return _items.Count + quantity <= Capacity;
        }

        BF_InventoryEntry entry = FindEntry(item.Id);
        return entry != null
            ? entry.Quantity + quantity <= item.MaxStack
            : _items.Count < Capacity && quantity <= item.MaxStack;
    }

    public bool TryAdd(BF_ItemConfigSO item, int quantity, bool publish = true)
    {
        if (!CanAdd(item, quantity))
        {
            return false;
        }

        if (item.ItemType == BF_ItemType.Equipment)
        {
            for (int i = 0; i < quantity; i++)
            {
                _items.Add(new BF_InventoryEntry(item, 1));
            }
        }
        else
        {
            BF_InventoryEntry entry = FindEntry(item.Id);
            if (entry == null)
            {
                _items.Add(new BF_InventoryEntry(item, quantity));
            }
            else
            {
                entry.Quantity += quantity;
            }
        }

        if (publish)
        {
            PublishChanged();
        }

        return true;
    }

    public bool TryRemove(string itemId, int quantity)
    {
        if (quantity <= 0 || GetCount(itemId) < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
        {
            BF_InventoryEntry entry = _items[i];
            if (entry.Item == null || entry.Item.Id != itemId)
            {
                continue;
            }

            int take = Mathf.Min(entry.Quantity, remaining);
            entry.Quantity -= take;
            remaining -= take;
            if (entry.Quantity == 0)
            {
                _items.RemoveAt(i);
            }
        }

        PublishChanged();
        return true;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        PublishChanged();
        return true;
    }

    public void AddGold(int amount)
    {
        Gold += Mathf.Max(0, amount);
        PublishChanged();
    }

    private BF_InventoryEntry FindEntry(string itemId)
    {
        return _items.Find(entry => entry.Item != null && entry.Item.Id == itemId);
    }

    private void PublishChanged()
    {
        GameEventBus.Instance.Publish(new BF_InventoryChangedEvent());
    }
}
