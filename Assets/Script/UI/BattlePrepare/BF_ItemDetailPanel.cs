using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BF_ItemDetailPanel : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _descriptionText;

    public void Show(BF_ItemConfigSO item)
    {
        _icon.sprite = item != null ? item.Icon : null;
        _icon.enabled = item != null && item.Icon != null;
        _nameText.text = item != null ? item.DisplayName : "请选择物品";
        _typeText.text = item != null ? GetTypeText(item) : string.Empty;
        _descriptionText.text = item != null ? item.Description : string.Empty;
    }

    private string GetTypeText(BF_ItemConfigSO item)
    {
        if (item.ItemType == BF_ItemType.Consumable)
        {
            return $"消耗品  |  恢复 {item.HealAmount} HP  |  {item.APCost} AP";
        }

        return $"装备  |  攻击 +{item.AttackBonus}  防御 +{item.DefenseBonus}  HP +{item.MaxHPBonus}";
    }
}
