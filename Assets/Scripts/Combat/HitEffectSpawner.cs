using UnityEngine;

/// <summary>
/// Attach to a Projectile GameObject. Call OnHit() from Projectile when a collision is detected.
/// Spawns a hit VFX particle system and plays the appropriate SFX via AudioManager.
/// Relies on Particle Systems configured with Stop Action = Destroy so the spawned instance
/// cleans itself up automatically.
/// </summary>
public class HitEffectSpawner : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private GameObject hitVFXPrefab;

    [Header("SFX")]
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip hitEnemySFX;

    private const float vfxFallbackDestroyTime = 2f;

    /// <summary>
    /// Spawns hit VFX and plays the matching SFX at the given world position.
    /// </summary>
    /// <param name="position">World position of the impact.</param>
    /// <param name="hitEnemy">True when the projectile struck an enemy, false for environment hits.</param>
    public void OnHit(Vector3 position, bool hitEnemy)
    {
        SpawnVFX(position);
        PlayHitSFX(position, hitEnemy);
    }

    void SpawnVFX(Vector3 position)
    {
        if (hitVFXPrefab == null) return;

        GameObject vfx = Instantiate(hitVFXPrefab, position, Quaternion.identity);

        // If the particle system does not use Stop Action = Destroy, fall back to timed destroy.
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps == null || ps.main.stopAction != ParticleSystemStopAction.Destroy)
        {
            Destroy(vfx, vfxFallbackDestroyTime);
        }
    }

    void PlayHitSFX(Vector3 position, bool hitEnemy)
    {
        if (AudioManager.Instance == null) return;

        AudioClip clip = hitEnemy ? hitEnemySFX : hitSFX;
        if (clip != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(clip, position, hitEnemy ? 0.62f : 0.42f);
        }
    }

    public AudioClip GetBeamSoundClip()
    {
        if (hitEnemySFX != null) return hitEnemySFX;
        return hitSFX;
    }
}
