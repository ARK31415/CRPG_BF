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

        foreach (BF_UnitSpawnData data in _board.LevelConfig.UnitSpawns)
        {
            BF_BattleUnit unit = Instantiate(_unitPrefab, _unitsRoot);
            unit.name = $"{data.Team}_{data.Unit.Id}";
            unit.Init(_board, data.Unit, data.Team, data.Pos);
            units.Add(unit);
        }

        return units;
    }
}
