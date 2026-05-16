using System.Collections;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Свет (найдёт сам если пусто)")]
    public Light flashlight;

    [Header("Фонарик")]
    public KeyCode flashlightKey = KeyCode.F;
    public float activeDuration  = 2f;

    [Header("Урон в конусе")]
    public float range           = 10f;
    [Range(1f, 89f)]
    public float coneAngle       = 25f;
    public float damagePerSecond = 50f;
    public LayerMask enemyLayer;

    [Header("Gizmos")]
    public bool showGizmos = true;

    bool  _isOn;
    float _cosThreshold;

    Coroutine _damageRoutine;
    Coroutine _autoOffRoutine;

    readonly WaitForSeconds _tick = new WaitForSeconds(0.1f);

    void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>(includeInactive: true);

        if (flashlight == null)
            Debug.LogError("[Flashlight] Light-компонент не найден!");
        else
        {
            flashlight.enabled = false;
            Debug.Log($"[Flashlight] Light найден: {flashlight.name}");
        }

        _cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);

        if (enemyLayer.value == 0)
            Debug.LogError("[Flashlight] enemyLayer = Nothing! Выбери слой врагов в инспекторе.");
        else
            Debug.Log($"[Flashlight] enemyLayer = {LayerMaskToString(enemyLayer)}");
    }

    void Update()
    {
        if (Input.GetKeyDown(flashlightKey) && !_isOn)
            Activate();

        if (Input.GetKeyUp(flashlightKey) && _isOn)
            Deactivate();
    }

    void Activate()
    {
        _isOn = true;
        if (flashlight != null) flashlight.enabled = true;
        StopAllRoutines();
        _damageRoutine  = StartCoroutine(DamageTick());
        _autoOffRoutine = StartCoroutine(AutoOff());
        Debug.Log("[Flashlight] Включен");
    }

    void Deactivate()
    {
        _isOn = false;
        if (flashlight != null) flashlight.enabled = false;
        StopAllRoutines();
        Debug.Log("[Flashlight] Выключен");
    }

    void StopAllRoutines()
    {
        if (_damageRoutine  != null) { StopCoroutine(_damageRoutine);  _damageRoutine  = null; }
        if (_autoOffRoutine != null) { StopCoroutine(_autoOffRoutine); _autoOffRoutine = null; }
    }

    IEnumerator AutoOff()
    {
        yield return new WaitForSeconds(activeDuration);
        Debug.Log("[Flashlight] Время вышло - выключаем");
        Deactivate();
    }

    IEnumerator DamageTick()
    {
        float dmg = damagePerSecond * 0.1f;
        Debug.Log($"[Flashlight] DamageTick запущен. Урон за тик = {dmg}, range = {range}, coneAngle = {coneAngle}");

        while (_isOn)
        {
            // --- Отладка: все коллайдеры вообще без маски ---
            Collider[] allNoMask = Physics.OverlapSphere(transform.position, range);
            Debug.Log($"[Flashlight] Все коллайдеры в радиусе (без маски): {allNoMask.Length}");
            foreach (var c in allNoMask)
                Debug.Log($"  - '{c.name}' | слой: {LayerMask.LayerToName(c.gameObject.layer)} (index {c.gameObject.layer})");

            // --- Урон с маской ---
            Collider[] enemies = Physics.OverlapSphere(transform.position, range, enemyLayer);
            Debug.Log($"[Flashlight] OverlapSphere с enemyLayer нашёл: {enemies.Length}");

            foreach (var col in enemies)
            {
                bool inCone = InCone(col.transform.position);
                float dot   = Vector3.Dot(transform.forward,
                              (col.transform.position - transform.position).normalized);

                Debug.Log($"[Flashlight] Враг '{col.name}' | в конусе: {inCone} | dot: {dot:F2} | порог: {_cosThreshold:F2}");

                if (!inCone) continue;

                if (col.TryGetComponent<Enemy>(out var e))
                {
                    e.TakeDamage(dmg);
                    Debug.Log($"[Flashlight] Нанесли {dmg} урона '{col.name}'");
                }
                else
                {
                    Debug.LogWarning($"[Flashlight] '{col.name}' в enemyLayer но Enemy.cs не найден!");
                }
            }

            // --- Искажение без маски ---
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

    // --- Gizmos ---
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Сфера дальности
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(transform.position, range);
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, range);

        // Конус фонарика
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
        float halfAngle = coneAngle * Mathf.Deg2Rad;
        Vector3 forward = transform.forward;
        Vector3 origin  = transform.position;

        // 4 луча по краям конуса
        Vector3[] directions =
        {
            Quaternion.AngleAxis( coneAngle, transform.up)    * forward,
            Quaternion.AngleAxis(-coneAngle, transform.up)    * forward,
            Quaternion.AngleAxis( coneAngle, transform.right) * forward,
            Quaternion.AngleAxis(-coneAngle, transform.right) * forward,
        };

        Gizmos.color = _isOn ? Color.red : new Color(0.2f, 0.8f, 1f, 0.9f);

        foreach (var dir in directions)
            Gizmos.DrawRay(origin, dir * range);

        // Центральный луч
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