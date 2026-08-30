using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent 场景中的玩家角色整备数据入口。
/// </summary>
[DefaultExecutionOrder(-70)]
public class BF_UnitRuntimeService : MonoBehaviour
{
    private readonly List<BF_UnitRuntimeData> _units = new();

    public IReadOnlyList<BF_UnitRuntimeData> Units => _units;

    public BF_UnitRuntimeData GetOrCreate(string unitId, string skill01Id, string skill02Id)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data != null)
        {
            return data;
        }

        data = new BF_UnitRuntimeData(unitId, skill01Id, skill02Id);
        _units.Add(data);
        PublishChanged(unitId);
        return data;
    }

    public BF_UnitRuntimeData Get(string unitId)
    {
        return _units.Find(unit => unit.UnitId == unitId);
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

    public void SetEquipment(string unitId, BF_EquipmentSlot slot, string itemId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null)
        {
            return;
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

    public void SetBattleItem(string unitId, int slot, string itemId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null || slot < 0 || slot >= data.BattleItemIds.Length)
        {
            return;
        }

        data.BattleItemIds[slot] = itemId;
        PublishChanged(unitId);
    }

    public int GetEquippedCount(string itemId)
    {
        int count = 0;
        foreach (BF_UnitRuntimeData unit in _units)
        {
            count += unit.WeaponItemId == itemId ? 1 : 0;
            count += unit.HeadItemId == itemId ? 1 : 0;
            count += unit.ArmorItemId == itemId ? 1 : 0;
            count += unit.ShoesItemId == itemId ? 1 : 0;
        }

        return count;
    }

    public int GetReservedCount(string itemId)
    {
        int count = GetEquippedCount(itemId);
        foreach (BF_UnitRuntimeData unit in _units)
        {
            for (int i = 0; i < unit.BattleItemIds.Length; i++)
            {
                count += unit.BattleItemIds[i] == itemId ? 1 : 0;
            }
        }

        return count;
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
            if (need == 0)
            {
                data.CurrentExp = 0;
                break;
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

    private void PublishChanged(string unitId)
    {
        GameEventBus.Instance.Publish(new BF_UnitRuntimeChangedEvent(unitId));
    }
}
