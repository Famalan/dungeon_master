using UnityEngine;

public class LootDrop : MonoBehaviour
{
    public int coinValue = 1;
    public float pickupRadius = 2f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    Transform playerTransform;
    Vector3 startPos;

    static PlayerStats cachedStats;
    static Material coinSharedMaterial;

    void Start()
    {
        startPos = transform.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (cachedStats == null)
        {
            cachedStats = Object.FindAnyObjectByType<PlayerStats>();
        }
    }

    void Update()
    {
        float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + Vector3.up * bobOffset;

        transform.Rotate(Vector3.up * 90f * Time.deltaTime);

        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist < pickupRadius)
        {
            Pickup();
        }
    }

    void Pickup()
    {
        if (cachedStats != null)
        {
            cachedStats.AddCoins(coinValue);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCoinPickup();
        }

        Destroy(gameObject);
    }

    public static GameObject CreateCoinObject(Vector3 position)
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        coin.transform.position = position + Vector3.up * 0.5f;
        coin.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        Renderer renderer = coin.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (coinSharedMaterial == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                coinSharedMaterial = new Material(lit);
                coinSharedMaterial.color = new Color(1f, 0.85f, 0f);
                if (coinSharedMaterial.HasProperty("_EmissionColor"))
                {
                    coinSharedMaterial.EnableKeyword("_EMISSION");
                    coinSharedMaterial.SetColor("_EmissionColor", new Color(1f, 0.85f, 0f) * 0.5f);
                }
            }

            renderer.sharedMaterial = coinSharedMaterial;
        }

        Collider col = coin.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }

        coin.AddComponent<LootDrop>();

        return coin;
    }
}
