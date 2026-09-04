using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ItemContextMenu : MonoBehaviour
{
    [SerializeField] private Button _primaryButton;
    [SerializeField] private TMP_Text _primaryText;
    [SerializeField] private Button _discardButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _messageText;

    private RectTransform _rect;
    private RectTransform _canvasRect;
    private BF_ItemConfigSO _item;
    private BF_UnitRuntimeData _data;
    private BF_UnitConfigSO _config;
    private int _battleItemSlot;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        _rect = transform as RectTransform;
        Canvas canvas = GetComponentInParent<Canvas>();
        _canvasRect = canvas != null ? canvas.transform as RectTransform : null;
    }

    private void OnEnable()
    {
        _primaryButton.onClick.AddListener(UsePrimary);
        _discardButton.onClick.AddListener(Discard);
        _closeButton.onClick.AddListener(Hide);
    }

    private void OnDisable()
    {
        _primaryButton.onClick.RemoveListener(UsePrimary);
        _discardButton.onClick.RemoveListener(Discard);
        _closeButton.onClick.RemoveListener(Hide);
    }

    public void Show(
        BF_ItemConfigSO item,
        BF_UnitRuntimeData data,
        BF_UnitConfigSO unit,
        int battleItemSlot,
        Vector2 screenPos)
    {
        _item = item;
        _data = data;
        _config = unit;
        _battleItemSlot = battleItemSlot;
        _messageText.text = string.Empty;

        bool isEquipment = item != null && item.ItemType == BF_ItemType.Equipment;
        _primaryText.text = isEquipment ? "装备" : "放入物品栏";
        _primaryButton.interactable = CanUsePrimary();
        _discardButton.interactable = GetAvailableCount() > 0;
        if (!_primaryButton.interactable)
        {
            _messageText.text = item != null && item.ItemType == BF_ItemType.Consumable && battleItemSlot < 0
                ? "先选择角色底部的物品格"
                : "可用数量为 0";
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetPosition(screenPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private bool CanUsePrimary()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_item == null || _data == null || inventory == null || runtime == null)
        {
            return false;
        }

        if (_item.ItemType == BF_ItemType.Consumable && _battleItemSlot < 0)
        {
            return false;
        }

        string current = _item.ItemType == BF_ItemType.Equipment
            ? runtime.GetEquipment(_data.UnitId, _item.EquipmentSlot)
            : _data.BattleItemIds[_battleItemSlot];
        return current == _item.Id || GetAvailableCount() > 0;
    }

    private int GetAvailableCount()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (_item == null || inventory == null || runtime == null)
        {
            return 0;
        }

        return inventory.GetCount(_item.Id) - runtime.GetReservedCount(_item.Id);
    }

    private void UsePrimary()
    {
        if (!CanUsePrimary())
        {
            _messageText.text = _item != null && _item.ItemType == BF_ItemType.Consumable
                ? "先选择角色底部的物品格"
                : "没有可用数量";
            return;
        }

        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (runtime == null)
        {
            return;
        }

        if (_item.ItemType == BF_ItemType.Equipment)
        {
            if (!runtime.SetEquipment(_data.UnitId, _item.EquipmentSlot, _item.Id))
            {
                _messageText.text = "没有可用数量";
                return;
            }
        }
        else
        {
            runtime.SetBattleItem(_data.UnitId, _battleItemSlot, _item.Id);
        }

        Hide();
    }

    private void Discard()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null || GetAvailableCount() <= 0 || !inventory.TryRemove(_item.Id, 1))
        {
            _messageText.text = "物品正在使用，不能丢弃";
            _discardButton.interactable = false;
            return;
        }

        Hide();
    }

    private void SetPosition(Vector2 screenPos)
    {
        if (_rect == null || _canvasRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            screenPos,
            null,
            out Vector2 pos);

        float halfWidth = _rect.rect.width * 0.5f;
        float halfHeight = _rect.rect.height * 0.5f;
        Rect canvas = _canvasRect.rect;
        pos.x = Mathf.Clamp(pos.x, canvas.xMin + halfWidth, canvas.xMax - halfWidth);
        pos.y = Mathf.Clamp(pos.y, canvas.yMin + halfHeight, canvas.yMax - halfHeight);
        _rect.anchoredPosition = pos;
    }
}
