using System.Collections.Generic;
using UnityEngine;

public class BF_UnitSpawner : MonoBehaviour
{
    [SerializeField]
    private BF_BoardManager _board;

    [SerializeField]
    private BF_BattleUnit _unitPrefab;

    [SerializeField]
    private Transform _unitsRoot;

    public List<BF_BattleUnit> SpawnUnits()
    {
        List<BF_BattleUnit> units = new();
        BF_BattleService battle = FindFirstObjectByType<BF_BattleService>();
        BF_UnitRuntimeService runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
        BF_InventoryService inventory = FindFirstObjectByType<BF_InventoryService>();

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
                unitData,
                inventory);
        }

        for (int i = 0; i < _board.LevelConfig.FixedSpawns.Count; i++)
        {
            BF_UnitSpawnData data = _board.LevelConfig.FixedSpawns[i];
            if (data == null || data.Unit == null)
            {
                continue;
            }

            SpawnUnit(units, data.Unit, data.Team, data.Pos, null, inventory);
        }

        return units;
    }

    private void SpawnUnit(
        List<BF_BattleUnit> units,
        BF_UnitConfigSO config,
        BF_UnitTeam team,
        Vector2Int pos,
        BF_UnitRuntimeData runtimeData,
        BF_InventoryService inventory)
    {
        BF_BattleUnit unit = Instantiate(_unitPrefab, _unitsRoot);
        unit.name = runtimeData != null ? runtimeData.UnitId : $"{team}_{config.Id}";
        unit.Init(_board, config, team, pos, runtimeData, inventory);
        units.Add(unit);
    }
}
