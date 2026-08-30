using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BF_WarehousePanel : MonoBehaviour
{
    [SerializeField] private BF_ItemSlot _slotPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private BF_ItemDetailPanel _detailPanel;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _capacityText;

    private readonly List<BF_ItemSlot> _slots = new();
    private BF_InventoryService _inventory;
    private BF_UnitRuntimeService _runtime;
    private int _selectedSlot = -1;
    private Action<BF_ItemConfigSO, Vector2> _onRightClick;
    private IDisposable _inventorySubscription;
    private IDisposable _unitSubscription;

    private void OnEnable()
    {
        _inventory = FindFirstObjectByType<BF_InventoryService>();
        _runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
        _inventorySubscription = GameEventBus.Instance.Subscribe<BF_InventoryChangedEvent>(_ =>
        {
            _selectedSlot = -1;
            _detailPanel.Show(null);
            Refresh();
        });
        _unitSubscription = GameEventBus.Instance.Subscribe<BF_UnitRuntimeChangedEvent>(_ => Refresh());
        BuildSlots();
        Refresh();
    }

    private void OnDisable()
    {
        _inventorySubscription?.Dispose();
        _unitSubscription?.Dispose();
        _inventorySubscription = null;
        _unitSubscription = null;
    }

    public void Refresh()
    {
        if (_inventory == null)
        {
            return;
        }

        _goldText.text = $"金币  {_inventory.Gold}";
        _capacityText.text = $"仓库  {_inventory.Items.Count} / {_inventory.Capacity}";

        for (int i = 0; i < _slots.Count; i++)
        {
            BF_InventoryEntry entry = i < _inventory.Items.Count ? _inventory.Items[i] : null;
            bool isConsumable = entry != null && entry.Item.ItemType == BF_ItemType.Consumable;
            int count = entry != null ? entry.Quantity - _runtime.GetReservedCount(entry.Item.Id) : 0;
            int slotIndex = i;
            _slots[i].Setup(
                entry?.Item,
                count,
                item => Select(slotIndex),
                (item, pos) => OpenMenu(slotIndex, pos),
                showCount: isConsumable);
            _slots[i].SetSelected(i == _selectedSlot);
        }

        if (_selectedSlot >= _inventory.Items.Count)
        {
            _selectedSlot = -1;
            _detailPanel.Show(null);
        }
    }

    public void SetRightClick(Action<BF_ItemConfigSO, Vector2> onRightClick)
    {
        _onRightClick = onRightClick;
    }

    private void BuildSlots()
    {
        int capacity = _inventory != null ? _inventory.Capacity : 0;

        while (_slots.Count < capacity)
        {
            _slots.Add(Instantiate(_slotPrefab, _content));
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].gameObject.SetActive(i < capacity);
        }
    }

    private void Select(int slotIndex)
    {
        _selectedSlot = slotIndex;
        _detailPanel.Show(_inventory.Items[slotIndex].Item);
        Refresh();
    }

    private void OpenMenu(int slotIndex, Vector2 screenPos)
    {
        if (slotIndex >= _inventory.Items.Count)
        {
            return;
        }

        Select(slotIndex);
        _onRightClick?.Invoke(_inventory.Items[slotIndex].Item, screenPos);
    }
}
