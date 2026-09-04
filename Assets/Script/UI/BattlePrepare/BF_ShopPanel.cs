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
    private BF_ShopService _shop;
    private BF_ItemConfigSO _selected;
    private bool _isShopItem;
    private IDisposable _subscription;

    private void OnEnable()
    {
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

        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null)
        {
            return;
        }

        while (_inventorySlots.Count < inventory.Capacity)
        {
            _inventorySlots.Add(Instantiate(_slotPrefab, _inventoryContent));
        }
    }

    private void Refresh()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (_shop == null || _shop.Config == null || inventory == null)
        {
            return;
        }

        _goldText.text = $"金币  {inventory.Gold}";

        for (int i = 0; i < _shopSlots.Count; i++)
        {
            BF_ItemConfigSO item = _shop.Config.Items[i];
            _shopSlots[i].Setup(
                item,
                0,
                selected => Select(selected, true),
                showCount: false);
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            BF_InventoryEntry entry = i < inventory.Items.Count ? inventory.Items[i] : null;
            int count = entry != null ? _shop.GetAvailableCount(entry.Item) : 0;
            bool showCount = entry != null && entry.Item.ItemType == BF_ItemType.Consumable;
            _inventorySlots[i].Setup(
                entry?.Item,
                count,
                selected => Select(selected, false),
                showCount: showCount);
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
        _sellButton.interactable = _selected != null && !_isShopItem && _shop.GetAvailableCount(_selected) > 0;
        _tradeText.text = _selected == null
            ? "选择商品或仓库物品"
            : _isShopItem
                ? $"购买价格：{_selected.BuyPrice}"
                : $"出售价格：{_selected.SellPrice}　可用：{_shop.GetAvailableCount(_selected)}";
    }

    private void Buy()
    {
        _tradeText.text = MapBuyResult(_shop.Buy(_selected));
    }

    private string MapBuyResult(BF_ShopBuyResult result)
    {
        return result switch
        {
            BF_ShopBuyResult.Success => "购买成功",
            BF_ShopBuyResult.InvalidItem => "商品无效",
            BF_ShopBuyResult.NotEnoughGold => "金币不足",
            BF_ShopBuyResult.InventoryFull => "仓库容量已满",
            _ => "该物品堆叠数量已满"
        };
    }

    private void Sell()
    {
        _tradeText.text = _shop.TrySell(_selected) ? "出售成功" : "没有可出售数量或物品已装备";
    }
}
