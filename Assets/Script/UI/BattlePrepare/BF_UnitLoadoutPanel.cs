using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_UnitLoadoutPanel : MonoBehaviour
{
    private static readonly BF_EquipmentSlot[] EquipmentSlots =
    {
        BF_EquipmentSlot.Weapon,
        BF_EquipmentSlot.Head,
        BF_EquipmentSlot.Armor,
        BF_EquipmentSlot.Shoes
    };

    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private Image _portrait;
    [SerializeField] private BF_ItemSlot[] _equipmentSlots = new BF_ItemSlot[4];
    [SerializeField] private BF_ItemSlot[] _itemSlots = new BF_ItemSlot[4];

    private BF_InventoryService _inventory;
    private BF_UnitRuntimeService _runtime;
    private BF_UnitConfigSO _unit;
    private int _selectedBattleItemSlot = -1;
    private IDisposable _unitSubscription;
    private IDisposable _inventorySubscription;

    public int SelectedBattleItemSlot => _selectedBattleItemSlot;

    private BF_UnitRuntimeData Data => _unit != null && _runtime != null
        ? _runtime.Get(_unit.Id)
        : null;

    private void OnEnable()
    {
        CacheServices();
        if (GameEventBus.Instance != null)
        {
            _unitSubscription = GameEventBus.Instance.Subscribe<BF_UnitRuntimeChangedEvent>(OnUnitChanged);
            _inventorySubscription = GameEventBus.Instance.Subscribe<BF_InventoryChangedEvent>(_ => Refresh());
        }

        Refresh();
    }

    private void OnDisable()
    {
        _unitSubscription?.Dispose();
        _inventorySubscription?.Dispose();
        _unitSubscription = null;
        _inventorySubscription = null;
    }

    public void ShowUnit(BF_UnitConfigSO unit)
    {
        CacheServices();
        _unit = unit;
        _selectedBattleItemSlot = -1;

        if (_unit != null && _runtime != null)
        {
            _runtime.GetOrCreate(
                _unit.Id,
                _unit.Skill01 != null ? _unit.Skill01.Id : string.Empty,
                _unit.Skill02 != null ? _unit.Skill02.Id : string.Empty);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_unit == null || Data == null || _inventory == null)
        {
            return;
        }

        int hpBonus = 0;
        int attackBonus = 0;
        int defenseBonus = 0;
        int apBonus = 0;

        for (int i = 0; i < EquipmentSlots.Length; i++)
        {
            BF_EquipmentSlot slot = EquipmentSlots[i];
            BF_ItemConfigSO item = _inventory.GetItem(_runtime.GetEquipment(_unit.Id, slot));
            if (item != null)
            {
                hpBonus += item.MaxHPBonus;
                attackBonus += item.AttackBonus;
                defenseBonus += item.DefenseBonus;
                apBonus += item.MaxAPBonus;
            }

            if (i < _equipmentSlots.Length && _equipmentSlots[i] != null)
            {
                BF_EquipmentSlot clickedSlot = slot;
                _equipmentSlots[i].Setup(
                    item,
                    0,
                    _ => ClearEquipment(clickedSlot),
                    emptyText: GetSlotName(slot),
                    showCount: false);
            }
        }

        BF_UnitStats stats = _unit.GetStatsForLevel(Data.Level);
        _unitNameText.text = _unit.DisplayName;
        _statsText.text =
            $"Lv  {Data.Level}\n" +
            $"HP   {stats.MaxHP} +{hpBonus}\n" +
            $"ATK  {stats.Attack} +{attackBonus}\n" +
            $"DEF  {stats.Defense} +{defenseBonus}\n" +
            $"AP   {stats.MaxAP} +{apBonus}";
        _portrait.sprite = _unit.Portrait;
        _portrait.enabled = _unit.Portrait != null;
        _portrait.preserveAspect = true;

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            int slot = i;
            BF_ItemConfigSO item = _inventory.GetItem(Data.BattleItemIds[i]);
            int count = item != null ? _inventory.GetCount(item.Id) : 0;
            _itemSlots[i].Setup(
                item,
                count,
                _ => SelectBattleItemSlot(slot),
                emptyText: $"物品 {i + 1}",
                allowEmptyClick: true);
            _itemSlots[i].SetSelected(i == _selectedBattleItemSlot);
        }
    }

    private void CacheServices()
    {
        _inventory ??= FindFirstObjectByType<BF_InventoryService>();
        _runtime ??= FindFirstObjectByType<BF_UnitRuntimeService>();
    }

    private void ClearEquipment(BF_EquipmentSlot slot)
    {
        if (_unit == null || string.IsNullOrEmpty(_runtime.GetEquipment(_unit.Id, slot)))
        {
            return;
        }

        if (!_runtime.SetEquipment(_unit.Id, slot, string.Empty))
        {
            Debug.Log("仓库已满，无法卸下装备");
        }
    }

    private void SelectBattleItemSlot(int slot)
    {
        if (Data == null)
        {
            return;
        }

        if (_selectedBattleItemSlot == slot && !string.IsNullOrEmpty(Data.BattleItemIds[slot]))
        {
            _runtime.SetBattleItem(_unit.Id, slot, string.Empty);
            _selectedBattleItemSlot = -1;
        }
        else
        {
            _selectedBattleItemSlot = slot;
            Refresh();
        }
    }

    private void OnUnitChanged(BF_UnitRuntimeChangedEvent evt)
    {
        if (_unit != null && evt.UnitId == _unit.Id)
        {
            Refresh();
        }
    }

    private string GetSlotName(BF_EquipmentSlot slot)
    {
        return slot switch
        {
            BF_EquipmentSlot.Weapon => "武器",
            BF_EquipmentSlot.Head => "头部",
            BF_EquipmentSlot.Armor => "护甲",
            BF_EquipmentSlot.Shoes => "鞋",
            _ => "装备"
        };
    }
}
