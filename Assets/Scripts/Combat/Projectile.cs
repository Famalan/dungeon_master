using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    public float maxLifetime = 4f;

    [Header("Damage")]
    public int damage = 25;
    public float explosionRadius = 2.5f;

    [Header("VFX")]
    public GameObject explosionPrefab;

    [Header("Owner")]
    public bool firedByPlayer = true;

    float lifetime;
    float safeTime = 0.1f;

    static PlayerStats cachedPlayerStats;
    readonly Collider[] overlapBuffer = new Collider[48];

    /// <summary>Настройка снаряда после Instantiate (дробь, большая сфера и т.д.).</summary>
    public void ApplyWeaponProfile(float moveSpeed, int hitDamage, float blastRadius, Vector3 localScale)
    {
        speed = moveSpeed;
        damage = hitDamage;
        explosionRadius = blastRadius;
        transform.localScale = localScale;
    }

    void Start()
    {
        lifetime = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.3f;
            sphere.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Projectile>() != null) return;
        if (lifetime < safeTime) return;

        if (firedByPlayer && other.CompareTag("Player")) return;
        if (!firedByPlayer && other.CompareTag("Enemy")) return;

        Explode();
    }

    void Explode()
    {
        bool hitEnemy = false;
        float effectiveExplosionRadius = explosionRadius;
        if (firedByPlayer)
        {
            PlayerStats stats = GetPlayerStats();
            if (stats != null)
            {
                effectiveExplosionRadius *= stats.blastRadiusMultiplier;
            }
        }

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            effectiveExplosionRadius,
            overlapBuffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;

            if (firedByPlayer)
            {
                if (!ColBelongsToTag(col, "Enemy")) continue;
                hitEnemy = true;
            }
            else
            {
                if (!ColBelongsToTag(col, "Player")) continue;
            }

            HealthSystem health = col.GetComponentInParent<HealthSystem>();
            if (health != null)
            {
                int dmg = damage;
                if (firedByPlayer)
                {
                    PlayerStats cachedPlayerStats = GetPlayerStats();
                    if (cachedPlayerStats != null)
                    {
                        dmg = Mathf.Max(1, Mathf.RoundToInt(damage * cachedPlayerStats.damageMultiplier));
                    }
                }

                health.TakeDamage(dmg);
            }
        }

        // Spawn hit VFX and SFX before destroying.
        HitEffectSpawner hitEffectSpawner = GetComponent<HitEffectSpawner>();
        if (hitEffectSpawner != null)
        {
            hitEffectSpawner.OnHit(transform.position, hitEnemy);
        }
        else if (explosionPrefab != null)
        {
            // Fallback: use legacy explosion prefab if no HitEffectSpawner is present.
            GameObject vfx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        Destroy(gameObject);
    }

    static PlayerStats GetPlayerStats()
    {
        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = Object.FindAnyObjectByType<PlayerStats>();
        }

        return cachedPlayerStats;
    }

    static bool ColBelongsToTag(Collider col, string unityTag)
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
}
