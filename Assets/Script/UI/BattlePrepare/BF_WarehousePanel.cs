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
    private IDisposable _subscription;

    private void OnEnable()
    {
        _inventory = FindFirstObjectByType<BF_InventoryService>();
        _subscription = GameEventBus.Instance.Subscribe<BF_InventoryChangedEvent>(_ => Refresh());
        BuildSlots();
        Refresh();
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
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
            _slots[i].Setup(entry?.Item, entry?.Quantity ?? 0, _detailPanel.Show);
        }
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
}
