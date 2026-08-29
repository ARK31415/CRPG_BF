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
        BF_UnitRuntimeService runtime = FindFirstObjectByType<BF_UnitRuntimeService>();
        BF_InventoryService inventory = FindFirstObjectByType<BF_InventoryService>();

        foreach (BF_UnitSpawnData data in _board.LevelConfig.UnitSpawns)
        {
            BF_BattleUnit unit = Instantiate(_unitPrefab, _unitsRoot);
            unit.name = $"{data.Team}_{data.Unit.Id}";
            BF_UnitRuntimeData unitData = null;
            if (data.Team == BF_UnitTeam.Player && runtime != null)
            {
                string skill01 = data.Unit.Skill01 != null ? data.Unit.Skill01.Id : string.Empty;
                string skill02 = data.Unit.Skill02 != null ? data.Unit.Skill02.Id : string.Empty;
                unitData = runtime.GetOrCreate(data.Unit.Id, skill01, skill02);
            }

            unit.Init(_board, data.Unit, data.Team, data.Pos, unitData, inventory);
            units.Add(unit);
        }

        return units;
    }
}
