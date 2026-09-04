using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10)]
public class BF_UIManager : Singleton<BF_UIManager>
{
    [SerializeField]
    private GameObject _battleUI;

    [SerializeField]
    private GameObject _resultUI;

    [SerializeField]
    private GameObject _pauseUI;

    [SerializeField]
    private BF_SettingsPanel _settingsPanel;

    [SerializeField]
    private BF_BattleHUD _battleHUD;

    [SerializeField]
    private BF_TutorialPanel _tutorialPanel;

    private IDisposable _gameModeSubscription;

    private void OnEnable()
    {
        if (Instance != this)
        {
            return;
        }

        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Loading;

        Refresh(gameMode, BF_GameMode.None);
    }

    private void OnDisable()
    {
        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
    }

    private void OnGameModeChanged(BF_GameModeChangedEvent gameEvent)
    {
        Refresh(gameEvent.CurrentMode, gameEvent.PreviousMode);
    }

    private void Update()
    {
        if (BF_InputManager.Instance == null || !BF_InputManager.Instance.PausePressed)
        {
            return;
        }

        _tutorialPanel ??= FindFirstObjectByType<BF_TutorialPanel>();
        if (_tutorialPanel != null && _tutorialPanel.IsOpen)
        {
            _tutorialPanel.Close();
            return;
        }

        if (_settingsPanel != null && _settingsPanel.IsOpen)
        {
            _settingsPanel.Close();
            return;
        }

        BF_ItemContextMenu itemMenu = FindFirstObjectByType<BF_ItemContextMenu>();
        if (itemMenu != null && itemMenu.IsOpen)
        {
            itemMenu.Hide();
            return;
        }

        BF_MenuController menu = FindFirstObjectByType<BF_MenuController>();
        if (menu != null && menu.IsConfirmOpen)
        {
            menu.CloseConfirm();
            return;
        }

        BF_PausePanel pausePanel = FindFirstObjectByType<BF_PausePanel>();
        if (pausePanel != null && pausePanel.IsExitConfirmOpen)
        {
            pausePanel.CloseExitConfirm();
            return;
        }

        BF_GameModeManager gameModeManager = BF_GameModeManager.Instance;
        if (gameModeManager == null)
        {
            return;
        }

        BF_SceneLoadManager sceneLoad = BF_SceneLoadManager.Instance;
        if (sceneLoad != null && sceneLoad.IsLoading)
        {
            return;
        }

        if (gameModeManager.CurrentGameMode == BF_GameMode.Battle)
        {
            gameModeManager.PauseBattle();
        }
        else if (gameModeManager.CurrentGameMode == BF_GameMode.Paused)
        {
            gameModeManager.ResumeBattle();
        }
        else if (gameModeManager.CurrentGameMode == BF_GameMode.Menu)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "BattlePrepare")
            {
                sceneLoad?.LoadLevelSelect();
            }
            else if (sceneName == "LevelSelect")
            {
                sceneLoad?.LoadMenu();
            }
        }
    }

    public void OpenSettingsPanel()
    {
        _settingsPanel?.Open();
    }

    public void CloseSettingsPanel()
    {
        _settingsPanel?.Close();
    }

    private void Refresh(BF_GameMode gameMode, BF_GameMode previousMode)
    {
        if (gameMode == BF_GameMode.Battle
            && previousMode != BF_GameMode.Paused)
        {
            _battleHUD?.ResetView();
        }

        _battleUI?.SetActive(gameMode == BF_GameMode.Battle || gameMode == BF_GameMode.Paused);
        _resultUI?.SetActive(gameMode == BF_GameMode.Result);
        _pauseUI?.SetActive(gameMode == BF_GameMode.Paused);

        if (gameMode == BF_GameMode.Loading || gameMode == BF_GameMode.Result)
        {
            _settingsPanel?.Close();
        }
    }
}
