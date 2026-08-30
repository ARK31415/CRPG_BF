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

    [SerializeField]
    private List<BF_UnitSpawnData> _unitSpawns = new();

    [Header("Reward")]
    [Min(0)]
    [SerializeField]
    private int _rewardGold;

    [Min(0)]
    [SerializeField]
    private int _rewardExp;

    [SerializeField]
    private List<BF_RewardItem> _rewardItems = new();

    public int Width => _width;
    public int Height => _height;
    public List<Vector2Int> BlockedCells => _blockedCells;
    public List<BF_UnitSpawnData> UnitSpawns => _unitSpawns;
    public int RewardGold => _rewardGold;
    public int RewardExp => _rewardExp;
    public IReadOnlyList<BF_RewardItem> RewardItems => _rewardItems;
}
