using UnityEngine;

/// <summary>
/// 物品静态定义。运行时数量由 BF_InventoryService 维护。
/// </summary>
[CreateAssetMenu(fileName = "SO_BF_Item", menuName = "CRPG BF/Game/Item Config")]
public class BF_ItemConfigSO : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [TextArea]
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private BF_ItemType _itemType;
    [Min(0)]
    [SerializeField] private int _buyPrice;
    [Min(0)]
    [SerializeField] private int _sellPrice;
    [Min(1)]
    [SerializeField] private int _maxStack = 99;

    [Header("Consumable")]
    [Min(0)]
    [SerializeField] private int _apCost = 2;
    [Min(0)]
    [SerializeField] private int _healAmount = 5;

    [Header("Equipment")]
    [SerializeField] private BF_EquipmentSlot _equipmentSlot;
    [SerializeField] private int _attackBonus;
    [SerializeField] private int _defenseBonus;
    [SerializeField] private int _maxHPBonus;
    [SerializeField] private int _maxAPBonus;

    public string Id => _id;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public BF_ItemType ItemType => _itemType;
    public int BuyPrice => _buyPrice;
    public int SellPrice => _sellPrice;
    public int MaxStack => _maxStack;
    public int APCost => _apCost;
    public int HealAmount => _healAmount;
    public BF_EquipmentSlot EquipmentSlot => _equipmentSlot;
    public int AttackBonus => _attackBonus;
    public int DefenseBonus => _defenseBonus;
    public int MaxHPBonus => _maxHPBonus;
    public int MaxAPBonus => _maxAPBonus;
}
