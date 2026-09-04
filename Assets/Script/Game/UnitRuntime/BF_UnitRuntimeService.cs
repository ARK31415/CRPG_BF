using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent 场景中的玩家角色整备数据入口。
/// 装备采用移动所有权：穿上即从仓库移除，卸下回仓；
/// 战斗物品槽是共享真实库存的快捷栏，只记录 ItemId，不扣除、不预留库存。
/// </summary>
[DefaultExecutionOrder(-70)]
public class BF_UnitRuntimeService : Singleton<BF_UnitRuntimeService>
{
    private readonly List<BF_UnitRuntimeData> _units = new();

    public IReadOnlyList<BF_UnitRuntimeData> Units => _units;

    public BF_UnitRuntimeData AddUnit(
        string configId,
        string skill01Id,
        string skill02Id,
        bool isDeployed = false)
    {
        if (string.IsNullOrEmpty(configId))
        {
            return null;
        }

        int index = 1;
        string unitId;
        do
        {
            unitId = $"{configId}_{index:000}";
            index++;
        }
        while (Get(unitId) != null);

        BF_UnitRuntimeData data = new BF_UnitRuntimeData(
            unitId,
            configId,
            skill01Id,
            skill02Id,
            isDeployed);
        _units.Add(data);
        PublishChanged(data.UnitId);
        return data;
    }

    public BF_UnitRuntimeData Get(string unitId)
    {
        return _units.Find(unit => unit.UnitId == unitId);
    }

