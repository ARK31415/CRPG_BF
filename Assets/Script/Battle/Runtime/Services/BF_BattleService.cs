using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-60)]
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
    private BF_SaveService _saveService;

    [SerializeField]
    private BF_LevelConfigSO[] _levels;

    [Header("Unit Config")]
    [SerializeField]
    private BF_UnitConfigSO[] _unitCatalog;

    [SerializeField]
    private BF_UnitConfigSO[] _initialUnits;

    private IDisposable _resultSubscription;
    private IDisposable _confirmSubscription;
    private readonly List<string> _battlePartyUnitIds = new();
    private bool _isResultActive;

    public int CurrentLevel { get; private set; } = 1;
    public BF_BattleResult LastResult { get; private set; }
    public BF_LevelProgress LevelProgress => _levelProgress;
    public BF_BattleReward LastReward { get; } = new();
    public BF_LevelConfigSO CurrentLevelConfig => GetLevelConfig(CurrentLevel);
    public IReadOnlyList<string> BattlePartyUnitIds => _battlePartyUnitIds;

    private void Awake()
    {
        CreateInitialUnits();
    }

    public void CreateInitialUnits()
    {
        if (_unitRuntime == null || _unitRuntime.Units.Count > 0 || _initialUnits == null)
        {
            return;
        }

        for (int i = 0; i < _initialUnits.Length; i++)
        {
            BF_UnitConfigSO config = _initialUnits[i];
            if (config == null)
            {
                continue;
            }

            string skill01 = config.Skill01 != null ? config.Skill01.Id : string.Empty;
            string skill02 = config.Skill02 != null ? config.Skill02.Id : string.Empty;
            _unitRuntime.AddUnit(config.Id, skill01, skill02, true);
        }
    }

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
        _battlePartyUnitIds.Clear();
        _isResultActive = false;
        _sceneLoadManager.LoadBattlePrepare();
    }

    public void StartPreparedLevel()
    {
        BF_LevelConfigSO level = CurrentLevelConfig;
        if (_sceneLoadManager.IsLoading
            || !_levelProgress.IsUnlocked(CurrentLevel)
            || !TryBuildBattleParty(level))
        {
            Debug.LogWarning("Cannot start battle: deployed roster is invalid for this level.", this);
            return;
        }

        LastResult = BF_BattleResult.None;
        LastReward.Clear();
        _isResultActive = false;
        _sceneLoadManager.LoadBattle(GetBattleAddress(CurrentLevel));
    }

    public BF_UnitConfigSO GetUnitConfig(string configId)
    {
        if (string.IsNullOrEmpty(configId))
        {
            return null;
        }

        BF_UnitConfigSO config = FindConfig(_unitCatalog, configId);
        if (config != null)
        {
            return config;
        }

        config = FindConfig(_initialUnits, configId);
        if (config != null)
        {
            return config;
        }

        if (_levels == null)
        {
            return null;
        }

        for (int i = 0; i < _levels.Length; i++)
        {
            BF_LevelConfigSO level = _levels[i];
            if (level == null)
            {
                continue;
            }

            if (level.RewardUnit != null && level.RewardUnit.Id == configId)
            {
                return level.RewardUnit;
            }

            for (int j = 0; j < level.FixedSpawns.Count; j++)
            {
                BF_UnitSpawnData spawn = level.FixedSpawns[j];
                if (spawn != null && spawn.Unit != null && spawn.Unit.Id == configId)
                {
                    return spawn.Unit;
                }
            }
        }

        return null;
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

        if (_saveService != null && _saveService.CurrentSlot > 0)
        {
            _saveService.Save();
        }

        _gameModeManager.SetGameMode(BF_GameMode.Result);
    }

    private void GiveReward()
    {
        BF_LevelConfigSO level = CurrentLevelConfig;
        LastReward.Clear();

        if (level == null)
        {
            return;
        }

        LastReward.Gold = level.RewardGold;
        if (_inventory != null)
        {
            _inventory.AddGold(level.RewardGold);

            foreach (BF_RewardItem reward in level.RewardItems)
            {
                if (reward.Item != null && _inventory.TryAdd(reward.Item, reward.Quantity))
                {
                    LastReward.Items.Add(new BF_InventoryEntry(reward.Item, reward.Quantity));
                }
            }
        }

        GiveExpReward(level);
        GiveUnitReward(level);
    }

    /// <summary>
    /// 胜利经验在当前关卡玩家阵容间均分，余数按阵容配置顺序补给；阵亡角色同样获得。
    /// 每次胜利都发放，关卡可以重复刷取。
    /// </summary>
    private void GiveExpReward(BF_LevelConfigSO level)
    {
        if (_unitRuntime == null || level.RewardExp <= 0)
        {
            return;
        }

        LastReward.Exp = level.RewardExp;
        int count = _battlePartyUnitIds.Count;
        if (count == 0)
        {
            return;
        }

        int baseExp = level.RewardExp / count;
        int remainder = level.RewardExp % count;

        for (int i = 0; i < count; i++)
        {
            int gain = baseExp + (i < remainder ? 1 : 0);
            BF_UnitRuntimeData unit = _unitRuntime.Get(_battlePartyUnitIds[i]);
            BF_UnitConfigSO config = unit != null ? GetUnitConfig(unit.ConfigId) : null;
            if (unit == null || config == null || gain <= 0)
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

    private void GiveUnitReward(BF_LevelConfigSO level)
    {
        if (_unitRuntime == null || level.RewardUnit == null)
        {
            return;
        }

        bool isFirstClear = !_levelProgress.IsCompleted(CurrentLevel);
        if (level.RewardUnitMode == BF_UnitRewardMode.FirstClearOnly && !isFirstClear)
        {
            return;
        }

        string skill01 = level.RewardUnit.Skill01 != null ? level.RewardUnit.Skill01.Id : string.Empty;
        string skill02 = level.RewardUnit.Skill02 != null ? level.RewardUnit.Skill02.Id : string.Empty;
        BF_UnitRuntimeData unit = _unitRuntime.AddUnit(
            level.RewardUnit.Id,
            skill01,
            skill02);

        if (unit != null)
        {
            LastReward.NewUnits.Add(new BF_NewUnitReward
            {
                UnitId = unit.UnitId,
                UnitName = level.RewardUnit.DisplayName,
                ConfigId = level.RewardUnit.Id
            });
        }
    }

    private bool TryBuildBattleParty(BF_LevelConfigSO level)
    {
        _battlePartyUnitIds.Clear();
        if (level == null || _unitRuntime == null)
        {
            return false;
        }

        List<BF_UnitRuntimeData> deployed = _unitRuntime.GetDeployedUnits();
        for (int i = 0; i < deployed.Count; i++)
        {
            _battlePartyUnitIds.Add(deployed[i].UnitId);
        }

        return _battlePartyUnitIds.Count > 0
            && _battlePartyUnitIds.Count <= level.PlayerSpawns.Count;
    }

    private BF_UnitConfigSO FindConfig(BF_UnitConfigSO[] configs, string configId)
    {
        if (configs == null)
        {
            return null;
        }

        for (int i = 0; i < configs.Length; i++)
        {
            if (configs[i] != null && configs[i].Id == configId)
            {
                return configs[i];
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
