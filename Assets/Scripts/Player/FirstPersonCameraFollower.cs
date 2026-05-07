using UnityEngine;

[DefaultExecutionOrder(10000)]
public class FirstPersonCameraFollower : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        ApplyFollow();
    }

    void OnPreCull()
    {
        ApplyFollow();
    }

    void ApplyFollow()
    {
        if (target == null) return;
        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
