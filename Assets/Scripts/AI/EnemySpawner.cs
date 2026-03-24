using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject gruntPrefab;
    public GameObject rangerPrefab;
    public GameObject tankPrefab;

    List<GameObject> spawnedEnemies = new List<GameObject>();

    public void SpawnEnemies(List<Vector3> spawnPoints, List<int> distances, int dungeonLevel)
    {
        ClearEnemies();

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            int dist = (i < distances.Count) ? distances[i] : 0;
            GameObject prefab = PickEnemyPrefab(dist, dungeonLevel);
            if (prefab == null) continue;

            GameObject enemy = Instantiate(prefab, spawnPoints[i], Quaternion.identity);

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.SetPatrolCenter(spawnPoints[i]);
            }

            MaybeMakeElite(enemy, dungeonLevel);

            spawnedEnemies.Add(enemy);
        }
    }

    void MaybeMakeElite(GameObject enemy, int dungeonLevel)
    {
        float chance = Mathf.Min(0.24f, 0.07f + dungeonLevel * 0.028f);
        if (Random.value > chance)
        {
            return;
        }

        enemy.AddComponent<EliteEnemy>();

        HealthSystem hp = enemy.GetComponent<HealthSystem>();
        if (hp != null)
        {
            hp.maxHealth = Mathf.RoundToInt(hp.maxHealth * 1.7f);
            hp.currentHealth = hp.maxHealth;
        }

        enemy.transform.localScale = enemy.transform.localScale * 1.1f;
    }

    GameObject PickEnemyPrefab(int distanceFromStart, int dungeonLevel)
    {
        if (gruntPrefab == null) return null;

        if (distanceFromStart < 15)
        {
            return gruntPrefab;
        }

        if (distanceFromStart < 30)
        {
            float roll = Random.value;
            if (roll < 0.6f) return gruntPrefab;
            if (rangerPrefab != null) return rangerPrefab;
            return gruntPrefab;
        }

        // Far rooms: all types
        float r = Random.value;
        if (r < 0.4f) return gruntPrefab;
        if (r < 0.7f && rangerPrefab != null) return rangerPrefab;
        if (tankPrefab != null) return tankPrefab;
        return gruntPrefab;
    }

    public void SpawnExitGuardian(Vector3 exitWorldPos, int dungeonLevel)
    {
        GameObject prefab = tankPrefab != null ? tankPrefab : gruntPrefab;
        if (prefab == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = 1.35f;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        Vector3 pos = exitWorldPos + offset;
        pos.y = 0.5f;

        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        enemy.name = "ExitGuardian";

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.SetPatrolCenter(pos);
        }

        HealthSystem hp = enemy.GetComponent<HealthSystem>();
        if (hp != null)
        {
            float mult = 1f + dungeonLevel * 0.09f;
            mult = Mathf.Min(mult, 1.65f);
            hp.maxHealth = Mathf.RoundToInt(hp.maxHealth * mult);
            hp.currentHealth = hp.maxHealth;
        }

        spawnedEnemies.Add(enemy);
    }

    public void ClearEnemies()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }
        spawnedEnemies.Clear();
    }

    public int GetAliveEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                count++;
            }
        }
        return count;
    }
}
