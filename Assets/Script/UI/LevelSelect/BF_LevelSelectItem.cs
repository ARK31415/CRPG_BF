using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_LevelSelectItem : MonoBehaviour, ISelectHandler
{
    private Button _button;
    private Image _image;
    private GameObject _selectedFrame;
    private TMP_Text _levelText;
    private TMP_Text _stateText;
    private Action<int> _onSelected;
    private int _level;

    public Button Button => _button;
    public int Level => _level;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _selectedFrame = transform.Find("SelectedFrame")?.gameObject;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text.name == "LevelLabel")
            {
                _levelText = text;
            }
            else if (text.name == "StateLabel")
            {
                _stateText = text;
            }
        }
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClicked);
    }

    public void Setup(int level, Sprite stateSprite, string state, bool selected, Action<int> onSelected)
    {
        _level = level;
        _onSelected = onSelected;
        _image.sprite = stateSprite;
        _levelText.text = $"第{level}关";
        _stateText.text = state;
        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        _selectedFrame.SetActive(selected);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _onSelected?.Invoke(_level);
    }

    private void OnClicked()
    {
        _onSelected?.Invoke(_level);
    }
}
