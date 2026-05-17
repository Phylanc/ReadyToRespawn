using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed     = 6f;
    [SerializeField] private float acceleration  = 14f;
    [SerializeField] private float deceleration  = 20f;
    [SerializeField] private float rotationSpeed = 12f;   // скорость поворота персонажа

    [Header("Прыжок / Гравитация")]
    [SerializeField] private float jumpHeight     = 1.8f;
    [SerializeField] private float gravity        = -20f;
    [SerializeField] private float fallMultiplier = 2.2f;  // быстрее падаем вниз
    [SerializeField] private float coyoteTime     = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Isometric камера")]
    [SerializeField] private Transform cameraTransform; 

    [Header("Следить за камерой")]
    [SerializeField] private bool faceCamera = false;
    [SerializeField] private bool faceCameraYOnly = true;
    [SerializeField] private Transform faceRoot;
    [SerializeField] private Transform faceTarget;

    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer spriteToFlip;
    [SerializeField] private bool invertFlip = false;
    [SerializeField] private SpriteRenderer weaponSpriteToFlip;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private FlashlightController flashlightController;

    [Header("Перекат")]
    [SerializeField] private KeyCode rollKey = KeyCode.LeftShift;
    [SerializeField] private float rollSpeed = 10f;
    [SerializeField] private float rollDuration = 0.35f;
    [SerializeField] private float rollCooldown = 0.75f;
    [SerializeField] private Animator animator;
    [SerializeField] private string rollBoolParam = "IsRolling";
    [SerializeField] private AudioSource rollAudioSource;
    [SerializeField] private AudioClip rollClip;
    
    
    // ── Компоненты ───────────────────────────────────────────
    private CharacterController _cc;

    public bool isClimbing = false;

    // ── Состояние ────────────────────────────────────────────
    private Vector2 _moveInput;      // сырой ввод (WASD или стик)
    private Vector3 _velocity;       // текущая скорость (X/Y/Z)

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _facingRight = true;
    private Vector3 _lastMoveDir;
    private bool _isRolling;
    private float _rollTimer;
    private float _rollCooldownTimer;
    private Vector3 _rollDir;

    // ── Unity ────────────────────────────────────────────────

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (spriteToFlip == null)
            spriteToFlip = GetComponentInChildren<SpriteRenderer>();

        if (flashlightController == null)
            flashlightController = GetComponentInChildren<FlashlightController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rollAudioSource == null)
            rollAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        ReadInput();
        HandleCoyoteTime();
        HandleJumpBuffer();
        UpdateRollTimers();

        TryStartRoll();

        Vector3 moveDir = ComputeIsometricMove();
        _lastMoveDir = moveDir;
        if (_isRolling)
        {
            _velocity.x = _rollDir.x * rollSpeed;
            _velocity.z = _rollDir.z * rollSpeed;
        }
        else
        {
            ApplyMovement(moveDir);
        }

        ApplyGravity();

        // Не крутим физически рутовую капсулу, если включено отслеживание камеры (2D спрайт)
        if (!faceCamera && !_isRolling)
        {
            RotateTowardsMoveDirection(moveDir);
        }

        UpdateSpriteFacing();

        if (isClimbing)
        {
            _velocity.y = 0f; // Убираем гравитацию на лестнице
            _cc.Move(new Vector3(_velocity.x, 0, _velocity.z) * Time.deltaTime);
        }
        else
        {
            _cc.Move(_velocity * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        FaceCameraIfEnabled();
    }

    // ── Ввод ─────────────────────────────────────────────────

    private void ReadInput()
    {
        // Клавиатура
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Геймпад — левый стик перекрывает клавиатуру если отклонён
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                h = stick.x;
                v = stick.y;
            }
        }

        _moveInput = new Vector2(h, v);

        // Нормализуем чтобы диагональ не была быстрее
        if (_moveInput.sqrMagnitude > 1f)
            _moveInput.Normalize();

        // Прыжок: Space или кнопка South (крест/A) на геймпаде
        bool jumpKey = Input.GetKeyDown(KeyCode.Space);
        bool jumpPad = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        if (jumpKey || jumpPad)
            _jumpBufferTimer = jumpBufferTime;
    }

    private void TryStartRoll()
    {
        if (_isRolling) return;
        if (isClimbing) return;
        if (_rollCooldownTimer > 0f) return;
        if (!_cc.isGrounded) return;

        if (Input.GetKeyDown(rollKey))
        {
            Vector3 moveDir = ComputeIsometricMove();
            if (moveDir.sqrMagnitude < 0.01f)
                moveDir = transform.forward;
            moveDir.y = 0f;
            moveDir.Normalize();

            _rollDir = moveDir;
            _rollTimer = rollDuration;
            _rollCooldownTimer = rollCooldown;
            _isRolling = true;

            if (animator != null && !string.IsNullOrEmpty(rollBoolParam))
                animator.SetBool(rollBoolParam, true);

            if (rollAudioSource != null && rollClip != null)
                rollAudioSource.PlayOneShot(rollClip);
        }
    }

    private void UpdateRollTimers()
    {
        if (_rollCooldownTimer > 0f)
            _rollCooldownTimer -= Time.deltaTime;

        if (_isRolling)
        {
            _rollTimer -= Time.deltaTime;
            if (_rollTimer <= 0f)
            {
                _isRolling = false;
                if (animator != null && !string.IsNullOrEmpty(rollBoolParam))
                    animator.SetBool(rollBoolParam, false);
            }
        }
    }

    // ── Isometric проекция ────────────────────────────────────

    /// Переводим плоский ввод в направление относительно isometric-камеры.
    private Vector3 ComputeIsometricMove()
    {
        if (_moveInput.sqrMagnitude < 0.01f)
            return Vector3.zero;

        // Берём направления камеры, убираем наклон по Y
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Итоговое направление в мировых координатах
        return camForward * _moveInput.y + camRight * _moveInput.x;
    }

    // ── Движение ─────────────────────────────────────────────

    private void ApplyMovement(Vector3 moveDir)
    {
        // Плавное управление горизонтальной скоростью без падения до нуля
        Vector3 currentHor = new Vector3(_velocity.x, 0f, _velocity.z);
        Vector3 desired = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized * moveSpeed : Vector3.zero;
        float rate = moveDir.sqrMagnitude > 0.01f ? acceleration : deceleration;

        Vector3 newHor = Vector3.MoveTowards(currentHor, desired, rate * Time.deltaTime);
        _velocity.x = newHor.x;
        _velocity.z = newHor.z;
    }

    // ── Гравитация и прыжок ───────────────────────────────────

    private void ApplyGravity()
    {
        bool grounded = _cc.isGrounded;

        // Если CharacterController еще не понял, что на земле (потому что скорость Y в нуле)
        if (!grounded && _velocity.y <= 0f)
        {
            grounded = Physics.SphereCast(transform.position + Vector3.up * (_cc.radius + 0.1f), 
                                          _cc.radius, Vector3.down, out _, 0.2f);
        }

        if (grounded && _velocity.y <= 0f)
        {
            _velocity.y  = -2f;          // прижимаем к земле
            _coyoteTimer = coyoteTime;   // сбрасываем coyote
        }

        // Прыжок
        bool canJump   = _coyoteTimer > 0f;
        bool wantsJump = _jumpBufferTimer > 0f;

        if (canJump && wantsJump)
        {
            // Формула: v = sqrt(2 * |g| * h)
            _velocity.y      = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            _jumpBufferTimer = 0f;
            _coyoteTimer     = 0f;
        }

        // Улучшенная гравитация: быстрее падаем
        float gMult = _velocity.y < 0f && !grounded ? fallMultiplier : 1f;
        _velocity.y += gravity * gMult * Time.deltaTime;
    }

    private void HandleCoyoteTime()
    {
        if (_cc.isGrounded)
            _coyoteTimer = coyoteTime;
        else
            _coyoteTimer -= Time.deltaTime;
    }

    private void HandleJumpBuffer()
    {
        _jumpBufferTimer -= Time.deltaTime;
    }

    // ── Поворот ───────────────────────────────────────────────

    /// Персонаж плавно поворачивается в сторону движения.
    private void RotateTowardsMoveDirection(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );
    }

    private void FaceCameraIfEnabled()
    {
        if (!faceCamera) return;

        Transform target = faceTarget;
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
        if (target == null) return;

        Transform root = faceRoot != null ? faceRoot : transform;
        Vector3 dir = target.position - root.position;
        if (faceCameraYOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        root.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void UpdateSpriteFacing()
    {
        if (spriteToFlip == null) return;

        float axis = _moveInput.x;
        if (cameraTransform != null && _lastMoveDir.sqrMagnitude > 0.0001f)
            axis = Vector3.Dot(_lastMoveDir.normalized, cameraTransform.right);

        if (axis > 0.01f) _facingRight = true;
        else if (axis < -0.01f) _facingRight = false;

        bool faceLeft = !_facingRight;
        spriteToFlip.flipX = invertFlip ? !faceLeft : faceLeft;

        if (weaponSpriteToFlip != null)
            weaponSpriteToFlip.flipX = invertFlip ? !faceLeft : faceLeft;

        if (weaponRoot != null)
        {
            Vector3 scale = weaponRoot.localScale;
            float xSign = faceLeft ? -1f : 1f;
            weaponRoot.localScale = new Vector3(Mathf.Abs(scale.x) * xSign, scale.y, scale.z);
        }

        if (flashlightController != null)
            flashlightController.SetFacingRight(_facingRight);
    }

    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;

        if (climbing)
        {
            _velocity = Vector3.zero;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _isRolling = false;

            if (animator != null && !string.IsNullOrEmpty(rollBoolParam))
                animator.SetBool(rollBoolParam, false);
        }
    }

    public void StopClimbingMotion()
    {
        isClimbing = false;
        _velocity.y = 0f;
    }

    // ── Гизмо ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}