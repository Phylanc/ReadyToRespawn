using System.Collections;
using UnityEngine;

public class PistolController : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Точка, из которой летят пули. Назначь muzzle/ствол в инспекторе.")]
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Combat")]
    [SerializeField] private float range = 30f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private int magazineSize = 12;
    [SerializeField] private float reloadTime = 1.2f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Input")]
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip reloadClip;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = false;
    [SerializeField] private Color gizmoRayColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color gizmoHitColor = new Color(0.9f, 0.2f, 0.2f, 1f);

    
    private int _ammo;
    private float _nextFireTime;
    private bool _isReloading;
    private bool _isAllowed = true;
    private Coroutine _reloadRoutine;
    private bool _facingRight = true;

    public bool IsAllowed => _isAllowed;
    public bool IsReloading => _isReloading;
    public int Ammo => _ammo;

    private void Awake()
    {
        if (shootOrigin == null)
            shootOrigin = transform.Find("ShootOrigin") ?? transform.Find("Muzzle") ?? transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        _ammo = magazineSize;
    }

    private void OnDisable()
    {
        StopReload();
    }

    private void Update()
    {
        if (!_isAllowed) return;
        if (_isReloading) return;

        if (Input.GetKeyDown(reloadKey) && _ammo < magazineSize)
        {
            StartReload();
            return;
        }

        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            if (_ammo <= 0)
            {
                StartReload();
                return;
            }

            Shoot();
        }
    }

    public void SetAllowed(bool allowed)
    {
        _isAllowed = allowed;
        if (!_isAllowed)
        {
            StopReload();
        }
    }

    public void SetFacingRight(bool facingRight)
    {
        _facingRight = facingRight;
        ApplyAimRotation();
    }

    private void ApplyAimRotation()
    {
        Vector3 dir = _facingRight ? Vector3.right : Vector3.left;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void Shoot()
    {
        _ammo--;
        _nextFireTime = Time.time + fireRate;

        if (audioSource != null && shotClip != null)
            audioSource.PlayOneShot(shotClip);

        FireHitscan();

        if (projectilePrefab != null)
        {
            GameObject projectileObject = Instantiate(projectilePrefab, shootOrigin.position, shootOrigin.rotation);
            if (projectileObject.TryGetComponent<BulletProjectile>(out var projectile))
            {
                projectile.Launch(shootOrigin.forward, projectileSpeed, projectileLifetime);
            }
            else if (projectileObject.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = shootOrigin.forward * projectileSpeed;
                Destroy(projectileObject, projectileLifetime);
            }
        }
    }

    private void FireHitscan()
    {
        if (shootOrigin == null) return;

        Ray ray = new Ray(shootOrigin.position, shootOrigin.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider == null) return;

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
            enemy.TakeDamage(damage);
    }

    private void StartReload()
    {
        if (_reloadRoutine != null) return;
        _reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private void StopReload()
    {
        if (_reloadRoutine != null)
        {
            StopCoroutine(_reloadRoutine);
            _reloadRoutine = null;
        }
        _isReloading = false;
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;

        if (audioSource != null && reloadClip != null)
            audioSource.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);
        _ammo = magazineSize;
        _isReloading = false;
        _reloadRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform origin = shootOrigin != null ? shootOrigin : transform;
        if (origin == null) return;

        Gizmos.color = gizmoRayColor;
        Gizmos.DrawRay(origin.position, origin.forward * range);

        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            Gizmos.color = gizmoHitColor;
            Gizmos.DrawWireSphere(hit.point, 0.08f);
        }
    }
}
