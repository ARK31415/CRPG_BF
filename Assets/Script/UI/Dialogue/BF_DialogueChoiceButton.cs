using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话选项按钮 View：显示选项文本并把 ChoiceId 作为用户意图回发。
/// 不查询业务数据、不推进对话。
/// </summary>
public class BF_DialogueChoiceButton : MonoBehaviour
{
    [SerializeField]
    private Button _button;

    [SerializeField]
    private TMP_Text _label;

    private string _choiceId;
    private Action<string> _onSelected;

    public Button Button => _button;
    public string ChoiceId => _choiceId;

    public void Setup(string choiceId, string text, Action<string> onSelected)
    {
        _choiceId = choiceId;
        _onSelected = onSelected;

        if (_label != null)
        {
            _label.text = text;
        }
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(OnClicked);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        _onSelected?.Invoke(_choiceId);
    }
}
