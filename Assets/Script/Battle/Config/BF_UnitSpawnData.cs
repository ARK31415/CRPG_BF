using System;
using UnityEngine;

[Serializable]
public class BF_UnitSpawnData
{
    [SerializeField]
    private BF_UnitConfigSO _unit;

    [SerializeField]
    private BF_UnitTeam _team;

    [SerializeField]
    private Vector2Int _pos;

    public BF_UnitConfigSO Unit => _unit;
    public BF_UnitTeam Team => _team;
    public Vector2Int Pos => _pos;
}
