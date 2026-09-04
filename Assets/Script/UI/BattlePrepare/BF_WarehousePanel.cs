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
    private int _selectedSlot = -1;
    private Action<BF_ItemConfigSO, Vector2> _onRightClick;
    private IDisposable _inventorySubscription;

    private void OnEnable()
    {
        _inventorySubscription = GameEventBus.Instance.Subscribe<BF_InventoryChangedEvent>(_ =>
        {
            _selectedSlot = -1;
            _detailPanel.Show(null);
            Refresh();
        });
        BuildSlots();
        Refresh();
    }

    private void OnDisable()
    {
        _inventorySubscription?.Dispose();
        _inventorySubscription = null;
    }

    public void Refresh()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null)
        {
            return;
        }

        _goldText.text = $"金币  {inventory.Gold}";
        _capacityText.text = $"仓库  {inventory.Items.Count} / {inventory.Capacity}";

        for (int i = 0; i < _slots.Count; i++)
        {
            BF_InventoryEntry entry = i < inventory.Items.Count ? inventory.Items[i] : null;
            bool isConsumable = entry != null && entry.Item.ItemType == BF_ItemType.Consumable;
            int count = entry != null && isConsumable ? entry.Quantity : 0;
            int slotIndex = i;
            _slots[i].Setup(
                entry?.Item,
                count,
                item => Select(slotIndex),
                (item, pos) => OpenMenu(slotIndex, pos),
                showCount: isConsumable);
            _slots[i].SetSelected(i == _selectedSlot);
        }

        if (_selectedSlot >= inventory.Items.Count)
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
        BF_InventoryService inventory = BF_InventoryService.Instance;
        int capacity = inventory != null ? inventory.Capacity : 0;

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
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Items.Count)
        {
            return;
        }

        _selectedSlot = slotIndex;
        _detailPanel.Show(inventory.Items[slotIndex].Item);
        Refresh();
    }

    private void OpenMenu(int slotIndex, Vector2 screenPos)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Items.Count)
        {
            return;
        }

        Select(slotIndex);
        _onRightClick?.Invoke(inventory.Items[slotIndex].Item, screenPos);
    }
}
