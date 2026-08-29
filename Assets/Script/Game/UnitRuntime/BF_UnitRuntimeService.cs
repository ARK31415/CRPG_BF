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

    public void SetEquipment(string unitId, string itemId)
    {
        BF_UnitRuntimeData data = Get(unitId);
        if (data == null)
        {
            return;
        }

        data.EquipmentItemId = itemId;
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
            if (unit.EquipmentItemId == itemId)
            {
                count++;
            }
        }

        return count;
    }

    private void PublishChanged(string unitId)
    {
        GameEventBus.Instance.Publish(new BF_UnitRuntimeChangedEvent(unitId));
    }
}
