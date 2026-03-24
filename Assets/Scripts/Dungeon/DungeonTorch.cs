using UnityEngine;

public class DungeonTorch : MonoBehaviour
{
    [Header("Flicker")]
    public Light torchLight;
    public float minIntensity = 2.5f;
    public float maxIntensity = 4f;
    public float flickerSpeed = 3f;

    float noise;

    void Start()
    {
        noise = Random.Range(0f, 100f);

        if (torchLight == null)
        {
            torchLight = GetComponentInChildren<Light>();
        }
    }

    void Update()
    {
        if (torchLight == null) return;

        noise += Time.deltaTime * flickerSpeed;
        float perlin = Mathf.PerlinNoise(noise, 0f);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, perlin);
    }
}
