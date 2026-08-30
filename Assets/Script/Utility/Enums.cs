/// <summary>
/// 项目中跨模块使用的简单枚举集中定义。
/// 具体业务逻辑仍然放在各自模块中。
/// </summary>

/// <summary>
/// 棋盘格的静态地形类型。
/// 动态单位占用不属于地形，由 BF_BoardCell 单独维护。
/// </summary>
public enum TerrainType
{
    Normal = 0,
    Blocked = 1,
}

public enum BF_UnitTeam
{
    Player = 0,
    Enemy = 1,
}

public enum BF_GameMode
{
    None = 0,
    Menu = 1,
    Battle = 2,
    Result = 3,
    Loading = 4
}

public enum BF_BattlePhase
{
    None,
    SetupPhase,
    PlayerPhase,
    EnemyPhase,
    BattleEnd
}

public enum BF_PlayerActionMode
{
    Move,
    Skill,
    Executing
}

public enum BF_BattleResult
{
    None,
    Victory,
    Defeat
}

public enum BF_BattleCommandType
{
    Move,
    Skill,
    Item,
    EndTurn
}

public enum BF_ItemType
{
    Consumable,
    Equipment
}

public enum BF_EquipmentSlot
{
    Weapon,
    Head,
    Armor,
    Shoes
}

public enum BF_SkillTargetType
{
    Unit,
    Cell,
    Direction
}

public enum BF_SkillAreaType
{
    Single,
    ProjectileLine,
    Square,
    FrontT
}

[System.Flags]
public enum BF_SkillTargetGroup
{
    None = 0,
    Enemy = 1 << 0,
    Ally = 1 << 1,
    Self = 1 << 2
}

public enum BF_SkillAnim
{
    Attack,
    Skill01,
    Skill02
}

/// <summary>
/// 属性成长档位，档位越高等级倍率越高。
/// </summary>
public enum BF_GrowthRank
{
    S,
    A,
    B,
    C,
    D
}
