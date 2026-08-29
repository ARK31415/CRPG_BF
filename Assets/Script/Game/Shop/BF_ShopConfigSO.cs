using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_BF_Shop", menuName = "CRPG BF/Game/Shop Config")]
public class BF_ShopConfigSO : ScriptableObject
{
    [SerializeField] private List<BF_ItemConfigSO> _items = new();

    public IReadOnlyList<BF_ItemConfigSO> Items => _items;
}
