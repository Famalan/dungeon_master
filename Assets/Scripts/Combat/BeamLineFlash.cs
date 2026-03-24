using UnityEngine;

/// <summary>
/// Короткая линия для визуала луча. Удаляется сама через несколько кадров.
/// </summary>
public class BeamLineFlash : MonoBehaviour
{
    public static void Create(Vector3 start, Vector3 end, float lifetimeSeconds)
    {
        GameObject go = new GameObject("BeamLineFlash");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.12f;
        lr.endWidth = 0.28f;

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");

        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", new Color(0.45f, 0.9f, 1f, 0.95f));
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", new Color(0.45f, 0.9f, 1f, 0.95f));
        }
        else
        {
            mat.color = new Color(0.45f, 0.9f, 1f, 0.95f);
        }

        lr.material = mat;
        lr.startColor = new Color(0.55f, 0.95f, 1f, 1f);
        lr.endColor = new Color(0.25f, 0.55f, 1f, 0.25f);

        BeamLineFlash flash = go.AddComponent<BeamLineFlash>();
        flash.life = Mathf.Max(0.02f, lifetimeSeconds);
    }

    float life;

    void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
