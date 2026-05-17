using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("Свет (найдёт сам если пусто)")]
    public Light flashlight;

    [Header("Aim (только влево/вправо)")]
    [SerializeField] private Transform aimRoot;

    [Header("Фонарик")]
    public KeyCode flashlightKey = KeyCode.F;
    public float activeDuration  = 2f;

    [Header("Урон в конусе")]
    public float  range           = 10f;
    [Range(1f, 89f)]
    public float  coneAngle       = 25f;
    public float  damagePerSecond = 50f;
    public LayerMask enemyLayer;

    [Header("Gizmos")]
    public bool showGizmos = true;

    bool  _isOn;
    float _cosThreshold;
    bool  _facingRight = true;

    Coroutine _damageRoutine;
    Coroutine _autoOffRoutine;

    readonly WaitForSeconds _tick = new WaitForSeconds(0.1f);

    public bool IsOn => _isOn;
    public bool FacingRight => _facingRight;

    void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>(includeInactive: true);

        if (aimRoot == null)
            aimRoot = transform.parent != null ? transform.parent : transform;

        if (flashlight == null)
            Debug.LogError("[Flashlight] Light не найден!");
        else
            flashlight.enabled = false;

        _cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);

        if (enemyLayer.value == 0)
            Debug.LogError("[Flashlight] enemyLayer не назначен!");
    }

    void Update()
    {
        UpdateAim();

        if (Input.GetKeyDown(flashlightKey) && !_isOn) Activate();
        if (Input.GetKeyUp(flashlightKey)   &&  _isOn) Deactivate();
    }

    void UpdateAim()
    {
        float h = Input.GetAxisRaw("Horizontal");

        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f)
                h = stick.x;
        }

        if (h > 0.01f) _facingRight = true;
        else if (h < -0.01f) _facingRight = false;

        Vector3 dir = _facingRight ? aimRoot.right : -aimRoot.right;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void Activate()
    {
        _isOn = true;
        if (flashlight != null) flashlight.enabled = true;
        StopAllRoutines();
        _damageRoutine  = StartCoroutine(DamageTick());
        _autoOffRoutine = StartCoroutine(AutoOff());
    }

    void Deactivate()
    {
        _isOn = false;
        if (flashlight != null) flashlight.enabled = false;
        StopAllRoutines();
    }

    void StopAllRoutines()
    {
        if (_damageRoutine  != null) { StopCoroutine(_damageRoutine);  _damageRoutine  = null; }
        if (_autoOffRoutine != null) { StopCoroutine(_autoOffRoutine); _autoOffRoutine = null; }
    }

    IEnumerator AutoOff()
    {
        yield return new WaitForSeconds(activeDuration);
        Deactivate();
    }

    IEnumerator DamageTick()
    {
        float dmg = damagePerSecond * 0.1f;

        while (_isOn)
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, range, enemyLayer);

            foreach (var col in enemies)
            {
                if (!InCone(col.transform.position)) continue;

                if (col.TryGetComponent<Enemy>(out var e))
                {
                    e.TakeDamage(dmg);
                    e.OnFlashlightHit();
                }
            }

            // Искажение всех объектов
            Collider[] all = Physics.OverlapSphere(transform.position, range);
            foreach (var col in all)
            {
                if (!InCone(col.transform.position)) continue;
                if (col.TryGetComponent<FlashlightDistortable>(out var d))
                    d.Hit();
            }

            yield return _tick;
        }
    }

    bool InCone(Vector3 pos)
    {
        Vector3 dir = (pos - transform.position).normalized;
        return Vector3.Dot(transform.forward, dir) >= _cosThreshold;
    }

    void OnValidate()
    {
        _cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, range);
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, range);
        DrawConeGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
        DrawConeGizmo();
    }

    void DrawConeGizmo()
    {
        Vector3 forward = transform.forward;
        Vector3 origin  = transform.position;

        Vector3[] dirs =
        {
            Quaternion.AngleAxis( coneAngle, transform.up)    * forward,
            Quaternion.AngleAxis(-coneAngle, transform.up)    * forward,
            Quaternion.AngleAxis( coneAngle, transform.right) * forward,
            Quaternion.AngleAxis(-coneAngle, transform.right) * forward,
        };

        Gizmos.color = _isOn ? Color.red : new Color(0.2f, 0.8f, 1f, 0.9f);
        foreach (var d in dirs)
            Gizmos.DrawRay(origin, d * range);

        Gizmos.color = Color.white;
        Gizmos.DrawRay(origin, forward * range);
    }

    string LayerMaskToString(LayerMask mask)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
            if ((mask.value & (1 << i)) != 0)
                sb.Append(LayerMask.LayerToName(i)).Append(" ");
        return sb.ToString().Trim();
    }
}