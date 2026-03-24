using UnityEngine;
using UnityEngine.UI;

public class CombatTextManager : MonoBehaviour
{
    public static CombatTextManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowDamage(Vector3 worldPosition, int amount, bool damageToPlayer)
    {
        if (amount <= 0) return;

        Vector3 jitter = Random.insideUnitSphere * 0.12f;
        jitter.y = Mathf.Abs(jitter.y) * 0.2f;
        Vector3 pos = worldPosition + Vector3.up * 0.95f + jitter;

        GameObject root = new GameObject("FloatDamage");
        root.transform.position = pos;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 120;

        RectTransform canvasRt = root.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(140f, 50f);
        canvasRt.localScale = Vector3.one * 0.02f;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(root.transform, false);
        Text txt = textGo.AddComponent<Text>();
        txt.text = "-" + amount.ToString();
        txt.fontSize = 36;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        txt.font = font;

        if (damageToPlayer)
        {
            txt.color = new Color(1f, 0.25f, 0.25f);
        }
        else
        {
            txt.color = new Color(1f, 0.92f, 0.4f);
        }

        Outline outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        root.AddComponent<FloatUpAndFade>();
    }
}
