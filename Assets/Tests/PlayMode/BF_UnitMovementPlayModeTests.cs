using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BF_UnitMovementPlayModeTests {
    private readonly List<Object> _objectsToDestroy = new();

    [TearDown]
    public void TearDown() {
        for (int i = _objectsToDestroy.Count - 1; i >= 0; i--) {
            if (_objectsToDestroy[i] != null) {
                Object.DestroyImmediate(_objectsToDestroy[i]);
            }
        }

        _objectsToDestroy.Clear();
    }

    [Test]
    public void FindReachable_UsesMoveRangeAndAvoidsBlockedOrOccupiedCells() {
        BF_BoardManager board = CreateBoard(
            4,
            3,
            new List<Vector2Int> { new(0, 2) });
        GameObject blocker = Track(new GameObject("Blocker"));
        board.TryOccupy(new Vector2Int(1, 1), blocker);
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> cost = new();

        HashSet<Vector2Int> reachable = BF_Pathfinder.FindReachable(
            board,
            new Vector2Int(0, 1),
            4,
            cameFrom,
            cost);

        Assert.That(reachable.Contains(new Vector2Int(0, 2)), Is.False);
        Assert.That(reachable.Contains(new Vector2Int(1, 1)), Is.False);
        Assert.That(reachable.Contains(new Vector2Int(2, 1)), Is.True);
    }

    [Test]
    public void BuildPath_ReturnsShortestPathWithoutStartCell() {
        BF_BoardManager board = CreateBoard(4, 3);
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> cost = new();
        Vector2Int start = new(0, 1);
        Vector2Int target = new(2, 1);

        BF_Pathfinder.FindReachable(board, start, 2, cameFrom, cost);
        List<Vector2Int> path = BF_Pathfinder.BuildPath(start, target, cameFrom);

        Assert.That(path.Count, Is.EqualTo(2));
        Assert.That(path[0], Is.EqualTo(new Vector2Int(1, 1)));
        Assert.That(path[1], Is.EqualTo(target));
    }

    [Test]
    public void FindReachable_ReachabilityFollowsCumulativeCostNotStepCount() {
        BF_BoardManager board = CreateBoard(
            4,
            1,
            null,
            new List<BF_TerrainCellData> { SwampAt(1, 0) });
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> cost = new();
        Vector2Int start = new(0, 0);

        HashSet<Vector2Int> reachable4 = BF_Pathfinder.FindReachable(board, start, 4, cameFrom, cost);
        Assert.That(reachable4.Contains(new Vector2Int(1, 0)), Is.True, "Single swamp cell within 4 AP budget.");
        Assert.That(reachable4.Contains(new Vector2Int(2, 0)), Is.False, "Two cells cost 4 + 1 = 5 AP, over budget 4.");

        HashSet<Vector2Int> reachable5 = BF_Pathfinder.FindReachable(board, start, 5, cameFrom, cost);
        Assert.That(reachable5.Contains(new Vector2Int(2, 0)), Is.True);
    }

    [Test]
    public void FindReachable_CheapestPathPrefersLongerNormalRouteOverSwampShortcut() {
        BF_BoardManager board = CreateBoard(
            4,
            2,
            null,
            new List<BF_TerrainCellData>
            {
                SwampAt(1, 0),
                SwampAt(2, 0),
            });
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> cost = new();
        Vector2Int start = new(0, 0);
        Vector2Int target = new(3, 0);

        HashSet<Vector2Int> reachable = BF_Pathfinder.FindReachable(board, start, 6, cameFrom, cost);

        Assert.That(reachable.Contains(target), Is.True);
        Assert.That(cost[target], Is.EqualTo(5), "5 normal cells on y=1 route cost 5 AP.");
        Assert.That(cost[new Vector2Int(1, 0)], Is.EqualTo(4), "Single swamp cell costs 4 AP.");
        Assert.That(reachable.Contains(new Vector2Int(2, 0)), Is.False,
            "Two swamp cells cost 8 AP and stay out of the 6 AP budget.");

        List<Vector2Int> path = BF_Pathfinder.BuildPath(start, target, cameFrom);
        Assert.That(path.Count, Is.EqualTo(5));
        Assert.That(path[0], Is.EqualTo(new Vector2Int(0, 1)));
        Assert.That(path[3], Is.EqualTo(new Vector2Int(3, 1)));
        Assert.That(path[4], Is.EqualTo(target));
    }

    [Test]
    public void FindReachable_EqualCostPathsStayDeterministic() {
        BF_BoardManager board = CreateBoard(3, 3);
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, int> cost = new();
        Vector2Int start = new(1, 1);
        Vector2Int target = new(2, 2);

        BF_Pathfinder.FindReachable(board, start, 2, cameFrom, cost);
        List<Vector2Int> firstRun = BF_Pathfinder.BuildPath(start, target, cameFrom);

        BF_Pathfinder.FindReachable(board, start, 2, cameFrom, cost);
        List<Vector2Int> secondRun = BF_Pathfinder.BuildPath(start, target, cameFrom);

        Assert.That(firstRun.Count, Is.EqualTo(2));
        Assert.That(secondRun, Is.EqualTo(firstRun));
    }

    [UnityTest]
    public IEnumerator Move_UpdatesUnitGridPosAndBoardOccupancy() {
        BF_BoardManager board = CreateBoard(3, 1);
        BF_BattleUnit unit = CreateUnitAt(board, Vector2Int.zero, out GameObject unitObject);
        yield return null;

        Assert.That(board.TryGetOccupant(Vector2Int.zero, out GameObject startOccupant), Is.True);
        Assert.That(startOccupant, Is.SameAs(unitObject));

        List<Vector2Int> path = new() { new(1, 0), new(2, 0) };
        yield return unit.Move(path);

        Assert.That(unit.GridPos, Is.EqualTo(new Vector2Int(2, 0)));
        Assert.That(board.IsOccupied(Vector2Int.zero), Is.False);
        Assert.That(board.TryGetOccupant(new Vector2Int(2, 0), out GameObject occupant), Is.True);
        Assert.That(occupant, Is.SameAs(unitObject));
    }

    [UnityTest]
    public IEnumerator Execute_MoveDeductsRealTerrainCostAfterArrival() {
        BF_BoardManager board = CreateBoard(
            3,
            1,
            null,
            new List<BF_TerrainCellData> { SwampAt(1, 0) });
        BF_BattleUnit unit = CreateUnitAt(board, Vector2Int.zero, out GameObject unitObject);
        unit.ResetTurn();
        Assert.That(unit.CurrentAP, Is.EqualTo(6));

        yield return new BF_BattleCommandExecutor().Execute(
            BF_BattleCommandRequest.CreateMove(unit, new List<Vector2Int> { new(1, 0) }));

        Assert.That(unit.GridPos, Is.EqualTo(new Vector2Int(1, 0)));
        Assert.That(unit.CurrentAP, Is.EqualTo(2), "Swamp entry cost 4 AP deducted after arrival, not Path.Count.");
    }

    [UnityTest]
    public IEnumerator Execute_MoveRejectsBlockedJumpOccupiedOrUnpayablePaths() {
        BF_BoardManager board = CreateBoard(
            3,
            2,
            new List<Vector2Int> { new(1, 1) },
            new List<BF_TerrainCellData> { SwampAt(1, 0) });
        BF_BattleUnit unit = CreateUnitAt(board, Vector2Int.zero, out _);
        GameObject blocker = Track(new GameObject("Blocker"));
        board.TryOccupy(new Vector2Int(2, 0), blocker);
        unit.ResetTurn();
        BF_BattleCommandExecutor executor = new();

        Vector2Int startPos = unit.GridPos;
        int startAP = unit.CurrentAP;

        // 非连续跳格。
        yield return executor.Execute(BF_BattleCommandRequest.CreateMove(
            unit,
            new List<Vector2Int> { new(2, 1) }));
        AssertStateUnchanged(unit, startPos, startAP);

        // 越过阻挡格。
        yield return executor.Execute(BF_BattleCommandRequest.CreateMove(
            unit,
            new List<Vector2Int> { new(0, 1), new(1, 1) }));
        AssertStateUnchanged(unit, startPos, startAP);

        // 目标被占用。
        yield return executor.Execute(BF_BattleCommandRequest.CreateMove(
            unit,
            new List<Vector2Int> { new(1, 0), new(2, 0) }));
        AssertStateUnchanged(unit, startPos, startAP);

        // 真实成本超过剩余 AP：先花到 3 AP，再尝试 4 AP 的沼泽格。
        Assert.That(unit.SpendAP(3), Is.True);
        int lowAP = unit.CurrentAP;
        yield return executor.Execute(BF_BattleCommandRequest.CreateMove(
            unit,
            new List<Vector2Int> { new(1, 0) }));
        AssertStateUnchanged(unit, startPos, lowAP);
        Assert.That(board.TryGetOccupant(startPos, out _), Is.True);
    }

    private static void AssertStateUnchanged(BF_BattleUnit unit, Vector2Int pos, int ap)
    {
        Assert.That(unit.GridPos, Is.EqualTo(pos), "Rejected move must not change GridPos.");
        Assert.That(unit.CurrentAP, Is.EqualTo(ap), "Rejected move must not deduct AP.");
    }

    [Test]
    public void TryCancelPathPreview_ClearsPathAndReturnsConsumedState()
    {
        BF_BoardManager board = CreateBoard(3, 3);
        BF_BattleUnit unit = CreateUnitAt(board, Vector2Int.zero, out _);

        GameObject controllerObject = Track(new GameObject("MoveController"));
        BF_UnitMoveController controller = controllerObject.AddComponent<BF_UnitMoveController>();
        SetField(controller, "_board", board);
        SetField(controller, "_unit", unit);

        GameObject lineObject = Track(new GameObject("PathLine"));
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        SetField(controller, "_pathLine", lineRenderer);

        List<Vector2Int> path = new() { new(1, 0), new(2, 0) };
        SetField(controller, "_path", path);
        SetField(controller, "_manualPathCost", 2);

        Assert.That(controller.TryCancelPathPreview(), Is.True, "Non-empty path must be consumed and cleared.");
        Assert.That(path.Count, Is.EqualTo(0));
        Assert.That(controller.TryCancelPathPreview(), Is.False, "Empty path must not be consumed again.");
    }

    private BF_BattleUnit CreateUnitAt(
        BF_BoardManager board,
        Vector2Int pos,
        out GameObject unitObject)
    {
        unitObject = Track(new GameObject("TestUnit"));
        unitObject.SetActive(false);
        BF_BattleUnit unit = unitObject.AddComponent<BF_BattleUnit>();
        BF_UnitConfigSO config = Track(ScriptableObject.CreateInstance<BF_UnitConfigSO>());
        SetField(config, "_moveSpeed", 1000f);
        SetField(unit, "_board", board);
        SetField(unit, "_config", config);
        unit.Init(board, config, BF_UnitTeam.Player, pos);
        unitObject.SetActive(true);
        return unit;
    }

    private static BF_TerrainCellData SwampAt(int x, int y)
    {
        return new BF_TerrainCellData
        {
            Position = new Vector2Int(x, y),
            TerrainType = TerrainType.Swamp
        };
    }

    private BF_BoardManager CreateBoard(
        int width,
        int height,
        List<Vector2Int> blockedCells = null,
        List<BF_TerrainCellData> extraTerrain = null)
    {
        BF_LevelConfigSO config = Track(ScriptableObject.CreateInstance<BF_LevelConfigSO>());
        BF_TerrainRuleSetSO rules = Track(ScriptableObject.CreateInstance<BF_TerrainRuleSetSO>());
        SetField(config, "_width", width);
        SetField(config, "_height", height);
        SetField(config, "_terrainRules", rules);

        List<BF_TerrainCellData> terrainCells = new();
        if (blockedCells != null)
        {
            for (int i = 0; i < blockedCells.Count; i++)
            {
                terrainCells.Add(new BF_TerrainCellData
                {
                    Position = blockedCells[i],
                    TerrainType = TerrainType.Blocked
                });
            }
        }

        if (extraTerrain != null)
        {
            terrainCells.AddRange(extraTerrain);
        }

        SetField(config, "_terrainCells", terrainCells);

        GameObject cellObject = Track(new GameObject("CellPrefab"));
        BF_BoardCell cellPrefab = cellObject.AddComponent<BF_BoardCell>();

        GameObject boardObject = Track(new GameObject("Board"));
        boardObject.SetActive(false);
        BF_BoardManager board = boardObject.AddComponent<BF_BoardManager>();
        SetField(board, "_levelConfig", config);
        SetField(board, "_cellPrefab", cellPrefab);
        SetField(board, "_cellSize", Vector2.one);
        boardObject.SetActive(true);
        return board;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private T Track<T>(T instance) where T : Object
    {
        _objectsToDestroy.Add(instance);
        return instance;
    }
}
