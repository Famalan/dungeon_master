using UnityEngine;
using System.Collections.Generic;

public class RoomSealZone : MonoBehaviour
{
    public Material sealMaterial;
    [Tooltip("Must match DungeonBuilder.tileSize — doorway grid coords are multiplied by this for world position.")]
    public float tileSize = 0.85f;
    [Tooltip("Matches DungeonBuilder.wallHeight for seal wall vertical size.")]
    public float wallHeight = 2.5f;
    public List<Vector2Int> doorwayPositions = new List<Vector2Int>();

    bool isSealed;
    bool wasCleared;
    List<GameObject> sealWalls = new List<GameObject>();
    readonly List<Renderer> sealWallRenderers = new List<Renderer>();
    List<GameObject> trackedEnemies = new List<GameObject>();
    MaterialPropertyBlock sealTintBlock;
    Color sealEmissionPulseBase;

    void OnTriggerEnter(Collider other)
    {
        if (wasCleared) return;
        if (isSealed) return;
        if (!other.CompareTag("Player")) return;

        FindEnemiesInRoom();

        if (trackedEnemies.Count == 0)
        {
            wasCleared = true;
            return;
        }

        SealRoom();
        GameEvents.FireSealNotification("ROOM SEALED!");
    }

    void FindEnemiesInRoom()
    {
        trackedEnemies.Clear();

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Vector3 center = transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject enemyRoot = GetEnemyRootFromCollider(hits[i]);
            if (enemyRoot == null) continue;
            if (!trackedEnemies.Contains(enemyRoot))
            {
                trackedEnemies.Add(enemyRoot);
            }
        }
    }

    GameObject GetEnemyRootFromCollider(Collider hitCollider)
    {
        Transform t = hitCollider.transform;
        while (t != null)
        {
            if (t.CompareTag("Enemy"))
            {
                return t.gameObject;
            }

            t = t.parent;
        }

        return null;
    }

    void SealRoom()
    {
        isSealed = true;
        sealWallRenderers.Clear();

        if (sealMaterial != null)
        {
            sealEmissionPulseBase = sealMaterial.GetColor("_EmissionColor");
            if (sealEmissionPulseBase.maxColorComponent < 0.02f)
            {
                sealEmissionPulseBase = new Color(0.45f, 1.1f, 1.55f, 1f);
            }
        }
        else
        {
            sealEmissionPulseBase = new Color(0.55f, 0.12f, 0.18f, 1f);
        }

        for (int i = 0; i < doorwayPositions.Count; i++)
        {
            Vector2Int pos = doorwayPositions[i];
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "SealWall";
            wall.transform.SetParent(transform);
            float wx = pos.x * tileSize;
            float wz = pos.y * tileSize;
            wall.transform.position = new Vector3(wx, wallHeight * 0.5f, wz);
            wall.transform.localScale = new Vector3(tileSize, wallHeight, tileSize);

            Renderer rend = wall.GetComponent<Renderer>();
            if (sealMaterial != null)
            {
                rend.sharedMaterial = sealMaterial;
            }
            else
            {
                if (sealTintBlock == null)
                {
                    sealTintBlock = new MaterialPropertyBlock();
                }

                Color c = new Color(0.6f, 0.1f, 0.1f, 0.8f);
                rend.GetPropertyBlock(sealTintBlock);
                sealTintBlock.SetColor("_BaseColor", c);
                rend.SetPropertyBlock(sealTintBlock);
            }

            sealWalls.Add(wall);
            sealWallRenderers.Add(rend);
        }
    }

    void LateUpdate()
    {
        if (!isSealed || sealWallRenderers.Count == 0 || sealMaterial == null)
            return;

        if (sealTintBlock == null)
            sealTintBlock = new MaterialPropertyBlock();

        float t = Mathf.Sin(Time.time * 2.75f) * 0.5f + 0.5f;
        float mul = Mathf.Lerp(0.5f, 1.35f, t);
        Color emission = sealEmissionPulseBase * mul;

        for (int i = 0; i < sealWallRenderers.Count; i++)
        {
            Renderer r = sealWallRenderers[i];
            if (r == null) continue;
            sealTintBlock.Clear();
            sealTintBlock.SetColor("_EmissionColor", emission);
            r.SetPropertyBlock(sealTintBlock);
        }
    }

    void Update()
    {
        if (!isSealed) return;

        bool allDead = true;
        for (int i = 0; i < trackedEnemies.Count; i++)
        {
            if (trackedEnemies[i] != null)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            UnsealRoom();
        }
    }

    void UnsealRoom()
    {
        isSealed = false;
        wasCleared = true;

        for (int i = 0; i < sealWalls.Count; i++)
        {
            if (sealWalls[i] != null)
            {
                Destroy(sealWalls[i]);
            }
        }
        sealWalls.Clear();
        sealWallRenderers.Clear();

        GameEvents.FireSealNotification("ROOM CLEARED!");
    }
}
