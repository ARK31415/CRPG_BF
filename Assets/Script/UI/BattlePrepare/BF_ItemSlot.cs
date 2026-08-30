using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Image _selection;

    private BF_ItemConfigSO _item;
    private Action<BF_ItemConfigSO> _onLeftClick;
    private Action<BF_ItemConfigSO, Vector2> _onRightClick;

    public void Setup(
        BF_ItemConfigSO item,
        int count,
        Action<BF_ItemConfigSO> onLeftClick,
        Action<BF_ItemConfigSO, Vector2> onRightClick = null,
        string emptyText = "",
        bool allowEmptyClick = false,
        bool showCount = true)
    {
        _item = item;
        _onLeftClick = onLeftClick;
        _onRightClick = onRightClick;
        _icon.sprite = item != null ? item.Icon : null;
        _icon.enabled = item != null && item.Icon != null;
        _nameText.text = item != null ? item.DisplayName : emptyText;
        _countText.text = item != null && showCount ? $"×{Mathf.Max(0, count)}" : string.Empty;
        _button.interactable = item != null || allowEmptyClick;
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_selection != null)
        {
            _selection.enabled = isSelected;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button != null && !_button.interactable)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _onRightClick?.Invoke(_item, eventData.position);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            _onLeftClick?.Invoke(_item);
        }
    }
}
