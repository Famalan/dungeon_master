using UnityEngine;
using Unity.Cinemachine;

public class KillCamShake : MonoBehaviour
{
    CinemachineImpulseSource impulseSource;

    void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += OnEnemyKilled;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
    }

    void OnEnemyKilled()
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(0.22f);
    }
}
