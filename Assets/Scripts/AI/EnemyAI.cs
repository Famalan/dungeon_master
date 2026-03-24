using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack
}

public enum EnemyType
{
    Grunt,
    Ranger,
    Tank
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Type")]
    public EnemyType enemyType = EnemyType.Grunt;

    [Header("Detection")]
    public float detectionRange = 12f;
    public float attackRange = 2f;
    public float loseTargetRange = 18f;

    [Header("Patrol")]
    public float patrolRadius = 8f;
    public float patrolWaitTime = 2f;

    [Header("Combat")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    [Header("Ranged (Ranger only)")]
    public GameObject projectilePrefab;
    public float preferredDistance = 8f;

    [Header("References")]
    public Transform playerTarget;

    NavMeshAgent agent;
    Animator animator;
    EnemyState currentState;
    float patrolTimer;
    float attackTimer;
    Vector3 patrolCenter;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentState = EnemyState.Patrol;
        patrolCenter = transform.position;
        patrolTimer = 0f;
        attackTimer = 0f;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (playerTarget == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol(distToPlayer);
                break;
            case EnemyState.Chase:
                UpdateChase(distToPlayer);
                break;
            case EnemyState.Attack:
                UpdateAttack(distToPlayer);
                break;
        }

        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);
    }

    void UpdatePatrol(float distToPlayer)
    {
        if (distToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                PickNewPatrolPoint();
                patrolTimer = 0f;
            }
        }
    }

    void UpdateChase(float distToPlayer)
    {
        if (distToPlayer > loseTargetRange)
        {
            currentState = EnemyState.Patrol;
            PickNewPatrolPoint();
            return;
        }

        if (enemyType == EnemyType.Ranger)
        {
            if (distToPlayer <= attackRange)
            {
                currentState = EnemyState.Attack;
                agent.ResetPath();
                return;
            }

            if (distToPlayer <= preferredDistance)
            {
                agent.ResetPath();
                currentState = EnemyState.Attack;
                return;
            }
        }
        else
        {
            if (distToPlayer <= attackRange)
            {
                currentState = EnemyState.Attack;
                agent.ResetPath();
                return;
            }
        }

        agent.SetDestination(playerTarget.position);
    }

    void UpdateAttack(float distToPlayer)
    {
        if (enemyType == EnemyType.Ranger)
        {
            if (distToPlayer > preferredDistance * 1.3f)
            {
                currentState = EnemyState.Chase;
                return;
            }

            if (distToPlayer < preferredDistance * 0.5f)
            {
                Vector3 awayDir = (transform.position - playerTarget.position).normalized;
                Vector3 retreatPos = transform.position + awayDir * 3f;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(retreatPos, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                agent.ResetPath();
            }
        }
        else
        {
            if (distToPlayer > attackRange * 1.5f)
            {
                currentState = EnemyState.Chase;
                return;
            }
        }

        transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;

            if (animator != null)
            {
                animator.SetTrigger("attack");
            }

            if (enemyType == EnemyType.Ranger)
            {
                ShootProjectile();
            }
            else
            {
                DealMeleeDamage();
            }
        }
    }

    void DealMeleeDamage()
    {
        float dist = Vector3.Distance(transform.position, playerTarget.position);
        if (dist > attackRange * 1.5f) return;

        HealthSystem playerHealth = playerTarget.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f + transform.forward * 0.5f;
        Vector3 dir = (playerTarget.position + Vector3.up * 1f - spawnPos).normalized;
        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject proj = Instantiate(projectilePrefab, spawnPos, rot);

        Projectile projScript = proj.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.damage = attackDamage;
            projScript.firedByPlayer = false;
        }
    }

    void PickNewPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += patrolCenter;
        randomDir.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    public void SetPatrolCenter(Vector3 center)
    {
        patrolCenter = center;
    }
}
