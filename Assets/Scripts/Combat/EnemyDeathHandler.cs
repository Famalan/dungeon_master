using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Death VFX")]
    public GameObject deathVFXPrefab;

    [Header("Death SFX")]
    [SerializeField] private AudioClip deathSFX;

    [Header("Loot")]
    public float baseLootChance = 0.5f;

    void Start()
    {
        HealthSystem health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.onDeath.AddListener(OnDeath);
        }
    }

    public void OnDeath()
    {
        GameEvents.FireEnemyKilled();

        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("die");
        }

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (deathVFXPrefab != null)
        {
            GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        if (deathSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(deathSFX, transform.position);
        }

        TryDropLoot();
        Destroy(gameObject, 1.5f);
    }

    void TryDropLoot()
    {
        float chance = baseLootChance;

        PlayerStats stats = Object.FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            chance += stats.lootChanceBonus;
        }

        if (Random.value < chance)
        {
            LootDrop.CreateCoinObject(transform.position);
        }

        if (GetComponent<EliteEnemy>() != null && Random.value < 0.6f)
        {
            LootDrop.CreateCoinObject(transform.position + new Vector3(0.35f, 0.15f, 0f));
        }
    }
}
