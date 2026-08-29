using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ItemSlot : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;

    private BF_ItemConfigSO _item;
    private Action<BF_ItemConfigSO> _onClick;

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);
    }

    public void Setup(BF_ItemConfigSO item, int count, Action<BF_ItemConfigSO> onClick)
    {
        _item = item;
        _onClick = onClick;
        _icon.sprite = item != null ? item.Icon : null;
        _icon.enabled = item != null && item.Icon != null;
        _nameText.text = item != null ? item.DisplayName : string.Empty;
        _countText.text = item != null ? $"×{count}" : string.Empty;
        _button.interactable = item != null;
    }

    private void OnClicked()
    {
        _onClick?.Invoke(_item);
    }
}
