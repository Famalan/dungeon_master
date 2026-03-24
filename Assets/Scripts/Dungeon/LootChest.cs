using UnityEngine;

public class LootChest : MonoBehaviour
{
    public AudioClip openSound;
    public int minCoins = 3;
    public int maxCoins = 8;

    bool opened;

    void OnTriggerEnter(Collider other)
    {
        if (opened) return;
        if (!other.CompareTag("Player")) return;

        opened = true;

        int amount = Random.Range(minCoins, maxCoins + 1);
        PlayerStats stats = Object.FindAnyObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.AddCoins(amount);
        }

        if (openSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPoint(openSound, transform.position, 0.95f);
        }

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 0.15f);
    }
}
