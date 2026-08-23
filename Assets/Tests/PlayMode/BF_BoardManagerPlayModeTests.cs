using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BF_BoardManagerPlayModeTests
{
    private readonly List<UnityEngine.Object> _objectsToDestroy = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objectsToDestroy.Count - 1; i >= 0; i--)
        {
            if (_objectsToDestroy[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(_objectsToDestroy[i]);
            }
        }

        _objectsToDestroy.Clear();
    }

    [UnityTest]
    public IEnumerator Awake_CreatesThreeByTwoBoardWithExpectedPositions()
    {
        Component manager = CreateManager(
            width: 3,
            height: 2,
            cellSize: new Vector2(2f, 3f),
            position: new Vector3(10f, 20f, 0f));
        yield return null;

        Assert.That(GetProperty<bool>(manager, "IsInitialized"), Is.True);
        Assert.That(manager.transform.childCount, Is.EqualTo(6));

        object[] arguments = { new Vector2Int(2, 1), null };
        bool found = Invoke<bool>(manager, "TryGetCell", arguments);

        Assert.That(found, Is.True);
        Assert.That(arguments[1], Is.Not.Null);
        Assert.That(GetProperty<Vector2Int>(arguments[1], "GridPos"), Is.EqualTo(new Vector2Int(2, 1)));

        Vector3 worldPos = Invoke<Vector3>(manager, "GridToWorld", new object[] { new Vector2Int(2, 1) });
        Assert.That(worldPos, Is.EqualTo(new Vector3(15f, 24.5f, 0f)));
        Assert.That(
            Invoke<Vector2Int>(manager, "WorldToGrid", new object[] { worldPos }),
            Is.EqualTo(new Vector2Int(2, 1)));
    }

    [Test]
    public void BlockedCell_IsReportedAndCannotBeEntered()
    {
        Component manager = CreateManager(
            width: 2,
            height: 2,
            blockedCells: new List<Vector2Int> { new(1, 1) });

        Assert.That(Invoke<bool>(manager, "IsBlocked", new object[] { new Vector2Int(1, 1) }), Is.True);
        Assert.That(Invoke<bool>(manager, "CanEnter", new object[] { new Vector2Int(1, 1) }), Is.False);
        Assert.That(Invoke<bool>(manager, "IsOccupied", new object[] { new Vector2Int(1, 1) }), Is.False);
    }

    [Test]
    public void Occupancy_CanOnlyBeClearedByExpectedOccupant()
    {
        Component manager = CreateManager(width: 2, height: 1);
        GameObject occupant = Track(new GameObject("Occupant"));
        GameObject other = Track(new GameObject("Other"));
        Vector2Int pos = Vector2Int.zero;

        Assert.That(Invoke<bool>(manager, "TryOccupy", new object[] { pos, occupant }), Is.True);
        Assert.That(Invoke<bool>(manager, "IsOccupied", new object[] { pos }), Is.True);
        Assert.That(Invoke<bool>(manager, "CanEnter", new object[] { pos }), Is.False);

        object[] occupantArguments = { pos, null };
        Assert.That(Invoke<bool>(manager, "TryGetOccupant", occupantArguments), Is.True);
        Assert.That(occupantArguments[1], Is.SameAs(occupant));

        Assert.That(Invoke<bool>(manager, "TryOccupy", new object[] { pos, other }), Is.False);
        Assert.That(Invoke<bool>(manager, "TryVacate", new object[] { pos, other }), Is.False);
        Assert.That(Invoke<bool>(manager, "TryVacate", new object[] { pos, occupant }), Is.True);
        Assert.That(Invoke<bool>(manager, "IsOccupied", new object[] { pos }), Is.False);
    }

    [Test]
    public void MoveOccupant_IsAtomicAndRequiresEnterableDestination()
    {
        Component manager = CreateManager(
            width: 3,
            height: 1,
            blockedCells: new List<Vector2Int> { new(2, 0) });
        GameObject occupant = Track(new GameObject("Occupant"));
        GameObject other = Track(new GameObject("Other"));
        Vector2Int source = Vector2Int.zero;
        Vector2Int destination = new(1, 0);

        Assert.That(Invoke<bool>(manager, "TryOccupy", new object[] { source, occupant }), Is.True);
        Assert.That(Invoke<bool>(manager, "TryMoveOccupant", new object[] { source, source, occupant }), Is.False);
        Assert.That(Invoke<bool>(manager, "TryMoveOccupant", new object[] { source, new Vector2Int(2, 0), occupant }), Is.False);
        Assert.That(Invoke<bool>(manager, "TryMoveOccupant", new object[] { source, destination, other }), Is.False);
        Assert.That(Invoke<bool>(manager, "IsOccupied", new object[] { source }), Is.True);

        Assert.That(Invoke<bool>(manager, "TryMoveOccupant", new object[] { source, destination, occupant }), Is.True);
        Assert.That(Invoke<bool>(manager, "IsOccupied", new object[] { source }), Is.False);

        object[] destinationOccupantArguments = { destination, null };
        Assert.That(Invoke<bool>(manager, "TryGetOccupant", destinationOccupantArguments), Is.True);
        Assert.That(destinationOccupantArguments[1], Is.SameAs(occupant));
    }

    [Test]
    public void OutOfBoundsAndUninitializedQueries_FailSafely()
    {
        LogAssert.Expect(LogType.Error, new Regex("Board config or cell prefab is missing"));
        Component manager = CreateManager(width: 1, height: 1, includeConfig: false);
        Vector2Int pos = Vector2Int.zero;

        Assert.That(GetProperty<bool>(manager, "IsInitialized"), Is.False);
        Assert.That(Invoke<bool>(manager, "IsInside", new object[] { pos }), Is.False);
        Assert.That(Invoke<bool>(manager, "CanEnter", new object[] { pos }), Is.False);

        object[] cellArguments = { pos, null };
        Assert.That(Invoke<bool>(manager, "TryGetCell", cellArguments), Is.False);
        Assert.That(Invoke<Vector3>(manager, "GridToWorld", new object[] { new Vector2Int(3, 4) }), Is.EqualTo(new Vector3(3.5f, 4.5f, 0f)));
    }

    private static Type RequireRuntimeType(string typeName)
    {
        Type type = Type.GetType($"{typeName}, BF.Battle");
        Assert.That(type, Is.Not.Null, $"Runtime type {typeName} was not found.");
        return type;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static T GetProperty<T>(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(property, Is.Not.Null, $"Property {propertyName} was not found on {target.GetType().Name}.");
        return (T)property.GetValue(target);
    }

    private static T Invoke<T>(object target, string methodName, object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(method, Is.Not.Null, $"Method {methodName} was not found on {target.GetType().Name}.");
        return (T)method.Invoke(target, arguments);
    }

    private Component CreateManager(
        int width,
        int height,
        List<Vector2Int> blockedCells = null,
        Vector2? cellSize = null,
        Vector3? position = null,
        bool includeConfig = true)
    {
        Type configType = RequireRuntimeType("BF_LevelConfigSO");
        Type cellType = RequireRuntimeType("BF_BoardCell");
        Type managerType = RequireRuntimeType("BF_BoardManager");

        ScriptableObject config = null;
        if (includeConfig)
        {
            config = Track(ScriptableObject.CreateInstance(configType));
            SetField(config, "_width", width);
            SetField(config, "_height", height);
            SetField(config, "_blockedCells", blockedCells ?? new List<Vector2Int>());
        }

        GameObject cellPrefabObject = Track(new GameObject("CellPrefab"));
        Component cellPrefab = cellPrefabObject.AddComponent(cellType);

        GameObject managerObject = Track(new GameObject("BoardManager"));
        managerObject.SetActive(false);
        managerObject.transform.position = position ?? Vector3.zero;
        Component manager = managerObject.AddComponent(managerType);
        SetField(manager, "_levelConfig", config);
        SetField(manager, "_cellPrefab", cellPrefab);
        SetField(manager, "_cellSize", cellSize ?? Vector2.one);
        managerObject.SetActive(true);
        return manager;
    }

    private T Track<T>(T instance) where T : UnityEngine.Object
    {
        _objectsToDestroy.Add(instance);
        return instance;
    }
}
