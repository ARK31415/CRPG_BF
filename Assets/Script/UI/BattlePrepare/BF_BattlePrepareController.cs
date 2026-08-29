using UnityEngine;
using UnityEngine.UI;

public class BF_BattlePrepareController : MonoBehaviour
{
    [SerializeField] private GameObject _warehousePage;
    [SerializeField] private GameObject _shopPage;
    [SerializeField] private Button _warehouseButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Button _startButton;

    private BF_BattleService _battleService;
    private BF_SceneLoadManager _sceneLoadManager;

    private void OnEnable()
    {
        _battleService = FindFirstObjectByType<BF_BattleService>();
        _sceneLoadManager = FindFirstObjectByType<BF_SceneLoadManager>();
        _warehouseButton.onClick.AddListener(ShowWarehouse);
        _shopButton.onClick.AddListener(ShowShop);
        _backButton.onClick.AddListener(Back);
        _startButton.onClick.AddListener(StartBattle);
        ShowWarehouse();
    }

    private void OnDisable()
    {
        _warehouseButton.onClick.RemoveListener(ShowWarehouse);
        _shopButton.onClick.RemoveListener(ShowShop);
        _backButton.onClick.RemoveListener(Back);
        _startButton.onClick.RemoveListener(StartBattle);
    }

    private void ShowWarehouse()
    {
        _warehousePage.SetActive(true);
        _shopPage.SetActive(false);
    }

    private void ShowShop()
    {
        _warehousePage.SetActive(false);
        _shopPage.SetActive(true);
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
