using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// 验收用回归测试：覆盖会破坏进度、金币、库存与战斗状态的核心规则。
/// 服务用测试内构造的 SO 与组件搭建，不依赖 Persistent 场景。
/// </summary>
public class BF_GameRulesPlayModeTests
{
    private readonly List<Object> _objectsToDestroy = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objectsToDestroy.Count - 1; i >= 0; i--)
        {
            if (_objectsToDestroy[i] != null)
            {
                Object.DestroyImmediate(_objectsToDestroy[i]);
            }
        }

        _objectsToDestroy.Clear();
    }

    #region 关卡解锁

    [Test]
    public void LevelProgress_UnlocksLinearlyAndMarksDemoCompleteOnLevel3()
    {
        BF_LevelProgress progress = Track(new GameObject("Progress")).AddComponent<BF_LevelProgress>();

        Assert.That(progress.IsUnlocked(1), Is.True);
        Assert.That(progress.IsUnlocked(2), Is.False);

        progress.CompleteLevel(1);
        Assert.That(progress.IsUnlocked(2), Is.True);
        Assert.That(progress.IsCompleted(1), Is.True);
        Assert.That(progress.IsUnlocked(3), Is.False);

        progress.CompleteLevel(1);
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2), "Replaying level 1 must not skip ahead.");

        progress.CompleteLevel(2);
        progress.CompleteLevel(3);
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(3));
        Assert.That(progress.IsDemoCompleted, Is.True);
        Assert.That(progress.IsCompleted(3), Is.True);

        progress.CompleteLevel(4);
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(3), "Invalid level must be ignored.");
    }

    [Test]
    public void LevelProgress_LoadClampsSavedValues()
    {
        BF_LevelProgress progress = Track(new GameObject("Progress")).AddComponent<BF_LevelProgress>();

        progress.LoadProgress(9, false);
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(3));

        progress.LoadProgress(0, false);
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
    }

    #endregion

    #region 商店与库存

    [Test]
    public void ShopBuy_SuccessSpendsGoldAndAddsItemOnce()
    {
        BF_ItemConfigSO potion = CreateItem("potion", BF_ItemType.Consumable, 30, 3);
        BF_InventoryService inventory = CreateInventory(100, 4, potion);
        BF_ShopService shop = Track(new GameObject("Shop")).AddComponent<BF_ShopService>();

        Assert.That(shop.Buy(potion), Is.EqualTo(BF_ShopBuyResult.Success));
        Assert.That(inventory.Gold, Is.EqualTo(70));
        Assert.That(inventory.GetCount("potion"), Is.EqualTo(1));
    }

    [Test]
    public void ShopBuy_RejectedPurchasesKeepGoldAndInventory()
    {
        BF_ItemConfigSO potion = CreateItem("potion", BF_ItemType.Consumable, 30, 1);
        BF_ItemConfigSO sword = CreateItem("sword", BF_ItemType.Equipment, 10, 1);
        BF_ItemConfigSO costly = CreateItem("costly", BF_ItemType.Consumable, 999, 5);
        BF_InventoryService inventory = CreateInventory(100, 1, potion, sword, costly);
        BF_ShopService shop = Track(new GameObject("Shop")).AddComponent<BF_ShopService>();

        Assert.That(shop.Buy(costly), Is.EqualTo(BF_ShopBuyResult.NotEnoughGold));
        AssertInventory(inventory, 100, 0);

        Assert.That(shop.Buy(potion), Is.EqualTo(BF_ShopBuyResult.Success));
        Assert.That(shop.Buy(potion), Is.EqualTo(BF_ShopBuyResult.StackFull));
        AssertInventory(inventory, 70, 1);

        Assert.That(shop.Buy(sword), Is.EqualTo(BF_ShopBuyResult.InventoryFull));
        AssertInventory(inventory, 70, 1);

        Assert.That(shop.Buy(null), Is.EqualTo(BF_ShopBuyResult.InvalidItem));
        AssertInventory(inventory, 70, 1);
    }

    #endregion

    #region 战斗物品快捷栏

    [Test]
    public void BattleItemAssign_RejectsDuplicateEquipmentAndEmptyStock()
    {
        BF_ItemConfigSO potion = CreateItem("potion", BF_ItemType.Consumable, 10, 9);
        BF_ItemConfigSO sword = CreateItem("sword", BF_ItemType.Equipment, 10, 1);
        BF_InventoryService inventory = CreateInventory(0, 8, potion, sword);
        inventory.TryAdd(potion, 1);
        inventory.TryAdd(sword, 1);
        BF_UnitRuntimeService units = Track(new GameObject("Units")).AddComponent<BF_UnitRuntimeService>();
        BF_UnitRuntimeData unit = units.AddUnit("knight", string.Empty, string.Empty, true);

        Assert.That(units.SetBattleItem(unit.UnitId, 0, "potion"), Is.EqualTo(BF_BattleItemAssignResult.Success));
        Assert.That(units.SetBattleItem(unit.UnitId, 0, "potion"),
            Is.EqualTo(BF_BattleItemAssignResult.CurrentSlotAlreadyAssigned));
        Assert.That(units.SetBattleItem(unit.UnitId, 1, "potion"),
            Is.EqualTo(BF_BattleItemAssignResult.AlreadyAssignedToUnit));
        Assert.That(units.SetBattleItem(unit.UnitId, 1, "sword"), Is.EqualTo(BF_BattleItemAssignResult.InvalidItem));
        Assert.That(units.SetBattleItem(unit.UnitId, 9, "potion"), Is.EqualTo(BF_BattleItemAssignResult.InvalidTarget));
        Assert.That(inventory.GetCount("potion"), Is.EqualTo(1), "Assigning a quick slot must not consume stock.");

        Assert.That(units.SetBattleItem(unit.UnitId, 0, string.Empty), Is.EqualTo(BF_BattleItemAssignResult.Success));
        inventory.TryRemove("potion", 1);
        Assert.That(units.SetBattleItem(unit.UnitId, 0, "potion"), Is.EqualTo(BF_BattleItemAssignResult.ItemUnavailable));
    }

    [UnityTest]
    public IEnumerator UseBattleItem_ConsumesStockSpendsAPAndHealsOnce()
    {
        BF_ItemConfigSO potion = CreateItem("potion", BF_ItemType.Consumable, 10, 9);
        SetField(potion, "_apCost", 2);
        SetField(potion, "_healAmount", 5);
        BF_InventoryService inventory = CreateInventory(0, 8, potion);
        inventory.TryAdd(potion, 1);

        BF_BoardManager board = CreateBoard(3, 1);
        BF_UnitRuntimeData data = new("knight#1", "knight", string.Empty, string.Empty, true);
        data.BattleItemIds[0] = "potion";
        BF_BattleUnit unit = CreateUnit(board, new Vector2Int(0, 0), BF_UnitTeam.Player, 20, 5, 0, 6, data);
        unit.ResetTurn();

        Assert.That(unit.CanUseBattleItem(0), Is.False, "Full HP must reject healing items.");
        unit.TakeDamage(8);
        Assert.That(unit.CanUseBattleItem(1), Is.False, "Empty slot must be rejected.");
        Assert.That(unit.CanUseBattleItem(0), Is.True);

        yield return unit.UseBattleItem(0);

        Assert.That(unit.CurrentHP, Is.EqualTo(17));
        Assert.That(unit.CurrentAP, Is.EqualTo(4));
        Assert.That(inventory.GetCount("potion"), Is.EqualTo(0));

        yield return unit.UseBattleItem(0);
        Assert.That(unit.CurrentHP, Is.EqualTo(17), "Zero stock must reject a second use.");
        Assert.That(unit.CurrentAP, Is.EqualTo(4));
    }

    #endregion

    #region 技能伤害与范围

    [UnityTest]
    public IEnumerator UseSkill_DamageFollowsRateFormulaWithMinimumOne()
    {
        BF_BoardManager board = CreateBoard(3, 1);
        BF_BattleUnit attacker = CreateUnit(board, new Vector2Int(0, 0), BF_UnitTeam.Player, 20, 10, 0, 6);
        BF_BattleUnit target = CreateUnit(board, new Vector2Int(1, 0), BF_UnitTeam.Enemy, 30, 1, 4, 6);
        attacker.ResetTurn();

        // round(10 * 1.5) - 4 = 11
        yield return attacker.UseSkill(CreateSkill(BF_SkillAreaType.Single, 1.5f, 2), new Vector2Int(1, 0));
        Assert.That(target.CurrentHP, Is.EqualTo(19));
        Assert.That(attacker.CurrentAP, Is.EqualTo(4));

        // round(10 * 0.1) - 4 < 1 -> minimum 1
        yield return attacker.UseSkill(CreateSkill(BF_SkillAreaType.Single, 0.1f, 2), new Vector2Int(1, 0));
        Assert.That(target.CurrentHP, Is.EqualTo(18));
    }

    [UnityTest]
    public IEnumerator UseSkill_WithoutEnoughAPChangesNothing()
    {
        BF_BoardManager board = CreateBoard(3, 1);
        BF_BattleUnit attacker = CreateUnit(board, new Vector2Int(0, 0), BF_UnitTeam.Player, 20, 10, 0, 1);
        BF_BattleUnit target = CreateUnit(board, new Vector2Int(1, 0), BF_UnitTeam.Enemy, 30, 1, 0, 6);
        attacker.ResetTurn();

        yield return attacker.UseSkill(CreateSkill(BF_SkillAreaType.Single, 1f, 2), new Vector2Int(1, 0));

        Assert.That(target.CurrentHP, Is.EqualTo(30));
        Assert.That(attacker.CurrentAP, Is.EqualTo(1));
    }

    [Test]
    public void ProjectilePath_PassesAlliesAndStopsAtFirstEnemyOrBlock()
    {
        BF_BoardManager board = CreateBoard(6, 1, new List<Vector2Int> { new(5, 0) });
        BF_BattleUnit caster = CreateUnit(board, new Vector2Int(0, 0), BF_UnitTeam.Player, 10, 1, 0, 6);
        CreateUnit(board, new Vector2Int(1, 0), BF_UnitTeam.Player, 10, 1, 0, 6);
        CreateUnit(board, new Vector2Int(3, 0), BF_UnitTeam.Enemy, 10, 1, 0, 6);

        List<Vector2Int> path = BF_SkillRange.GetProjectilePath(board, caster, new Vector2Int(5, 0), 5);
        Assert.That(path, Is.EqualTo(new List<Vector2Int> { new(1, 0), new(2, 0), new(3, 0) }));

        BF_BoardManager openBoard = CreateBoard(6, 1, new List<Vector2Int> { new(3, 0) });
        BF_BattleUnit lone = CreateUnit(openBoard, new Vector2Int(0, 0), BF_UnitTeam.Player, 10, 1, 0, 6);
        List<Vector2Int> blocked = BF_SkillRange.GetProjectilePath(openBoard, lone, new Vector2Int(5, 0), 5);
        Assert.That(blocked[blocked.Count - 1], Is.EqualTo(new Vector2Int(3, 0)), "Blocked cell ends the line.");
    }

    [Test]
    public void AreaCells_FrontTAndSquareMatchSkillShapes()
    {
        BF_BoardManager board = CreateBoard(5, 5);
        BF_BattleUnit caster = CreateUnit(board, new Vector2Int(2, 2), BF_UnitTeam.Player, 10, 1, 0, 6);

        List<Vector2Int> frontT = BF_SkillRange.GetAreaCells(
            board, caster, new Vector2Int(3, 2), CreateSkill(BF_SkillAreaType.FrontT, 1f, 2));
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(3, 2), new Vector2Int(3, 3), new Vector2Int(3, 1) }, frontT);

        BF_SkillConfigSO square = CreateSkill(BF_SkillAreaType.Square, 1f, 2);
        SetField(square, "_areaSize", 3);
        List<Vector2Int> cells = BF_SkillRange.GetAreaCells(board, caster, new Vector2Int(0, 0), square);
        Assert.That(cells.Count, Is.EqualTo(4), "3x3 at the corner is clipped to the board.");
    }

    #endregion

    #region 战斗结算

    [Test]
    public void BattleResult_RepeatedEventsAndFinalizeSettleOnlyOnce()
    {
        BF_ItemConfigSO potion = CreateItem("potion", BF_ItemType.Consumable, 10, 9);
        BF_InventoryService inventory = CreateInventory(0, 8, potion);

        BF_LevelConfigSO level = Track(ScriptableObject.CreateInstance<BF_LevelConfigSO>());
        SetField(level, "_rewardGold", 50);

        BF_BattleService battle = CreateBattleService(level, out BF_LevelProgress progress);

        GameEventBus.Instance.Publish(new BF_BattleResultEvent(BF_BattleResult.Victory));
        GameEventBus.Instance.Publish(new BF_BattleResultEvent(BF_BattleResult.Victory));
        Assert.That(inventory.Gold, Is.EqualTo(0), "Result must stay pending until finalize.");

        battle.FinalizePendingResult();
        GameEventBus.Instance.Publish(new BF_BattleResultEvent(BF_BattleResult.Victory));
        battle.FinalizePendingResult();

        Assert.That(inventory.Gold, Is.EqualTo(50), "Reward must be granted exactly once.");
        Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(2));
        Assert.That(battle.LastResult, Is.EqualTo(BF_BattleResult.Victory));
    }

    [Test]
    public void BattleResult_FirstClearOnlyRewardUnitIsGrantedOnce()
    {
        CreateInventory(0, 8);
        BF_UnitRuntimeService units = Track(new GameObject("Units")).AddComponent<BF_UnitRuntimeService>();
        BF_UnitConfigSO mage = Track(ScriptableObject.CreateInstance<BF_UnitConfigSO>());
        SetField(mage, "_id", "mage");

        BF_LevelConfigSO level = Track(ScriptableObject.CreateInstance<BF_LevelConfigSO>());
        SetField(level, "_rewardUnit", mage);
        SetField(level, "_rewardUnitMode", BF_UnitRewardMode.FirstClearOnly);
        BF_BattleService battle = CreateBattleService(level, out _);

        SettleVictory(battle);
        SettleVictory(battle);

        Assert.That(units.Units.Count, Is.EqualTo(1), "FirstClearOnly reward must not repeat on replay.");
        Assert.That(units.Units[0].ConfigId, Is.EqualTo("mage"));
        Assert.That(units.Units[0].IsDeployed, Is.False);
    }

    #endregion

    #region 角色实例与成长

    [Test]
    public void Equipment_MovesOwnershipAndStacksStatsIntoBattleUnit()
    {
        BF_ItemConfigSO sword = CreateItem("sword", BF_ItemType.Equipment, 10, 1);
        SetField(sword, "_equipmentSlot", BF_EquipmentSlot.Weapon);
        SetField(sword, "_attackBonus", 3);
        BF_ItemConfigSO boots = CreateItem("boots", BF_ItemType.Equipment, 10, 1);
        SetField(boots, "_equipmentSlot", BF_EquipmentSlot.Shoes);
        SetField(boots, "_maxAPBonus", 1);
        BF_InventoryService inventory = CreateInventory(0, 8, sword, boots);
        inventory.TryAdd(sword, 1);
        inventory.TryAdd(boots, 1);
        BF_UnitRuntimeService units = Track(new GameObject("Units")).AddComponent<BF_UnitRuntimeService>();
        BF_UnitRuntimeData data = units.AddUnit("knight", string.Empty, string.Empty, true);

        Assert.That(units.SetEquipment(data.UnitId, BF_EquipmentSlot.Weapon, "sword"), Is.True);
        Assert.That(units.SetEquipment(data.UnitId, BF_EquipmentSlot.Shoes, "boots"), Is.True);
        Assert.That(inventory.Items.Count, Is.EqualTo(0), "Equipped items leave the warehouse.");

        BF_BattleUnit unit = CreateUnit(CreateBoard(2, 1), Vector2Int.zero, BF_UnitTeam.Player, 10, 2, 0, 6, data);
        Assert.That(unit.Attack, Is.EqualTo(5));
        Assert.That(unit.MaxAP, Is.EqualTo(7));

        Assert.That(units.SetEquipment(data.UnitId, BF_EquipmentSlot.Weapon, string.Empty), Is.True);
        Assert.That(inventory.GetCount("sword"), Is.EqualTo(1), "Unequipped item returns to the warehouse.");
        Assert.That(units.SetEquipment(data.UnitId, BF_EquipmentSlot.Head, "missing"), Is.False);
    }

    [Test]
    public void UnitInstances_HaveIndependentIdsExpAndDeployState()
    {
        BF_UnitRuntimeService units = Track(new GameObject("Units")).AddComponent<BF_UnitRuntimeService>();
        BF_UnitConfigSO config = Track(ScriptableObject.CreateInstance<BF_UnitConfigSO>());
        SetField(config, "_maxLevel", 2);

        BF_UnitRuntimeData first = units.AddUnit("knight", string.Empty, string.Empty, true);
        BF_UnitRuntimeData second = units.AddUnit("knight", string.Empty, string.Empty);
        Assert.That(first.UnitId, Is.Not.EqualTo(second.UnitId));

        // Lv1 -> Lv2 needs 50; max level 2 stops further growth.
        Assert.That(units.AddExp(first.UnitId, 60, config.GetExpRequiredToNextLevel), Is.EqualTo(50));
        Assert.That(first.Level, Is.EqualTo(2));
        Assert.That(first.CurrentExp, Is.EqualTo(0));
        Assert.That(second.Level, Is.EqualTo(1), "Exp must not leak to another instance.");

        units.SetDeployed(second.UnitId, true);
        units.SetDeployed(first.UnitId, false);
        Assert.That(units.GetDeployedUnits(), Is.EqualTo(new List<BF_UnitRuntimeData> { second }));
    }

    [UnityTest]
    public IEnumerator Singleton_DuplicateIsDestroyedAndInstanceClearsOnDestroy()
    {
        BF_InventoryService first = CreateInventory(0, 4);
        GameObject duplicate = Track(new GameObject("DuplicateInventory"));
        duplicate.AddComponent<BF_InventoryService>();
        yield return null;

        Assert.That(duplicate == null, Is.True, "Second instance must destroy itself.");
        Assert.That(BF_InventoryService.Instance, Is.SameAs(first));

        Object.Destroy(first.gameObject);
        yield return null;
        Assert.That(BF_InventoryService.Instance, Is.Null);
    }

    #endregion

    #region 测试搭建

    private BF_BattleService CreateBattleService(BF_LevelConfigSO level, out BF_LevelProgress progress)
    {
        GameObject obj = Track(new GameObject("BattleService"));
        obj.SetActive(false);
        progress = obj.AddComponent<BF_LevelProgress>();
        BF_BattleService battle = obj.AddComponent<BF_BattleService>();
        SetField(battle, "_levelProgress", progress);
        SetField(battle, "_levels", new[] { level });
        obj.SetActive(true);
        return battle;
    }

    private static void SettleVictory(BF_BattleService battle)
    {
        GameEventBus.Instance.Publish(new BF_BattleResultEvent(BF_BattleResult.Victory));
        battle.FinalizePendingResult();
        // Result 确认依赖场景加载；测试内直接结束本次结算以模拟返回选关。
        SetField(battle, "_isResultActive", false);
    }

    private BF_InventoryService CreateInventory(int gold, int capacity, params BF_ItemConfigSO[] catalog)
    {
        BF_InventoryConfigSO config = Track(ScriptableObject.CreateInstance<BF_InventoryConfigSO>());
        SetField(config, "_capacity", capacity);
        SetField(config, "_startingGold", gold);
        SetField(config, "_itemCatalog", new List<BF_ItemConfigSO>(catalog));

        GameObject obj = Track(new GameObject("Inventory"));
        obj.SetActive(false);
        BF_InventoryService inventory = obj.AddComponent<BF_InventoryService>();
        SetField(inventory, "_config", config);
        obj.SetActive(true);
        Assert.That(BF_InventoryService.Instance, Is.SameAs(inventory));
        return inventory;
    }

    private BF_ItemConfigSO CreateItem(string id, BF_ItemType type, int price, int maxStack)
    {
        BF_ItemConfigSO item = Track(ScriptableObject.CreateInstance<BF_ItemConfigSO>());
        SetField(item, "_id", id);
        SetField(item, "_itemType", type);
        SetField(item, "_buyPrice", price);
        SetField(item, "_maxStack", maxStack);
        return item;
    }

    private BF_SkillConfigSO CreateSkill(BF_SkillAreaType area, float rate, int apCost)
    {
        BF_SkillConfigSO skill = Track(ScriptableObject.CreateInstance<BF_SkillConfigSO>());
        SetField(skill, "_areaType", area);
        SetField(skill, "_rate", rate);
        SetField(skill, "_apCost", apCost);
        SetField(skill, "_targetRange", 5);
        SetField(skill, "_hitDelay", 0f);
        SetField(skill, "_duration", 0f);
        return skill;
    }

    private BF_BattleUnit CreateUnit(
        BF_BoardManager board,
        Vector2Int pos,
        BF_UnitTeam team,
        int hp,
        int attack,
        int defense,
        int ap,
        BF_UnitRuntimeData data = null)
    {
        BF_UnitConfigSO config = Track(ScriptableObject.CreateInstance<BF_UnitConfigSO>());
        SetField(config, "_maxHP", hp);
        SetField(config, "_attack", attack);
        SetField(config, "_defense", defense);
        SetField(config, "_maxAP", ap);

        GameObject obj = Track(new GameObject($"Unit_{pos.x}_{pos.y}"));
        BF_BattleUnit unit = obj.AddComponent<BF_BattleUnit>();
        unit.Init(board, config, team, pos, data);
        return unit;
    }

    private BF_BoardManager CreateBoard(int width, int height, List<Vector2Int> blockedCells = null)
    {
        BF_LevelConfigSO config = Track(ScriptableObject.CreateInstance<BF_LevelConfigSO>());
        BF_TerrainRuleSetSO rules = Track(ScriptableObject.CreateInstance<BF_TerrainRuleSetSO>());
        SetField(config, "_width", width);
        SetField(config, "_height", height);
        SetField(config, "_terrainRules", rules);

        List<BF_TerrainCellData> terrain = new();
        if (blockedCells != null)
        {
            for (int i = 0; i < blockedCells.Count; i++)
            {
                terrain.Add(new BF_TerrainCellData { Position = blockedCells[i], TerrainType = TerrainType.Blocked });
            }
        }

        SetField(config, "_terrainCells", terrain);

        BF_BoardCell cellPrefab = Track(new GameObject("CellPrefab")).AddComponent<BF_BoardCell>();
        GameObject boardObject = Track(new GameObject("Board"));
        boardObject.SetActive(false);
        BF_BoardManager board = boardObject.AddComponent<BF_BoardManager>();
        SetField(board, "_levelConfig", config);
        SetField(board, "_cellPrefab", cellPrefab);
        SetField(board, "_cellSize", Vector2.one);
        boardObject.SetActive(true);
        return board;
    }

    private static void AssertInventory(BF_InventoryService inventory, int gold, int entries)
    {
        Assert.That(inventory.Gold, Is.EqualTo(gold));
        Assert.That(inventory.Items.Count, Is.EqualTo(entries));
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

    #endregion
}
