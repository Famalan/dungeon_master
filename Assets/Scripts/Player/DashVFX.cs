using UnityEngine;

/// <summary>
/// Attach to the Player GameObject alongside PlayerController.
/// Handles dash visual and audio feedback by responding to OnDashStart / OnDashEnd calls
/// from PlayerController.
/// </summary>
public class DashVFX : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem dashTrailParticles;

    [Header("SFX")]
    [SerializeField] private AudioClip dashSFX;

    /// <summary>Called by PlayerController at the start of a dash.</summary>
    public void OnDashStart()
    {
        if (dashTrailParticles != null)
        {
            dashTrailParticles.Play();
        }

        if (AudioManager.Instance != null && dashSFX != null)
        {
            AudioManager.Instance.PlaySFX(dashSFX);
        }
    }

    /// <summary>Called by PlayerController when the dash duration ends.</summary>
    public void OnDashEnd()
    {
        if (dashTrailParticles != null)
        {
            dashTrailParticles.Stop();
        }
    }
}
