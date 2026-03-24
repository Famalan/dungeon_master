using UnityEngine;

public class ExitPortalTrigger : MonoBehaviour
{
    [Header("Visual")]
    public float rotateSpeed = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.3f;

    [Header("Trigger")]
    [Tooltip("Used if the object has no collider yet (runtime safety).")]
    public float triggerRadius = 2f;

    Vector3 startPos;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = triggerRadius;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameEvents.FirePlayerReachedExit();
    }

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
