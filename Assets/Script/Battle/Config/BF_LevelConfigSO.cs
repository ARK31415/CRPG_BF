using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_BF_LevelConfig", menuName = "CRPG BF/Battle/Level Config")]
public class BF_LevelConfigSO : ScriptableObject
{
    [Min(1)]
    [SerializeField]
    private int _width = 1;

    [Min(1)]
    [SerializeField]
    private int _height = 1;

    [SerializeField]
    private List<Vector2Int> _blockedCells = new();

    public int Width => _width;
    public int Height => _height;
    public List<Vector2Int> BlockedCells => _blockedCells;
}
