using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_SkillSlot : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _infoText;
    [SerializeField] private Image _selection;

    private Action _onClick;

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);
    }

    public void Setup(BF_SkillConfigSO skill, string title, Action onClick)
    {
        _onClick = onClick;
        _icon.sprite = skill != null ? skill.Icon : null;
        _icon.enabled = skill != null && skill.Icon != null;
        _nameText.text = skill != null ? $"{title}\n{skill.DisplayName}" : $"{title}\n空";
        _infoText.text = skill != null ? $"{skill.APCost} AP  ×{skill.Rate:0.##}" : string.Empty;
        _button.interactable = onClick != null;
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (_selection != null)
        {
            _selection.enabled = isSelected;
        }
    }

    private void OnClicked()
    {
        _onClick?.Invoke();
    }
}
