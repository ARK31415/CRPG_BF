using UnityEngine;

/// <summary>
/// 单个逻辑棋盘格的轻量运行时组件。
///
/// 负责保存逻辑坐标、静态地形和动态占用者；不负责寻路、输入或单位移动。
/// </summary>
public class BF_BoardCell : MonoBehaviour {
    [SerializeField]
    private SpriteRenderer _defaultSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _selectionSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _directionSpriteRenderer;

    public Vector2Int GridPos { get; private set; }
    public TerrainType TerrainType { get; private set; }
    public GameObject Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public bool CanEnter => TerrainType != TerrainType.Blocked && !IsOccupied;

    internal void Init(Vector2Int pos, TerrainType terrainType) {
        GridPos = pos;
        TerrainType = terrainType;
        Occupant = null;
    }

    internal void SetOccupant(GameObject occupant) {
        Occupant = occupant;
    }
}
