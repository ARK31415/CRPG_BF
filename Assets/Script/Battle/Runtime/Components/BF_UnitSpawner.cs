using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按关卡配置生成敌我单位：玩家方取当前出战阵容，敌方取固定出生点。
/// </summary>
public class BF_UnitSpawner : MonoBehaviour
{
    #region 序列化配置与引用

    [Header("棋盘引用")]
    [SerializeField]
    private BF_BoardManager _board;

    [Header("单位生成")]
    [SerializeField]
    private BF_BattleUnit _unitPrefab;

    [SerializeField]
    private Transform _unitsRoot;

    #endregion

    #region 单位生成

    public List<BF_BattleUnit> SpawnUnits()
    {
        List<BF_BattleUnit> units = new();
        BF_BattleService battle = BF_BattleService.Instance;
        BF_UnitRuntimeService runtime = BF_UnitRuntimeService.Instance;

        if (battle == null || runtime == null || _board == null || _board.LevelConfig == null)
        {
            return units;
        }

        for (int i = 0; i < battle.BattlePartyUnitIds.Count && i < _board.LevelConfig.PlayerSpawns.Count; i++)
        {
            BF_UnitRuntimeData unitData = runtime.Get(battle.BattlePartyUnitIds[i]);
            BF_UnitConfigSO config = unitData != null ? battle.GetUnitConfig(unitData.ConfigId) : null;
            if (unitData == null || config == null)
            {
                continue;
            }

            SpawnUnit(
                units,
                config,
                BF_UnitTeam.Player,
                _board.LevelConfig.PlayerSpawns[i],
                unitData);
        }

        for (int i = 0; i < _board.LevelConfig.FixedSpawns.Count; i++)
        {
            BF_UnitSpawnData data = _board.LevelConfig.FixedSpawns[i];
            if (data == null || data.Unit == null)
            {
                continue;
            }

            SpawnUnit(units, data.Unit, data.Team, data.Pos, null);
        }

        return units;
    }

    private void SpawnUnit(
        List<BF_BattleUnit> units,
        BF_UnitConfigSO config,
        BF_UnitTeam team,
        Vector2Int pos,
        BF_UnitRuntimeData runtimeData)
    {
        BF_BattleUnit unit = Instantiate(_unitPrefab, _unitsRoot);
        unit.name = runtimeData != null ? runtimeData.UnitId : $"{team}_{config.Id}";
        unit.Init(_board, config, team, pos, runtimeData);
        units.Add(unit);
    }

    #endregion
}