    public int DeployedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].IsDeployed)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public List<BF_UnitRuntimeData> GetDeployedUnits()
    {
        List<BF_UnitRuntimeData> deployed = new();
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].IsDeployed)
            {
                deployed.Add(_units[i]);
            }
        }

        return deployed;
    }

    public bool SetDeployed(string unitId, bool isDeployed)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null || data.IsDeployed == isDeployed)
        {
            return data != null;
        }

        data.IsDeployed = isDeployed;
        PublishChanged(unitId);
        return true;
    }

    public string GetEquipment(string unitId, BF_EquipmentSlot slot)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null)
        {
            return string.Empty;
        }

        return slot switch
        {
            BF_EquipmentSlot.Weapon => data.WeaponItemId,
            BF_EquipmentSlot.Head => data.HeadItemId,
            BF_EquipmentSlot.Armor => data.ArmorItemId,
            BF_EquipmentSlot.Shoes => data.ShoesItemId,
            _ => string.Empty
        };
    }

    /// <summary>
    /// 设置指定部位的装备。穿上时从仓库移除一件，替换时旧装备回仓，卸下时空位不足则失败。
    /// </summary>
    public bool SetEquipment(string unitId, BF_EquipmentSlot slot, string itemId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null)
        {
            return false;
        }

        string oldItemId = GetEquipment(unitId, slot);
        if (oldItemId == itemId)
        {
            return true;
        }

        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("BF_InventoryService not found, cannot change equipment.", this);
            return false;
        }

        if (!string.IsNullOrEmpty(itemId) && !inventory.TryRemove(itemId, 1))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(oldItemId) && !inventory.TryAdd(inventory.GetItem(oldItemId), 1))
        {
            if (!string.IsNullOrEmpty(itemId))
            {
                inventory.TryAdd(inventory.GetItem(itemId), 1);
            }

            return false;
        }

        switch (slot)
        {
            case BF_EquipmentSlot.Weapon:
                data.WeaponItemId = itemId;
                break;
            case BF_EquipmentSlot.Head:
                data.HeadItemId = itemId;
                break;
            case BF_EquipmentSlot.Armor:
                data.ArmorItemId = itemId;
                break;
            case BF_EquipmentSlot.Shoes:
                data.ShoesItemId = itemId;
                break;
        }

        PublishChanged(unitId);
        return true;
    }

    public void SetSkill(string unitId, int slot, string skillId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null)
        {
            return;
        }

        if (slot == 0)
        {
            data.Skill01Id = skillId;
        }
        else if (slot == 1)
        {
            data.Skill02Id = skillId;
        }

        PublishChanged(unitId);
    }

    /// <summary>
    /// 配置战斗物品快捷栏。空 itemId 表示清空槽位，直接成功；
    /// 其余分支复用 GetBattleItemAssignResult 的唯一判定。
    /// </summary>
    public BF_BattleItemAssignResult SetBattleItem(string unitId, int slot, string itemId)
    {
        BF_BattleItemAssignResult result = GetBattleItemAssignResult(unitId, slot, itemId);
        if (result != BF_BattleItemAssignResult.Success)
        {
            return result;
        }

        Get(unitId).BattleItemIds[slot] = itemId;
        PublishChanged(unitId);
        return BF_BattleItemAssignResult.Success;
    }

    /// <summary>
    /// 战斗物品配置结果的唯一判定入口：
    /// 无效角色或槽位、无效物品、当前槽重复、同角色跨槽重复和新配置时真实库存为零。
    /// 不限制其他角色配置同一物品。
    /// </summary>
    public BF_BattleItemAssignResult GetBattleItemAssignResult(string unitId, int slot, string itemId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null || slot < 0 || slot >= data.BattleItemIds.Length)
        {
            return BF_BattleItemAssignResult.InvalidTarget;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            return BF_BattleItemAssignResult.Success;
        }

        if (data.BattleItemIds[slot] == itemId)
        {
            return BF_BattleItemAssignResult.CurrentSlotAlreadyAssigned;
        }

        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_ItemConfigSO item = inventory != null ? inventory.GetItem(itemId) : null;
        if (item == null || item.ItemType != BF_ItemType.Consumable)
        {
            return BF_BattleItemAssignResult.InvalidItem;
        }

        for (int i = 0; i < data.BattleItemIds.Length; i++)
        {
            if (i != slot && data.BattleItemIds[i] == itemId)
            {
                return BF_BattleItemAssignResult.AlreadyAssignedToUnit;
            }
        }

        return inventory.GetCount(itemId) > 0
            ? BF_BattleItemAssignResult.Success
            : BF_BattleItemAssignResult.ItemUnavailable;
    }

    /// <summary>
    /// 为角色增加经验并处理升级循环，返回实际生效的经验值。
    /// expRequired 由调用方传入角色的升级经验曲线（满级返回 0）；
    /// 满级后不再累计，CurrentExp 保持 0。
    /// </summary>
    public int AddExp(string unitId, int amount, Func<int, int> expRequired)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null || expRequired == null || amount <= 0)
        {
            return 0;
        }

        int applied = 0;
        int remaining = amount;

        while (remaining > 0)
        {
            int need = expRequired(data.Level);
            if (need <= 0)
            {
                data.CurrentExp = 0;
                break;
            }

            if (data.CurrentExp < 0 || data.CurrentExp >= need)
            {
                data.CurrentExp = Mathf.Clamp(data.CurrentExp, 0, need - 1);
            }

            int gain = Mathf.Min(need - data.CurrentExp, remaining);
            data.CurrentExp += gain;
            remaining -= gain;
            applied += gain;

            if (data.CurrentExp >= need)
            {
                data.Level++;
                data.CurrentExp = 0;
            }
        }

        PublishChanged(unitId);
        return applied;
    }

    public bool CanLoadUnits(IReadOnlyList<BF_UnitRuntimeData> units)
    {
        if (units == null)
        {
            return false;
        }

        HashSet<string> ids = new();
        for (int i = 0; i < units.Count; i++)
        {
            BF_UnitRuntimeData unit = units[i];
            if (unit == null
                || string.IsNullOrEmpty(unit.UnitId)
                || string.IsNullOrEmpty(unit.ConfigId)
                || !ids.Add(unit.UnitId)
                || unit.BattleItemIds == null
                || unit.BattleItemIds.Length != BF_GameConstants.BattleItemSlotCount
                || unit.Level < 1
                || unit.CurrentExp < 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool LoadUnits(IReadOnlyList<BF_UnitRuntimeData> units)
    {
        if (!CanLoadUnits(units))
        {
            return false;
        }

        _units.Clear();
        for (int i = 0; i < units.Count; i++)
        {
            _units.Add(units[i].Clone());
        }

        PublishChanged(string.Empty);
        return true;
    }

    public void Clear(bool publish = true)
    {
        _units.Clear();
        if (publish)
        {
            PublishChanged(string.Empty);
        }
    }

    private void PublishChanged(string unitId)
    {
        GameEventBus.Instance.Publish(new BF_UnitRuntimeChangedEvent(unitId));
    }
}
