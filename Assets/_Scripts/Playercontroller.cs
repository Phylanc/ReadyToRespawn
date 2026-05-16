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
    [SerializeField] private Transform cameraTransform; // перетащи Main Camera сюда

    // ── Компоненты ───────────────────────────────────────────
    private CharacterController _cc;

    // ── Состояние ────────────────────────────────────────────
    private Vector2 _moveInput;      // сырой ввод (WASD или стик)
    private Vector3 _velocity;       // текущая скорость (X/Y/Z)

    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // ── Unity ────────────────────────────────────────────────

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        ReadInput();
        HandleCoyoteTime();
        HandleJumpBuffer();

        Vector3 moveDir = ComputeIsometricMove();
        ApplyMovement(moveDir);
        ApplyGravity();
        RotateTowardsMoveDirection(moveDir);

        _cc.Move(_velocity * Time.deltaTime);
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
        float targetSpeed = moveDir.sqrMagnitude > 0.01f ? moveSpeed : 0f;
        Vector3 targetVel = moveDir * targetSpeed;

        float rate = targetSpeed > 0.01f ? acceleration : deceleration;

        // Плавно меняем только горизонтальную скорость
        _velocity.x = Mathf.MoveTowards(_velocity.x, targetVel.x, rate * Time.deltaTime);
        _velocity.z = Mathf.MoveTowards(_velocity.z, targetVel.z, rate * Time.deltaTime);
    }

    // ── Гравитация и прыжок ───────────────────────────────────

    private void ApplyGravity()
    {
        bool grounded = _cc.isGrounded;

        if (grounded && _velocity.y < 0f)
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
        float gMult = _velocity.y < 0f ? fallMultiplier : 1f;
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

    // ── Гизмо ────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
    }
}