using System;
using UnityEngine;

public class BF_BattleService : MonoBehaviour
{
    [SerializeField]
    private BF_SceneLoadManager _sceneLoadManager;

    [SerializeField]
    private BF_GameModeManager _gameModeManager;

    [SerializeField]
    private BF_LevelProgress _levelProgress;

    [SerializeField]
    private BF_InventoryService _inventory;

    [SerializeField]
    private BF_LevelConfigSO[] _levels;

    private IDisposable _resultSubscription;
    private IDisposable _confirmSubscription;
    private bool _isResultActive;

    public int CurrentLevel { get; private set; } = 1;
    public BF_BattleResult LastResult { get; private set; }
    public BF_LevelProgress LevelProgress => _levelProgress;
    public BF_BattleReward LastReward { get; } = new();
    public BF_LevelConfigSO CurrentLevelConfig => GetLevelConfig(CurrentLevel);

    private void OnEnable()
    {
        _resultSubscription = GameEventBus.Instance.Subscribe<BF_BattleResultEvent>(OnBattleResult);
        _confirmSubscription = GameEventBus.Instance.Subscribe<BF_ConfirmBattleResultRequestEvent>(OnConfirmResult);
    }

    private void OnDisable()
    {
        _resultSubscription?.Dispose();
        _confirmSubscription?.Dispose();
        _resultSubscription = null;
        _confirmSubscription = null;
    }

    public void PrepareLevel(int level)
    {
        if (_sceneLoadManager.IsLoading || !_levelProgress.IsUnlocked(level))
        {
            return;
        }

        CurrentLevel = level;
        LastResult = BF_BattleResult.None;
        LastReward.Clear();
        _isResultActive = false;
        _sceneLoadManager.LoadBattlePrepare();
    }

    public void StartPreparedLevel()
    {
        if (_sceneLoadManager.IsLoading || !_levelProgress.IsUnlocked(CurrentLevel))
        {
            return;
        }

        LastResult = BF_BattleResult.None;
        LastReward.Clear();
        _isResultActive = false;
        _sceneLoadManager.LoadBattle(GetBattleAddress(CurrentLevel));
    }

    private void OnBattleResult(BF_BattleResultEvent gameEvent)
    {
        if (_isResultActive || gameEvent.Result == BF_BattleResult.None)
        {
            return;
        }

        LastResult = gameEvent.Result;
        _isResultActive = true;

        if (LastResult == BF_BattleResult.Victory)
        {
            GiveReward();
            _levelProgress.CompleteLevel(CurrentLevel);
        }

        _gameModeManager.SetGameMode(BF_GameMode.Result);
    }

    private void GiveReward()
    {
        BF_LevelConfigSO level = CurrentLevelConfig;
        LastReward.Clear();

        if (level == null || _inventory == null)
        {
            return;
        }

        LastReward.Gold = level.RewardGold;
        _inventory.AddGold(level.RewardGold);

        foreach (BF_RewardItem reward in level.RewardItems)
        {
            if (reward.Item != null && _inventory.TryAdd(reward.Item, reward.Quantity))
            {
                LastReward.Items.Add(new BF_InventoryEntry(reward.Item, reward.Quantity));
            }
        }
    }

    private BF_LevelConfigSO GetLevelConfig(int level)
    {
        int index = level - 1;
        return _levels != null && index >= 0 && index < _levels.Length ? _levels[index] : null;
    }

    private void OnConfirmResult(BF_ConfirmBattleResultRequestEvent gameEvent)
    {
        if (!_isResultActive || _sceneLoadManager.IsLoading)
        {
            return;
        }

        _isResultActive = false;
        _sceneLoadManager.LoadLevelSelect();
    }

    private string GetBattleAddress(int level)
    {
        return level switch
        {
            1 => "Battle_Level_01",
            2 => "Battle_Level_02",
            3 => "Battle_Level_03",
            _ => "Battle_Level_01"
        };
    }
}
