using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ItemContextMenu : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("操作按钮")]
    [SerializeField]
    private Button _primaryButton;

    [SerializeField]
    private TMP_Text _primaryText;

    [SerializeField]
    private Button _discardButton;

    [SerializeField]
    private Button _closeButton;

    [Header("结果提示")]
    [SerializeField]
    private TMP_Text _messageText;

    #endregion

    #region 运行时数据

    // 布局缓存
    private RectTransform _rect;
    private RectTransform _canvasRect;

    // 绑定上下文
    private BF_ItemConfigSO _item;
    private BF_UnitRuntimeData _data;
    private BF_UnitConfigSO _config;
    private int _battleItemSlot;

    #endregion

    #region 对外接口

    // 面板开关状态
    public bool IsOpen => gameObject.activeSelf;

    #endregion

    #region 生命周期

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

    #endregion

    #region 显示定位

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
        string blockedReason = GetPrimaryBlockedReason();
        _primaryButton.interactable = blockedReason == null;
        _discardButton.interactable = GetAvailableCount() > 0;
        if (blockedReason != null)
        {
            _messageText.text = blockedReason;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        SetPosition(screenPos);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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

    #endregion

    #region 主操作

    private void UsePrimary()
    {
        string blockedReason = GetPrimaryBlockedReason();
        if (blockedReason != null)
        {
            _messageText.text = blockedReason;
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
        else if (runtime.SetBattleItem(_data.UnitId, _battleItemSlot, _item.Id) != BF_BattleItemAssignResult.Success)
        {
            // GetPrimaryBlockedReason 已使用同一判定，这里只做防御。
            return;
        }

        Hide();
    }

    #endregion

    #region 丢弃

    private void Discard()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        if (inventory == null || GetAvailableCount() <= 0 || !inventory.TryRemove(_item.Id, 1))
        {
            _messageText.text = "当前没有该物品";
            _discardButton.interactable = false;
            return;
        }

        Hide();
    }

    #endregion

    #region 判定与失败提示

    /// <summary>
    /// 主操作失败原因；null 表示可执行。装备保持原有判定，
    /// 消耗品消费 BF_UnitRuntimeService 的唯一配置结果，UI 只映射文案。
    /// </summary>
    private string GetPrimaryBlockedReason()
    {
        if (_item == null || _data == null)
        {
            return "无法配置该物品";
        }

        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;
        if (runtime == null)
        {
            return "无法配置该物品";
        }

        if (_item.ItemType == BF_ItemType.Equipment)
        {
            return runtime.GetEquipment(_data.UnitId, _item.EquipmentSlot) == _item.Id
                || GetAvailableCount() > 0
                ? null
                : "可用数量为 0";
        }

        if (_battleItemSlot < 0)
        {
            return "先选择角色底部的物品格";
        }

        return MapAssignResult(runtime.GetBattleItemAssignResult(_data.UnitId, _battleItemSlot, _item.Id));
    }

    private string MapAssignResult(BF_BattleItemAssignResult result)
    {
        return result switch
        {
            BF_BattleItemAssignResult.Success => null,
            BF_BattleItemAssignResult.CurrentSlotAlreadyAssigned => "当前格已配置此物品",
            BF_BattleItemAssignResult.AlreadyAssignedToUnit => "该角色已配置此物品",
            BF_BattleItemAssignResult.ItemUnavailable => "当前没有该物品",
            _ => "无法配置该物品"
        };
    }

    private int GetAvailableCount()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        return _item != null && inventory != null ? inventory.GetCount(_item.Id) : 0;
    }

    #endregion
}
