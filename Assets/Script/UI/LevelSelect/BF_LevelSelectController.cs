using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BF_LevelSelectController : MonoBehaviour
{
    [SerializeField]
    private int[] _levels = { 1, 2, 3 };

    [SerializeField]
    private BF_LevelSelectItem _itemPrefab;

    [SerializeField]
    private RectTransform _content;

    [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField]
    private Sprite _unlockedSprite;

    [SerializeField]
    private Sprite _completedSprite;

    [SerializeField]
    private Button _backButton;

    [SerializeField]
    private Button _enterButton;

    [SerializeField]
    private TMP_Text _levelInfoText;

    private BF_SceneLoadManager _sceneLoadManager;
    private BF_BattleService _battleService;
    private BF_LevelProgress _levelProgress;
    private int _selectedLevel = 1;
    private readonly List<BF_LevelSelectItem> _items = new();

    private void OnEnable()
    {
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _levelProgress = _battleService.LevelProgress;

        _backButton.onClick.AddListener(OnBackClicked);
        _enterButton.onClick.AddListener(OnEnterClicked);
        RefreshList();
    }

    private void OnDisable()
    {
        _backButton.onClick.RemoveListener(OnBackClicked);
        _enterButton.onClick.RemoveListener(OnEnterClicked);
        ClearItems();
    }

    private void OnBackClicked()
    {
        _backButton.interactable = false;
        _sceneLoadManager.LoadMenu();
    }

    private void OnEnterClicked()
    {
        _enterButton.interactable = false;
        _battleService.StartLevel(_selectedLevel);
    }

    private void OnItemSelected(int level)
    {
        if (!_levelProgress.IsUnlocked(level))
        {
            return;
        }

        _selectedLevel = level;
        RefreshSelection();
        FocusSelectedItem(false);
    }

    private void RefreshList()
    {
        ClearItems();
        _selectedLevel = GetDefaultLevel();

        foreach (int level in _levels)
        {
            if (!_levelProgress.IsUnlocked(level))
            {
                continue;
            }

            BF_LevelSelectItem item = Instantiate(_itemPrefab, _content);
            item.Setup(level, GetStateSprite(level), GetStateText(level), level == _selectedLevel, OnItemSelected);
            _items.Add(item);
        }

        BuildNavigation();
        RefreshSelection();
        FocusSelectedItem();
    }

    private void ClearItems()
    {
        foreach (BF_LevelSelectItem item in _items)
        {
            Destroy(item.gameObject);
        }

        _items.Clear();
    }

    private int GetDefaultLevel()
    {
        int level = 1;
        foreach (int configuredLevel in _levels)
        {
            if (_levelProgress.IsUnlocked(configuredLevel) && configuredLevel > level)
            {
                level = configuredLevel;
            }
        }

        return _levelProgress.IsUnlocked(level) ? level : _levelProgress.HighestUnlockedLevel;
    }

    private Sprite GetStateSprite(int level)
    {
        return _levelProgress.IsCompleted(level) ? _completedSprite : _unlockedSprite;
    }

    private string GetStateText(int level)
    {
        return _levelProgress.IsCompleted(level) ? "已完成" : "已解锁";
    }

    private void BuildNavigation()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            Navigation navigation = _items[i].Button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = i > 0 ? _items[i - 1].Button : null;
            navigation.selectOnRight = i < _items.Count - 1 ? _items[i + 1].Button : null;
            _items[i].Button.navigation = navigation;
        }
    }

    private void RefreshSelection()
    {
        foreach (BF_LevelSelectItem item in _items)
        {
            item.SetSelected(item.Level == _selectedLevel);
        }

        string state = _levelProgress.IsCompleted(_selectedLevel) ? "已完成" : "已解锁";
        _levelInfoText.text = $"第{_selectedLevel}关\n状态：{state} / 当前选择";
        _enterButton.interactable = true;
    }

    private void FocusSelectedItem(bool selectItem = true)
    {
        BF_LevelSelectItem selectedItem = _items.Find(item => item.Level == _selectedLevel);
        if (selectedItem == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        if (_items.Count > 1)
        {
            int index = _items.IndexOf(selectedItem);
            _scrollRect.horizontalNormalizedPosition = (float)index / (_items.Count - 1);
        }

        if (selectItem && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(selectedItem.gameObject);
        }
    }
}
