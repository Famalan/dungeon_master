using UnityEngine;

public class WeaponViewModelController : MonoBehaviour
{
    public GameObject swordModel;
    public GameObject axeModel;
    public GameObject spearModel;
    public GameObject hammerModel;
    public GameObject bowModel;

    public Vector3 restLocalPosition = new Vector3(0.72f, -0.46f, 1.08f);
    public Vector3 restLocalEuler = new Vector3(12f, -34f, 9f);
    public Vector3 swingLocalOffset = new Vector3(-0.34f, 0.16f, 0.28f);
    public Vector3 swingLocalEulerOffset = new Vector3(58f, 52f, -36f);
    public float visibleScaleMultiplier = 1.28f;

    [Header("Idle Motion")]
    public float idleBobAmplitude = 0.025f;
    public float idleBobSpeed = 1.7f;

    MeleeWeaponMode currentMode = MeleeWeaponMode.Sword;
    float swingTimer;
    float swingDuration = 0.18f;

    void Awake()
    {
        ApplyModeVisibility();
        ApplyPose(0f);
    }

    void Update()
    {
        if (swingTimer > 0f)
        {
            swingTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(1f - swingTimer / Mathf.Max(0.01f, swingDuration));
            float punch = Mathf.Sin(t * Mathf.PI);
            if (currentMode == MeleeWeaponMode.Bow)
            {
                punch = Mathf.Sin(t * Mathf.PI * 0.5f);
            }
            ApplyPose(punch);
        }
        else
        {
            ApplyPose(0f);
        }
    }

    public void SetMode(MeleeWeaponMode mode)
    {
        currentMode = mode;
        ApplyModeVisibility();
        ApplyPose(0f);
    }

    public void PlaySwing(MeleeWeaponMode mode, float duration)
    {
        SetMode(mode);
        swingDuration = Mathf.Max(0.05f, duration);
        swingTimer = swingDuration;
    }

    void ApplyModeVisibility()
    {
        SetActive(swordModel, currentMode == MeleeWeaponMode.Sword);
        SetActive(axeModel, currentMode == MeleeWeaponMode.Axe);
        SetActive(spearModel, currentMode == MeleeWeaponMode.Spear);
        SetActive(hammerModel, currentMode == MeleeWeaponMode.Hammer);
        SetActive(bowModel, currentMode == MeleeWeaponMode.Bow);
    }

    void ApplyPose(float swing)
    {
        Vector3 idle = Vector3.up * (Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmplitude);
        Vector3 modeOffset = currentMode == MeleeWeaponMode.Bow ? new Vector3(-0.1f, 0.02f, 0.1f) : Vector3.zero;
        Vector3 modeSwingOffset = currentMode == MeleeWeaponMode.Bow
            ? new Vector3(-0.08f, -0.02f, -0.16f)
            : swingLocalOffset;
        Vector3 modeSwingEuler = currentMode == MeleeWeaponMode.Bow
            ? new Vector3(-8f, 0f, -11f)
            : swingLocalEulerOffset;

        transform.localPosition = restLocalPosition + modeOffset + idle + modeSwingOffset * swing;
        transform.localRotation = Quaternion.Euler(restLocalEuler + modeSwingEuler * swing);
        transform.localScale = Vector3.one * Mathf.Max(0.1f, visibleScaleMultiplier);
    }

    void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
