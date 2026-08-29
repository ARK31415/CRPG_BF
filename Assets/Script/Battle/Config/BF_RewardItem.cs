using System;
using UnityEngine;

[Serializable]
public class BF_RewardItem
{
    [SerializeField] private BF_ItemConfigSO _item;
    [Min(1)]
    [SerializeField] private int _quantity = 1;

    public BF_ItemConfigSO Item => _item;
    public int Quantity => _quantity;
}
