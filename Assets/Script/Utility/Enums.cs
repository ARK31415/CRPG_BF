/// <summary>
/// 棋盘格的静态地形类型。
///
/// 动态单位占用不属于地形，由 BF_BoardCell.Occupant 单独维护。
/// </summary>
public enum TerrainType
{
    Normal = 0,
    Blocked = 1,
}
