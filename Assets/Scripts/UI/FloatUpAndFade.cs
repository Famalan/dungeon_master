using UnityEngine;
using UnityEngine.UI;

public class FloatUpAndFade : MonoBehaviour
{
    float timer;
    Vector3 startPos;
    Text label;

    void Start()
    {
        startPos = transform.position;
        label = GetComponentInChildren<Text>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        transform.position = startPos + Vector3.up * timer * 0.9f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                cam.transform.rotation * Vector3.up);
        }

        if (label != null)
        {
            Color c = label.color;
            c.a = 1f - Mathf.Clamp01(timer / 0.75f);
            label.color = c;
        }

        if (timer > 0.85f)
        {
            Destroy(gameObject);
        }
    }
}
