using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_BattlePrepareController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject _warehousePage;
    [SerializeField] private GameObject _skillPage;
    [SerializeField] private GameObject _shopPage;
    [SerializeField] private Button _warehouseButton;
    [SerializeField] private Button _skillButton;
    [SerializeField] private Button _shopButton;

    [Header("Unit")]
    [SerializeField] private TMP_Text _unitNameText;
    [SerializeField] private Button _prevUnitButton;
    [SerializeField] private Button _nextUnitButton;
    [SerializeField] private BF_UnitConfigSO[] _playerUnits;
    [SerializeField] private BF_UnitLoadoutPanel _unitLoadoutPanel;
    [SerializeField] private BF_SkillLoadoutPanel _skillLoadoutPanel;
    [SerializeField] private BF_WarehousePanel _warehousePanel;
    [SerializeField] private BF_ItemContextMenu _itemContextMenu;

    [Header("Flow")]
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _startButton;

    private BF_BattleService _battleService;
    private BF_SceneLoadManager _sceneLoadManager;
    private BF_UnitRuntimeService _runtime;
    private int _unitIndex;

    private BF_UnitConfigSO CurrentUnit => _playerUnits != null && _playerUnits.Length > 0
        ? _playerUnits[_unitIndex]
        : null;

    private void OnEnable()
    {
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _runtime = FindFirstObjectByType<BF_UnitRuntimeService>();

        _warehouseButton.onClick.AddListener(ShowWarehouse);
        _skillButton.onClick.AddListener(ShowSkill);
        _shopButton.onClick.AddListener(ShowShop);
        _prevUnitButton.onClick.AddListener(PreviousUnit);
        _nextUnitButton.onClick.AddListener(NextUnit);
        _backButton.onClick.AddListener(Back);
        _startButton.onClick.AddListener(StartBattle);
        _warehousePanel.SetRightClick(OpenItemMenu);

        InitUnits();
        SelectUnit(0);
        ShowWarehouse();
    }

    private void OnDisable()
    {
        _warehouseButton.onClick.RemoveListener(ShowWarehouse);
        _skillButton.onClick.RemoveListener(ShowSkill);
        _shopButton.onClick.RemoveListener(ShowShop);
        _prevUnitButton.onClick.RemoveListener(PreviousUnit);
        _nextUnitButton.onClick.RemoveListener(NextUnit);
        _backButton.onClick.RemoveListener(Back);
        _startButton.onClick.RemoveListener(StartBattle);
        _warehousePanel.SetRightClick(null);
    }

    private void InitUnits()
    {
        if (_runtime == null || _playerUnits == null)
        {
            return;
        }

        foreach (BF_UnitConfigSO unit in _playerUnits)
        {
            if (unit == null)
            {
                continue;
            }

            _runtime.GetOrCreate(
                unit.Id,
                unit.Skill01 != null ? unit.Skill01.Id : string.Empty,
                unit.Skill02 != null ? unit.Skill02.Id : string.Empty);
        }
    }

    private void SelectUnit(int index)
    {
        if (_playerUnits == null || _playerUnits.Length == 0)
        {
            return;
        }

        _unitIndex = (index + _playerUnits.Length) % _playerUnits.Length;
        _unitNameText.text = CurrentUnit != null ? CurrentUnit.DisplayName : string.Empty;
        _unitLoadoutPanel.ShowUnit(CurrentUnit);
        _skillLoadoutPanel.ShowUnit(CurrentUnit);
        _itemContextMenu.Hide();
    }

    private void PreviousUnit()
    {
        SelectUnit(_unitIndex - 1);
    }

    private void NextUnit()
    {
        SelectUnit(_unitIndex + 1);
    }

    private void ShowWarehouse()
    {
        _warehousePage.SetActive(true);
        _skillPage.SetActive(false);
        _shopPage.SetActive(false);
        _itemContextMenu.Hide();
    }

    private void ShowSkill()
    {
        _warehousePage.SetActive(false);
        _skillPage.SetActive(true);
        _shopPage.SetActive(false);
        _itemContextMenu.Hide();
    }

    private void ShowShop()
    {
        _warehousePage.SetActive(false);
        _skillPage.SetActive(false);
        _shopPage.SetActive(true);
        _itemContextMenu.Hide();
    }

    private void OpenItemMenu(BF_ItemConfigSO item, Vector2 screenPos)
    {
        _itemContextMenu.Show(
            item,
            CurrentUnit,
            _unitLoadoutPanel.SelectedBattleItemSlot,
            screenPos);
    }

    private void Back()
    {
        _backButton.interactable = false;
        _sceneLoadManager.LoadLevelSelect();
    }

    private void StartBattle()
    {
        _startButton.interactable = false;
        _battleService.StartPreparedLevel();
    }
}
