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
    [SerializeField] private Button _deployButton;
    [SerializeField] private TMP_Text _deployButtonText;
    [SerializeField] private BF_ItemSlot[] _equipmentSlots = new BF_ItemSlot[4];
    [SerializeField] private BF_ItemSlot[] _itemSlots = new BF_ItemSlot[4];

    private BF_UnitRuntimeData _data;
    private BF_UnitConfigSO _config;
    private int _selectedBattleItemSlot = -1;
    private IDisposable _unitSubscription;
    private IDisposable _inventorySubscription;

    public int SelectedBattleItemSlot => _selectedBattleItemSlot;

    private void OnEnable()
    {
        _deployButton?.onClick.AddListener(ToggleDeployed);
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
        _deployButton?.onClick.RemoveListener(ToggleDeployed);
    }

    public void ShowUnit(BF_UnitRuntimeData data, BF_UnitConfigSO config)
    {
        _data = data;
        _config = config;
        _selectedBattleItemSlot = -1;

        Refresh();
    }

    public void Refresh()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_config == null || _data == null || inventory == null || runtime == null)
        {
            UpdateDeployButton();
            return;
        }

        int hpBonus = 0;
        int attackBonus = 0;
        int defenseBonus = 0;
        int apBonus = 0;

        for (int i = 0; i < EquipmentSlots.Length; i++)
        {
            BF_EquipmentSlot slot = EquipmentSlots[i];
            BF_ItemConfigSO item = inventory.GetItem(runtime.GetEquipment(_data.UnitId, slot));
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

        BF_UnitStats stats = _config.GetStatsForLevel(_data.Level);
        _unitNameText.text = _config.DisplayName;
        _statsText.text =
            $"Lv  {_data.Level}\n" +
            $"HP   {stats.MaxHP} +{hpBonus}\n" +
            $"ATK  {stats.Attack} +{attackBonus}\n" +
            $"DEF  {stats.Defense} +{defenseBonus}\n" +
            $"AP   {stats.MaxAP} +{apBonus}";
        _portrait.sprite = _config.Portrait;
        _portrait.enabled = _config.Portrait != null;
        _portrait.preserveAspect = true;

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            int slot = i;
            BF_ItemConfigSO item = inventory.GetItem(_data.BattleItemIds[i]);
            int count = item != null ? 1 : 0;
            _itemSlots[i].Setup(
                item,
                count,
                _ => SelectBattleItemSlot(slot),
                emptyText: $"物品 {i + 1}",
                allowEmptyClick: true);
            _itemSlots[i].SetSelected(i == _selectedBattleItemSlot);
        }

        UpdateDeployButton();
    }

    private void ClearEquipment(BF_EquipmentSlot slot)
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_data == null || runtime == null || string.IsNullOrEmpty(runtime.GetEquipment(_data.UnitId, slot)))
        {
            return;
        }

        if (!runtime.SetEquipment(_data.UnitId, slot, string.Empty))
        {
            Debug.Log("仓库已满，无法卸下装备");
        }
    }

    private void SelectBattleItemSlot(int slot)
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_data == null || runtime == null)
        {
            return;
        }

        if (_selectedBattleItemSlot == slot && !string.IsNullOrEmpty(_data.BattleItemIds[slot]))
        {
            runtime.SetBattleItem(_data.UnitId, slot, string.Empty);
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
        if (_data != null && evt.UnitId == _data.UnitId)
        {
            Refresh();
        }
    }

    private void ToggleDeployed()
    {
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_data == null || runtime == null)
        {
            return;
        }

        if (_data.IsDeployed)
        {
            runtime.SetDeployed(_data.UnitId, false);
            return;
        }

        BF_BattleService battleService = BF_BattleService.Instance;
        int limit = battleService != null && battleService.CurrentLevelConfig != null
            ? battleService.CurrentLevelConfig.PlayerSpawns.Count
            : 0;
        if (runtime.DeployedCount < limit)
        {
            runtime.SetDeployed(_data.UnitId, true);
        }
    }

    private void UpdateDeployButton()
    {
        if (_deployButton == null)
        {
            return;
        }

        bool isDeployed = _data != null && _data.IsDeployed;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        BF_BattleService battleService = BF_BattleService.Instance;
        int limit = battleService != null && battleService.CurrentLevelConfig != null
            ? battleService.CurrentLevelConfig.PlayerSpawns.Count
            : 0;
        bool canDeploy = isDeployed || (runtime != null && runtime.DeployedCount < limit);
        _deployButton.interactable = _data != null && canDeploy;
        if (_deployButtonText != null)
        {
            _deployButtonText.text = isDeployed ? "撤下" : "出战";
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
