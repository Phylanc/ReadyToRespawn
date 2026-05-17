using System.Collections;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Свет (найдёт сам если пусто)")]
    public Light flashlight;

    [Header("Aim (только влево/вправо)")]
    [SerializeField] private Transform aimRoot;

    [Header("Фонарик")]
    public KeyCode flashlightKey = KeyCode.F;
    public float activeDuration  = 2f;
    public float cooldownDuration = 10f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cooldownClip;
    [SerializeField] private float cooldownSoundInterval = 0.5f;

    [Header("Урон в конусе")]
    public float  range           = 10f;
    [Range(1f, 89f)]
    public float  coneAngle       = 25f;
    public float  damagePerSecond = 50f;
    public LayerMask enemyLayer;

    [Header("Gizmos")]
    public bool showGizmos = true;

    bool  _isOn;
    bool  _isAllowed = true;
    bool  _isCoolingDown;
    float _cosThreshold;
    bool  _facingRight = true;
    float _lastCooldownSoundTime;

    Coroutine _damageRoutine;
    Coroutine _autoOffRoutine;
    Coroutine _cooldownRoutine;

    readonly WaitForSeconds _tick = new WaitForSeconds(0.1f);

    public bool IsOn => _isOn;
    public bool FacingRight => _facingRight;
    public bool IsCoolingDown => _isCoolingDown;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);

        if (enemyLayer.value == 0)
            Debug.LogError("[Flashlight] enemyLayer не назначен!");
    }

    void Update()
    {
        if (!_isAllowed) return;

        if (Input.GetKeyDown(flashlightKey) && !_isOn) Activate();
        if (Input.GetKeyUp(flashlightKey)   &&  _isOn) Deactivate(true);
    }

    public void SetAllowed(bool allowed)
    {
        _isAllowed = allowed;
        if (!_isAllowed)
        {
            Deactivate(false);
        }
    }

    public void SetFacingRight(bool facingRight)
    {
        _facingRight = facingRight;
        ApplyAimRotation();
    }

    void ApplyAimRotation()
    {
        // Поворачиваем по мировой оси X, так как игра 2.5D/Изометрия с плоским спрайтом
        Vector3 dir = _facingRight ? Vector3.right : Vector3.left;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void Activate()
    {
        if (_isCoolingDown)
        {
            PlayCooldownSound();
            return;
        }

        _isOn = true;
        if (flashlight != null) flashlight.enabled = true;
        StopAllRoutines();
        _damageRoutine  = StartCoroutine(DamageTick());
        _autoOffRoutine = StartCoroutine(AutoOff());
    }

    void Deactivate(bool startCooldown)
    {
        _isOn = false;
        if (flashlight != null) flashlight.enabled = false;
        StopAllRoutines();

        if (startCooldown && !_isCoolingDown && _isAllowed)
        {
            PlayCooldownSound();
            _cooldownRoutine = StartCoroutine(Cooldown());
        }
    }

    void StopAllRoutines()
    {
        if (_damageRoutine  != null) { StopCoroutine(_damageRoutine);  _damageRoutine  = null; }
        if (_autoOffRoutine != null) { StopCoroutine(_autoOffRoutine); _autoOffRoutine = null; }
        if (_cooldownRoutine != null) { StopCoroutine(_cooldownRoutine); _cooldownRoutine = null; }
    }

    IEnumerator AutoOff()
    {
        yield return new WaitForSeconds(activeDuration);
        Deactivate(true);
    }

    IEnumerator Cooldown()
    {
        _isCoolingDown = true;
        yield return new WaitForSeconds(cooldownDuration);
        _isCoolingDown = false;
    }

    void PlayCooldownSound()
    {
        if (audioSource == null || cooldownClip == null) return;
        if (Time.time - _lastCooldownSoundTime < cooldownSoundInterval) return;
        _lastCooldownSoundTime = Time.time;
        audioSource.PlayOneShot(cooldownClip);
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