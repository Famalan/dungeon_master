using System.Collections.Generic;
using UnityEngine;

public enum MeleeWeaponMode
{
    Sword = 0,
    Axe = 1,
    Spear = 2,
    Hammer = 3,
    Bow = 4
}

public class MeleeWeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform attackOrigin;
    public PlayerStats damageMultiplierSource;
    public WeaponViewModelController viewModel;
    public ProjectileShooter rangedShooter;

    [Header("Mode")]
    public MeleeWeaponMode activeMode = MeleeWeaponMode.Sword;

    [Header("Audio")]
    public AudioClip swingClip;
    public AudioClip hitClip;

    readonly Collider[] hitBuffer = new Collider[48];
    readonly HashSet<HealthSystem> damagedThisSwing = new HashSet<HealthSystem>();
    float lastAttackTime = -999f;

    void Awake()
    {
        CacheReferences();
    }

    void Start()
    {
        CacheReferences();
        GameEvents.FireWeaponModeChanged();
    }

    void CacheReferences()
    {
        if (attackOrigin == null)
        {
            Camera cam = Camera.main;
            attackOrigin = cam != null ? cam.transform : transform;
        }

        if (damageMultiplierSource == null)
        {
            damageMultiplierSource = GetComponentInParent<PlayerStats>();
        }

        if (viewModel == null)
        {
            viewModel = GetComponentInChildren<WeaponViewModelController>(true);
        }
    }

    public void CycleWeapon(int direction)
    {
        int count = System.Enum.GetValues(typeof(MeleeWeaponMode)).Length;
        int next = (int)activeMode + direction;
        while (next < 0) next += count;
        while (next >= count) next -= count;

        SetMode((MeleeWeaponMode)next);
    }

    public void SetMode(MeleeWeaponMode mode)
    {
        activeMode = mode;
        if (viewModel != null)
        {
            viewModel.SetMode(mode);
        }

        GameEvents.FireWeaponModeChanged();
    }

    public string GetWeaponHudLine()
    {
        MeleeWeaponStats stats = GetStats(activeMode);
        return stats.DisplayName + "  [1/5]";
    }

    public bool TryAttack()
    {
        CacheReferences();

        MeleeWeaponStats stats = GetStats(activeMode);
        if (activeMode == MeleeWeaponMode.Bow)
        {
            if (Time.time - lastAttackTime < stats.Cooldown)
            {
                return false;
            }

            lastAttackTime = Time.time;
            if (viewModel != null)
            {
                viewModel.PlaySwing(activeMode, Mathf.Min(0.24f, stats.Cooldown * 0.65f));
            }

            if (rangedShooter == null)
            {
                rangedShooter = GetComponentInParent<ProjectileShooter>();
            }

            if (rangedShooter != null)
            {
                rangedShooter.Shoot();
            }

            return true;
        }

        if (Time.time - lastAttackTime < stats.Cooldown)
        {
            return false;
        }

        lastAttackTime = Time.time;
        damagedThisSwing.Clear();

        if (viewModel != null)
        {
            viewModel.PlaySwing(activeMode, Mathf.Min(0.22f, stats.Cooldown * 0.58f));
        }

        if (AudioManager.Instance != null && swingClip != null)
        {
            AudioManager.Instance.PlaySFX(swingClip, 0.55f);
        }

        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector3 forward = attackOrigin != null ? attackOrigin.forward : transform.forward;
        Vector3 start = origin + forward * 0.35f;
        Vector3 end = origin + forward * stats.Reach;
        Vector3 center = (start + end) * 0.5f;
        int hitCount = Physics.OverlapCapsuleNonAlloc(start, end, stats.Radius, hitBuffer);
        bool hitAny = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null) continue;
            if (!ColliderHasAncestorTag(col, "Enemy")) continue;

            HealthSystem health = col.GetComponentInParent<HealthSystem>();
            if (health == null || damagedThisSwing.Contains(health)) continue;
            if (!IsInsideAttackArc(origin, forward, col.bounds.center, stats)) continue;

            damagedThisSwing.Add(health);
            health.TakeDamage(GetScaledDamage(stats.Damage));
            ApplyKnockback(col, origin, stats.Knockback);
            hitAny = true;
        }

        if (hitAny && AudioManager.Instance != null && hitClip != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(hitClip, center, 0.65f);
        }

        return true;
    }

    int GetScaledDamage(int baseDamage)
    {
        float mult = damageMultiplierSource != null ? damageMultiplierSource.damageMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mult));
    }

    bool IsInsideAttackArc(Vector3 origin, Vector3 forward, Vector3 target, MeleeWeaponStats stats)
    {
        Vector3 toTarget = target - origin;
        toTarget.y = 0f;
        Vector3 flatForward = forward;
        flatForward.y = 0f;

        if (toTarget.sqrMagnitude > stats.Reach * stats.Reach) return false;
        if (toTarget.sqrMagnitude < 0.01f) return true;
        if (flatForward.sqrMagnitude < 0.01f) return true;

        float angle = Vector3.Angle(flatForward.normalized, toTarget.normalized);
        return angle <= stats.ArcDegrees * 0.5f;
    }

    void ApplyKnockback(Collider col, Vector3 origin, float knockback)
    {
        if (knockback <= 0.01f) return;

        Rigidbody rb = col.GetComponentInParent<Rigidbody>();
        if (rb == null || rb.isKinematic) return;

        Vector3 dir = col.bounds.center - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        rb.AddForce(dir.normalized * knockback, ForceMode.Impulse);
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

    MeleeWeaponStats GetStats(MeleeWeaponMode mode)
    {
        switch (mode)
        {
            case MeleeWeaponMode.Axe:
                return new MeleeWeaponStats("Топор", 38, 2.45f, 1.15f, 112f, 0.62f, 2.2f);
            case MeleeWeaponMode.Spear:
                return new MeleeWeaponStats("Копье", 30, 3.65f, 0.62f, 48f, 0.46f, 1.35f);
            case MeleeWeaponMode.Hammer:
                return new MeleeWeaponStats("Молот", 48, 2.25f, 1.35f, 136f, 0.82f, 4.8f);
            case MeleeWeaponMode.Bow:
                return new MeleeWeaponStats("Лук", 24, 18f, 0.35f, 14f, 0.34f, 0.4f);
            default:
                return new MeleeWeaponStats("Меч", 27, 2.55f, 0.95f, 92f, 0.38f, 1.65f);
        }
    }

    struct MeleeWeaponStats
    {
        public readonly string DisplayName;
        public readonly int Damage;
        public readonly float Reach;
        public readonly float Radius;
        public readonly float ArcDegrees;
        public readonly float Cooldown;
        public readonly float Knockback;

        public MeleeWeaponStats(string displayName, int damage, float reach, float radius,
            float arcDegrees, float cooldown, float knockback)
        {
            DisplayName = displayName;
            Damage = damage;
            Reach = reach;
            Radius = radius;
            ArcDegrees = arcDegrees;
            Cooldown = cooldown;
            Knockback = knockback;
        }
    }
}
