using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_SaveSlotView : MonoBehaviour
{
    [SerializeField] private Button _selectButton;
    [SerializeField] private Button _deleteButton;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _summaryText;

    private int _slot;
    private Action<int> _selectAction;
    private Action<int> _deleteAction;

    private void OnEnable()
    {
        _selectButton.onClick.AddListener(Select);
        _deleteButton.onClick.AddListener(Delete);
    }

    private void OnDisable()
    {
        _selectButton.onClick.RemoveListener(Select);
        _deleteButton.onClick.RemoveListener(Delete);
    }

    public void Show(
        BF_SaveSlotInfo info,
        bool canSelect,
        Action<int> selectAction,
        Action<int> deleteAction)
    {
        _slot = info.Slot;
        _selectAction = selectAction;
        _deleteAction = deleteAction;
        _titleText.text = $"存档 {_slot}";
        _selectButton.interactable = canSelect;
        _deleteButton.gameObject.SetActive(info.HasData);

        if (!info.HasData)
        {
            _summaryText.text = "空存档";
        }
        else if (!info.IsValid)
        {
            _summaryText.text = "存档损坏或版本不兼容";
        }
        else
        {
            _summaryText.text = $"最新关卡：{info.HighestUnlockedLevel}    角色：{info.UnitCount}\n{info.SavedAt}";
        }
    }

    private void Select()
    {
        _selectAction?.Invoke(_slot);
    }

    private void Delete()
    {
        _deleteAction?.Invoke(_slot);
    }
}
