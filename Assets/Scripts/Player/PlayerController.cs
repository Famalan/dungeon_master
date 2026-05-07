using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.5f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    public Transform cameraHolder;

    [Header("Combat")]
    public ProjectileShooter shooter;
    public MeleeWeaponController meleeWeapon;

    [Header("Dash")]
    public float dashDistance = 6f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.5f;
    public float dashIFrames = 0.2f;

    [Header("Dash VFX")]
    [SerializeField] private DashVFX dashVFX;

    [Header("Audio")]
    [SerializeField] private float footstepInterval = 0.38f;

    CharacterController characterController;
    HealthSystem healthSystem;
    Vector2 moveInput;
    Vector2 lookInput;
    float verticalVelocity;
    float cameraPitch;
    bool isSprinting;
    bool jumpRequested;

    bool isDashing;
    float dashTimer;
    [HideInInspector] public float dashCooldownTimer;
    float iFrameTimer;
    float footstepTimer;
    Vector3 dashDirection;

    public bool IsInvulnerable { get { return iFrameTimer > 0f; } }

    void Awake()
    {
        CacheComponents();
    }

    void Start()
    {
        CacheComponents();
    }

    void CacheComponents()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }
    }

    public void SetActive(bool active)
    {
        CacheComponents();

        // Пока PlayerInput выключен, Input System не шлёт отпускание клавиш —
        // moveInput/lookInput остаются старыми и после рестарта/уровня дают «залипшее» движение.
        if (isDashing && dashVFX != null)
        {
            dashVFX.OnDashEnd();
        }

        enabled = active;

        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = active;
        }

        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        isSprinting = false;
        jumpRequested = false;
        isDashing = false;
        dashTimer = 0f;
    }

    void Update()
    {
        CacheComponents();

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (iFrameTimer > 0f)
        {
            iFrameTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            UpdateDash();
        }
        else
        {
            HandleMovement();
        }

        HandleLook();
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        if (characterController == null) return;
        if (!characterController.isGrounded) return;
        if (moveInput.sqrMagnitude < 0.1f)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f) return;

        footstepTimer = isSprinting ? footstepInterval * 0.72f : footstepInterval;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFootstep();
        }
    }

    void HandleMovement()
    {
        if (characterController == null)
        {
            return;
        }

        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        Vector3 forward = transform.forward * moveInput.y;
        Vector3 right = transform.right * moveInput.x;
        Vector3 horizontalMove = (forward + right).normalized * currentSpeed;

        if (characterController.isGrounded)
        {
            verticalVelocity = -2f;

            if (jumpRequested)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpRequested = false;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 move = horizontalMove + Vector3.up * verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    void StartDash()
    {
        if (dashCooldownTimer > 0f) return;
        if (isDashing) return;

        Vector3 inputDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        if (inputDir.sqrMagnitude < 0.01f)
        {
            inputDir = transform.forward;
        }
        dashDirection = inputDir.normalized;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        iFrameTimer = dashIFrames;

        if (dashVFX != null) dashVFX.OnDashStart();
    }

    void UpdateDash()
    {
        if (characterController == null)
        {
            isDashing = false;
            if (dashVFX != null) dashVFX.OnDashEnd();
            return;
        }

        dashTimer -= Time.deltaTime;

        float dashSpeed = dashDistance / dashDuration;
        Vector3 move = dashDirection * dashSpeed * Time.deltaTime;
        characterController.Move(move);

        if (dashTimer <= 0f)
        {
            isDashing = false;
            if (dashVFX != null) dashVFX.OnDashEnd();
        }
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void OnAttack(InputValue value)
    {
        if (!value.isPressed) return;
        if (meleeWeapon != null)
        {
            meleeWeapon.TryAttack();
            return;
        }

        if (shooter != null)
        {
            shooter.Shoot();
        }
    }

    void OnPrevious(InputValue value)
    {
        if (!value.isPressed) return;
        if (meleeWeapon != null)
        {
            meleeWeapon.CycleWeapon(-1);
            return;
        }

        if (shooter != null)
        {
            shooter.CycleWeapon(-1);
        }
    }

    void OnNext(InputValue value)
    {
        if (!value.isPressed) return;
        if (meleeWeapon != null)
        {
            meleeWeapon.CycleWeapon(1);
            return;
        }

        if (shooter != null)
        {
            shooter.CycleWeapon(1);
        }
    }

    void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        jumpRequested = true;
    }

    void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    void OnDash(InputValue value)
    {
        if (!value.isPressed) return;
        StartDash();
    }

    public void TeleportTo(Vector3 position)
    {
        CacheComponents();

        bool wasControllerEnabled = characterController != null && characterController.enabled;
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.position = position;
        verticalVelocity = 0f;

        if (characterController != null)
        {
            characterController.enabled = wasControllerEnabled;
        }
    }
}
