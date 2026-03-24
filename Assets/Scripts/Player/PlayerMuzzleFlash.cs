using UnityEngine;

public class PlayerMuzzleFlash : MonoBehaviour
{
    Light flashLight;
    float timer;

    void Awake()
    {
        flashLight = gameObject.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.range = 5f;
        flashLight.color = new Color(0.45f, 0.9f, 1f);
        flashLight.enabled = false;
    }

    public void Play()
    {
        if (flashLight == null) return;
        flashLight.intensity = 6f;
        flashLight.enabled = true;
        timer = 0.06f;
    }

    void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        flashLight.intensity *= 0.82f;
        if (timer <= 0f)
        {
            flashLight.enabled = false;
        }
    }
}
