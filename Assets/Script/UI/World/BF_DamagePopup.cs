using System.Collections;
using TMPro;
using UnityEngine;

public class BF_DamagePopup : MonoBehaviour
{
    [SerializeField]
    private Color _damageColor = new Color(1f, 0.4f, 0.2f, 1f);

    [Min(0.01f)]
    [SerializeField]
    private float _life = 0.6f;

    [SerializeField]
    private float _rise = 0.5f;

    [SerializeField]
    private float _fontSize = 8f;

    [SerializeField]
    private int _sortingOrder = 20;

    private BF_BattleUnit _unit;
    private int _popupCount;

    private void Awake()
    {
        _unit = GetComponentInParent<BF_BattleUnit>();
        GameEventBus.Instance?.Subscribe<BF_DamagePopupEvent>(OnDamage)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnDamage(BF_DamagePopupEvent gameEvent)
    {
        if (gameEvent.Unit != _unit || gameEvent.Damage <= 0)
        {
            return;
        }

        Show(gameEvent.Damage);
    }

    private void Show(int damage)
    {
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.SetParent(transform, false);

        int slot = _popupCount++ % 3;
        popup.transform.localPosition = new Vector3((slot - 1) * 0.25f, 0f, 0f);

        TextMeshPro text = popup.AddComponent<TextMeshPro>();
        text.text = damage.ToString();
        text.fontSize = _fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.autoSizeTextContainer = true;
        text.color = _damageColor;
        text.outlineWidth = 0.2f;
        text.outlineColor = Color.black;
        text.sortingLayerID = SortingLayer.NameToID("Front1");
        text.sortingOrder = _sortingOrder;

        StartCoroutine(ShowCoroutine(text));
    }

    private IEnumerator ShowCoroutine(TextMeshPro text)
    {
        float time = 0f;
        float life = Mathf.Max(0.01f, _life);
        Vector3 start = text.transform.localPosition;
        Color color = text.color;

        while (time < life)
        {
            if (!text)
            {
                yield break;
            }

            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / life);
            text.transform.localPosition = start + Vector3.up * (_rise * t);
            color.a = 1f - t;
            text.color = color;
            yield return null;
        }

        if (text)
        {
            Destroy(text.gameObject);
        }
    }
}
