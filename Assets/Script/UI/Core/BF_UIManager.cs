using System;
using UnityEngine;

public class BF_UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _battleUI;

    [SerializeField]
    private GameObject _resultUI;

    private IDisposable _gameModeSubscription;

    private void OnEnable()
    {
        _gameModeSubscription = GameEventBus.Instance.Subscribe<BF_GameModeChangedEvent>(OnGameModeChanged);

        BF_GameMode gameMode = BF_GameModeManager.Instance != null
            ? BF_GameModeManager.Instance.CurrentGameMode
            : BF_GameMode.Loading;

        Refresh(gameMode);
    }

    private void OnDisable()
    {
        _gameModeSubscription?.Dispose();
        _gameModeSubscription = null;
    }

    private void OnGameModeChanged(BF_GameModeChangedEvent gameEvent)
    {
        Refresh(gameEvent.CurrentMode);
    }

    private void Refresh(BF_GameMode gameMode)
    {
        _battleUI.SetActive(gameMode == BF_GameMode.Battle);
        _resultUI.SetActive(gameMode == BF_GameMode.Result);
    }
}
