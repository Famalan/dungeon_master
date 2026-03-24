using UnityEngine;
using Unity.Cinemachine;

public class ExplosionVFXTrigger : MonoBehaviour
{
    [Header("Particle System")]
    public ParticleSystem explosionParticles;

    [Header("Light Flash")]
    public float flashDuration = 0.3f;
    public float flashIntensity = 5f;

    [Header("Cinemachine Impulse")]
    [Tooltip("Сила тряски камеры. 0 = без тряски (звук настраивается в HitEffectSpawner / AudioManager).")]
    public float impulseForce = 0f;

    Light flashLight;
    float flashTimer;

    void Start()
    {
        if (explosionParticles != null)
        {
            explosionParticles.Play();
        }

        flashLight = GetComponentInChildren<Light>();
        if (flashLight != null)
        {
            flashLight.intensity = flashIntensity;
            flashTimer = flashDuration;
        }

        if (impulseForce > 0.0001f)
        {
            CinemachineImpulseSource impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse == null)
            {
                impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            }

            impulse.GenerateImpulse(impulseForce);
        }
    }

    void Update()
    {
        if (flashLight != null && flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(0, flashIntensity, flashTimer / flashDuration);

            if (flashTimer <= 0)
            {
                flashLight.intensity = 0;
            }
        }
    }
}
