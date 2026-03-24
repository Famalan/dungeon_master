using System;
using UnityEngine;

public enum WeaponFireMode
{
    Standard = 0,
    Buckshot = 1,
    SlowOrb = 2,
    Beam = 3
}

public class ProjectileShooter : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public PlayerMuzzleFlash muzzleFlash;

    [Header("Cooldown")]
    public float fireCooldown = 0.3f;

    [Header("Режимы")]
    public WeaponFireMode fireMode = WeaponFireMode.Standard;

    [Header("Дробь")]
    public int buckshotPellets = 6;
    public float buckshotSpreadDegrees = 4.5f;
    public float buckshotCooldownMul = 1.35f;
    [Range(0.1f, 1f)]
    public float buckshotDamageMul = 0.32f;

    [Header("Большая сфера")]
    public float slowOrbSpeed = 7f;
    public int slowOrbDamage = 38;
    public float slowOrbRadius = 3.2f;
    public float slowOrbScale = 0.85f;
    public float slowOrbCooldownMul = 1.45f;

    [Header("Луч")]
    public float beamCooldown = 2.4f;
    public float beamRange = 36f;
    public int beamDamage = 32;
    public float beamFlashSeconds = 0.1f;

    float lastFireTime;
    float lastBeamTime = -1000f;

    PlayerStats playerStats;
    readonly RaycastHit[] beamHitBuffer = new RaycastHit[32];

    void Start()
    {
        playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();
        }

        GameEvents.FireWeaponModeChanged();
    }

    public void CycleWeapon(int direction)
    {
        int count = Enum.GetValues(typeof(WeaponFireMode)).Length;
        int next = (int)fireMode + direction;
        while (next < 0)
        {
            next += count;
        }

        while (next >= count)
        {
            next -= count;
        }

        fireMode = (WeaponFireMode)next;
        GameEvents.FireWeaponModeChanged();
    }

    public string GetWeaponHudLine()
    {
        string name = GetWeaponNameRu();
        if (fireMode == WeaponFireMode.Beam)
        {
            float left = GetBeamCooldownRemaining();
            if (left > 0.02f)
            {
                return name + "  " + left.ToString("F1") + "s";
            }
        }

        return name + "  [1/2]";
    }

    float GetBeamCooldownRemaining()
    {
        float end = lastBeamTime + beamCooldown;
        return Mathf.Max(0f, end - Time.time);
    }

    string GetWeaponNameRu()
    {
        if (fireMode == WeaponFireMode.Standard) return "Пуля";
        if (fireMode == WeaponFireMode.Buckshot) return "Дробь";
        if (fireMode == WeaponFireMode.SlowOrb) return "Сфера";
        return "Луч";
    }

    public void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        if (fireMode == WeaponFireMode.Beam)
        {
            if (Time.time - lastBeamTime < beamCooldown) return;

            lastBeamTime = Time.time;
            FireBeam();
            return;
        }

        float needCd = GetProjectileCooldown();
        if (Time.time - lastFireTime < needCd) return;

        lastFireTime = Time.time;

        if (fireMode == WeaponFireMode.Standard)
        {
            FireOneProjectile(firePoint.rotation, null);
        }
        else if (fireMode == WeaponFireMode.Buckshot)
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            for (int i = 0; i < buckshotPellets; i++)
            {
                float rx = UnityEngine.Random.Range(-buckshotSpreadDegrees, buckshotSpreadDegrees);
                float ry = UnityEngine.Random.Range(-buckshotSpreadDegrees, buckshotSpreadDegrees);
                Quaternion spread = firePoint.rotation * Quaternion.Euler(rx, ry, 0f);
                FireOneProjectile(spread, WeaponFireMode.Buckshot);
            }
        }
        else if (fireMode == WeaponFireMode.SlowOrb)
        {
            FireOneProjectile(firePoint.rotation, WeaponFireMode.SlowOrb);
        }
    }

    float GetProjectileCooldown()
    {
        if (fireMode == WeaponFireMode.Buckshot)
        {
            return fireCooldown * buckshotCooldownMul;
        }

        if (fireMode == WeaponFireMode.SlowOrb)
        {
            return fireCooldown * slowOrbCooldownMul;
        }

        return fireCooldown;
    }

    void FireOneProjectile(Quaternion rotation, WeaponFireMode? profile)
    {
        if (muzzleFlash != null && profile != WeaponFireMode.Buckshot)
        {
            muzzleFlash.Play();
        }

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, rotation);
        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj == null) return;

        if (profile == WeaponFireMode.Buckshot)
        {
            int dmg = Mathf.Max(1, Mathf.RoundToInt(proj.damage * buckshotDamageMul));
            float rad = proj.explosionRadius * 0.72f;
            Vector3 sc = projObj.transform.localScale * 0.75f;
            proj.ApplyWeaponProfile(proj.speed, dmg, rad, sc);
        }
        else if (profile == WeaponFireMode.SlowOrb)
        {
            Vector3 sc = Vector3.one * slowOrbScale;
            proj.ApplyWeaponProfile(slowOrbSpeed, slowOrbDamage, slowOrbRadius, sc);
        }
    }

    void FireBeam()
    {
        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward;

        int hitCount = Physics.RaycastNonAlloc(origin, dir, beamHitBuffer, beamRange);
        if (hitCount > 1)
        {
            SortHitsByDistance(beamHitBuffer, hitCount);
        }

        Vector3 beamEnd = origin + dir * beamRange;

        float mult = 1f;
        if (playerStats != null)
        {
            mult = playerStats.damageMultiplier;
        }

        int dmg = Mathf.Max(1, Mathf.RoundToInt(beamDamage * mult));

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = beamHitBuffer[i];
            Collider col = hit.collider;
            if (col == null) continue;

            if (ColliderHasAncestorTag(col, "Enemy"))
            {
                HealthSystem hp = col.GetComponentInParent<HealthSystem>();
                if (hp != null)
                {
                    hp.TakeDamage(dmg);
                }

                beamEnd = hit.point;
            }
            else if (ColliderHasAncestorTag(col, "Player"))
            {
                continue;
            }
            else
            {
                beamEnd = hit.point;
                break;
            }
        }

        BeamLineFlash.Create(origin, beamEnd, beamFlashSeconds);

        if (AudioManager.Instance != null)
        {
            AudioClip boom = null;
            HitEffectSpawner sample = projectilePrefab.GetComponent<HitEffectSpawner>();
            if (sample != null)
            {
                boom = sample.GetBeamSoundClip();
            }

            if (boom != null)
            {
                AudioManager.Instance.PlayExplosionWithBassAtPoint(boom, beamEnd, 0.85f);
            }
        }
    }

    static bool ColliderHasAncestorTag(Collider col, string unityTag)
    {
        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag(unityTag))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    static void SortHitsByDistance(RaycastHit[] hits, int count)
    {
        for (int a = 0; a < count - 1; a++)
        {
            for (int b = a + 1; b < count; b++)
            {
                if (hits[b].distance < hits[a].distance)
                {
                    RaycastHit tmp = hits[a];
                    hits[a] = hits[b];
                    hits[b] = tmp;
                }
            }
        }
    }
}
