using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BF_StartingItem
{
    [SerializeField] private BF_ItemConfigSO _item;
    [Min(1)]
    [SerializeField] private int _quantity = 1;

    public BF_ItemConfigSO Item => _item;
    public int Quantity => _quantity;
}

[CreateAssetMenu(fileName = "SO_BF_Inventory", menuName = "CRPG BF/Game/Inventory Config")]
public class BF_InventoryConfigSO : ScriptableObject
{
    [Min(1)]
    [SerializeField] private int _capacity = 24;
    [Min(0)]
    [SerializeField] private int _startingGold = 500;
    [SerializeField] private List<BF_ItemConfigSO> _itemCatalog = new();
    [SerializeField] private List<BF_StartingItem> _startingItems = new();

    public int Capacity => _capacity;
    public int StartingGold => _startingGold;
    public IReadOnlyList<BF_ItemConfigSO> ItemCatalog => _itemCatalog;
    public IReadOnlyList<BF_StartingItem> StartingItems => _startingItems;
}
