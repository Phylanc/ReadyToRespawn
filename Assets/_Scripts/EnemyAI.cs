using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    public Transform eyePoint;
    public float eyeHeight = 1.6f;
    public float targetHeight = 1.2f;

    [Header("Vision")]
    public float viewDistance = 10f;
    [Range(0f, 180f)]
    public float viewAngle = 90f;
    public float chaseMemory = 2f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public bool ignoreLineOfSight = false;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float rotateSpeed = 8f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.2f;
    public float attackAnimDuration = 1.0f;
    public float attackRest = 0.4f;

    [Header("Facing")]
    public bool faceCamera = true;
    public bool faceCameraYOnly = true;
    public Transform faceRoot;
    public Transform faceTarget;

    [Header("Sprite Flip")]
    public SpriteRenderer spriteToFlip;
    public bool flipByPlayerX = true;
    public bool invertFlip = false;

    [Header("Debug")]
    public bool showGizmos = true;

    Enemy _enemy;
    Transform _player;
    PlayerHealth _playerHealth;
    float _lastSeenTime = -999f;
    float _nextAttackTime;
    bool _attacking;
    float _nextFindTime;
    bool _warnedNoHealth;
    float _cosViewThreshold;

    static readonly RaycastHit[] RayHits = new RaycastHit[4];

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        if (eyePoint == null) eyePoint = transform;
        _cosViewThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
    }

    void Update()
    {
        if (_enemy != null && _enemy.IsDead) return;

        if (_player == null) TryFindPlayer();
        if (_player == null)
        {
            _enemy?.SetWalk(false);
            return;
        }

        Vector3 toPlayer = _player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;

        UpdateSpriteFlip(toPlayer);

        if (ignoreLineOfSight ? sqrDist <= viewDistance * viewDistance : CanSeePlayer(toPlayer, sqrDist))
            _lastSeenTime = Time.time;

        if (Time.time - _lastSeenTime > chaseMemory)
        {
            _enemy?.SetWalk(false);
            return;
        }

        if (sqrDist > attackRange * attackRange)
        {
            MoveTowards(_player.position);
            _enemy?.SetWalk(true);
        }
        else
        {
            _enemy?.SetWalk(false);
            TryAttack();
        }
    }

    void TryFindPlayer()
    {
        if (Time.time < _nextFindTime) return;
        _nextFindTime = Time.time + 1f;

        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go == null) return;

        _player = go.transform;
        _playerHealth = go.GetComponent<PlayerHealth>();
        if (_playerHealth == null && !_warnedNoHealth)
        {
            Debug.LogWarning("[EnemyAI] PlayerHealth not found on player.");
            _warnedNoHealth = true;
        }
    }

    bool CanSeePlayer(Vector3 toPlayer, float sqrDist)
    {
        if (viewDistance <= 0f) return false;
        if (sqrDist > viewDistance * viewDistance) return false;

        Vector3 dir = toPlayer.normalized;
        if (viewAngle < 180f)
        {
            float dot = Vector3.Dot(transform.forward, dir);
            if (dot < _cosViewThreshold) return false;
        }

        Vector3 origin = eyePoint != null ? eyePoint.position : transform.position + Vector3.up * eyeHeight;
        Vector3 target = _player.position + Vector3.up * targetHeight;
        Vector3 rayDir = (target - origin);
        float dist = rayDir.magnitude;
        if (dist <= 0.01f) return true;

        int mask = obstacleMask | playerMask;
        if (mask == 0)
        {
            int playerLayer = _player.gameObject.layer;
            mask = 1 << playerLayer;
        }

        int hitCount = Physics.RaycastNonAlloc(origin, rayDir.normalized, RayHits, dist, mask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0) return false;

        float bestDist = float.MaxValue;
        Transform bestTransform = null;
        for (int i = 0; i < hitCount; i++)
        {
            var hit = RayHits[i];
            if (hit.collider == null) continue;
            var tr = hit.collider.transform;
            if (tr == transform || tr.IsChildOf(transform)) continue;
            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                bestTransform = tr;
            }
        }

        if (bestTransform == null) return false;
        return bestTransform == _player || bestTransform.IsChildOf(_player);
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        transform.position = Vector3.MoveTowards(transform.position, flatTarget, chaseSpeed * Time.deltaTime);

        Vector3 lookDir = flatTarget - transform.position;
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }

    void UpdateSpriteFlip(Vector3 toPlayer)
    {
        if (!flipByPlayerX) return;
        if (spriteToFlip == null) return;

        bool faceLeft = toPlayer.x < 0f;
        spriteToFlip.flipX = invertFlip ? !faceLeft : faceLeft;
    }

    void TryAttack()
    {
        if (_attacking) return;
        if (Time.time < _nextAttackTime) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        _attacking = true;
        _nextAttackTime = Time.time + attackCooldown + attackRest;

        if (_enemy != null) _enemy.TriggerAttack();

        float delay = Mathf.Max(0f, attackAnimDuration * 0.5f);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (_enemy != null && _enemy.IsDead)
        {
            _attacking = false;
            yield break;
        }

        if (_playerHealth != null && !_playerHealth.IsDead)
        {
            float sqrDist = (_player.position - transform.position).sqrMagnitude;
            if (sqrDist <= attackRange * attackRange)
                _playerHealth.TakeDamage(attackDamage);
        }

        float remaining = Mathf.Max(0f, attackAnimDuration - delay);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        _attacking = false;
    }

    void LateUpdate()
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

    void OnValidate()
    {
        _cosViewThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
