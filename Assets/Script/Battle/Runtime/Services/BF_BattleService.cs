using System;
using System.Collections.Generic;
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
    private BF_UnitRuntimeService _unitRuntime;

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

        GiveExpReward(level);
    }

    /// <summary>
    /// 胜利经验在全体玩家角色间均分，余数按运行时列表顺序补给；阵亡角色同样获得。
    /// 每次胜利都发放，关卡可以重复刷取。
    /// </summary>
    private void GiveExpReward(BF_LevelConfigSO level)
    {
        if (_unitRuntime == null || level.RewardExp <= 0)
        {
            return;
        }

        LastReward.Exp = level.RewardExp;
        IReadOnlyList<BF_UnitRuntimeData> units = _unitRuntime.Units;
        int count = units.Count;
        if (count == 0)
        {
            return;
        }

        int baseExp = level.RewardExp / count;
        int remainder = level.RewardExp % count;

        for (int i = 0; i < count; i++)
        {
            BF_UnitRuntimeData unit = units[i];
            BF_UnitConfigSO config = FindPlayerConfig(level, unit.UnitId);
            int gain = baseExp + (i < remainder ? 1 : 0);
            if (config == null || gain <= 0)
            {
                continue;
            }

            int oldLevel = unit.Level;
            int applied = _unitRuntime.AddExp(unit.UnitId, gain, config.GetExpRequiredToNextLevel);
            if (applied <= 0)
            {
                continue;
            }

            LastReward.UnitGains.Add(new BF_UnitExpGain
            {
                UnitId = unit.UnitId,
                UnitName = config.DisplayName,
                GainedExp = applied,
                OldLevel = oldLevel,
                NewLevel = unit.Level
            });
        }
    }

    private BF_UnitConfigSO FindPlayerConfig(BF_LevelConfigSO level, string unitId)
    {
        foreach (BF_UnitSpawnData spawn in level.UnitSpawns)
        {
            if (spawn != null && spawn.Unit != null && spawn.Team == BF_UnitTeam.Player && spawn.Unit.Id == unitId)
            {
                return spawn.Unit;
            }
        }

        return null;
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
