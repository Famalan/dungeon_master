using UnityEngine;

public class ExitBeaconPulse : MonoBehaviour
{
    public Light targetLight;
    float time;

    void Update()
    {
        if (targetLight == null) return;

        time += Time.deltaTime;
        targetLight.intensity = 2.55f + Mathf.Sin(time * 2.6f) * 0.95f;
    }
}
