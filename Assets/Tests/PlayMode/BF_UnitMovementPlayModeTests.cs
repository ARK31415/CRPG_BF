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

        HashSet<Vector2Int> reachable = BF_Pathfinder.FindReachable(
            board,
            new Vector2Int(0, 1),
            4,
            cameFrom);

        Assert.That(reachable.Contains(new Vector2Int(0, 2)), Is.False);
        Assert.That(reachable.Contains(new Vector2Int(1, 1)), Is.False);
        Assert.That(reachable.Contains(new Vector2Int(2, 1)), Is.True);
    }

    [Test]
    public void BuildPath_ReturnsShortestPathWithoutStartCell() {
        BF_BoardManager board = CreateBoard(4, 3);
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Vector2Int start = new(0, 1);
        Vector2Int target = new(2, 1);

        BF_Pathfinder.FindReachable(board, start, 2, cameFrom);
        List<Vector2Int> path = BF_Pathfinder.BuildPath(start, target, cameFrom);

        Assert.That(path.Count, Is.EqualTo(2));
        Assert.That(path[0], Is.EqualTo(new Vector2Int(1, 1)));
        Assert.That(path[1], Is.EqualTo(target));
    }

    [UnityTest]
    public IEnumerator Move_UpdatesUnitGridPosAndBoardOccupancy() {
        BF_BoardManager board = CreateBoard(3, 1);
        GameObject unitObject = Track(new GameObject("TestUnit"));
        unitObject.SetActive(false);
        BF_BattleUnit unit = unitObject.AddComponent<BF_BattleUnit>();
        BF_UnitConfigSO config = Track(ScriptableObject.CreateInstance<BF_UnitConfigSO>());
        SetField(config, "_moveSpeed", 1000f);
        SetField(unit, "_board", board);
        SetField(unit, "_config", config);
        SetField(unit, "_startPos", Vector2Int.zero);
        unitObject.SetActive(true);
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

    private BF_BoardManager CreateBoard(
        int width,
        int height,
        List<Vector2Int> blockedCells = null) {
        BF_LevelConfigSO config = Track(ScriptableObject.CreateInstance<BF_LevelConfigSO>());
        SetField(config, "_width", width);
        SetField(config, "_height", height);
        SetField(config, "_blockedCells", blockedCells ?? new List<Vector2Int>());

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

    private static void SetField(object target, string fieldName, object value) {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private T Track<T>(T instance) where T : Object {
        _objectsToDestroy.Add(instance);
        return instance;
    }
}
