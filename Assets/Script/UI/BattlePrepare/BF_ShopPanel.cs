using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ShopPanel : MonoBehaviour
{
    [SerializeField] private BF_ItemSlot _slotPrefab;
    [SerializeField] private Transform _shopContent;
    [SerializeField] private Transform _inventoryContent;
    [SerializeField] private BF_ItemDetailPanel _detailPanel;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _tradeText;
    [SerializeField] private Button _buyButton;
    [SerializeField] private Button _sellButton;

    private readonly List<BF_ItemSlot> _shopSlots = new();
    private readonly List<BF_ItemSlot> _inventorySlots = new();
    private BF_InventoryService _inventory;
    private BF_ShopService _shop;
    private BF_ItemConfigSO _selected;
    private bool _isShopItem;
    private IDisposable _subscription;

    private void OnEnable()
    {
        _inventory = FindFirstObjectByType<BF_InventoryService>();
        _shop = FindFirstObjectByType<BF_ShopService>();
        _subscription = GameEventBus.Instance.Subscribe<BF_InventoryChangedEvent>(_ => Refresh());
        _buyButton.onClick.AddListener(Buy);
        _sellButton.onClick.AddListener(Sell);
        BuildSlots();
        Refresh();
    }

    private void OnDisable()
    {
        _subscription?.Dispose();
        _subscription = null;
        _buyButton.onClick.RemoveListener(Buy);
        _sellButton.onClick.RemoveListener(Sell);
    }

    private void BuildSlots()
    {
        if (_shop == null || _shop.Config == null)
        {
            return;
        }

        while (_shopSlots.Count < _shop.Config.Items.Count)
        {
            _shopSlots.Add(Instantiate(_slotPrefab, _shopContent));
        }

        while (_inventorySlots.Count < _inventory.Capacity)
        {
            _inventorySlots.Add(Instantiate(_slotPrefab, _inventoryContent));
        }
    }

    private void Refresh()
    {
        if (_shop == null || _shop.Config == null || _inventory == null)
        {
            return;
        }

        _goldText.text = $"金币  {_inventory.Gold}";

        for (int i = 0; i < _shopSlots.Count; i++)
        {
            BF_ItemConfigSO item = _shop.Config.Items[i];
            _shopSlots[i].Setup(item, item.BuyPrice, selected => Select(selected, true));
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            BF_InventoryEntry entry = i < _inventory.Items.Count ? _inventory.Items[i] : null;
            _inventorySlots[i].Setup(entry?.Item, entry?.Quantity ?? 0, selected => Select(selected, false));
        }

        RefreshTrade();
    }

    private void Select(BF_ItemConfigSO item, bool isShopItem)
    {
        _selected = item;
        _isShopItem = isShopItem;
        _detailPanel.Show(item);
        RefreshTrade();
    }

    private void RefreshTrade()
    {
        _buyButton.interactable = _selected != null && _isShopItem;
        _sellButton.interactable = _selected != null && !_isShopItem;
        _tradeText.text = _selected == null
            ? "选择商品或仓库物品"
            : _isShopItem
                ? $"购买价格：{_selected.BuyPrice}"
                : $"出售价格：{_selected.SellPrice}";
    }

    private void Buy()
    {
        _tradeText.text = _shop.TryBuy(_selected) ? "购买成功" : "金币不足或仓库已满";
    }

    private void Sell()
    {
        _tradeText.text = _shop.TrySell(_selected) ? "出售成功" : "没有可出售数量或物品已装备";
    }
}
