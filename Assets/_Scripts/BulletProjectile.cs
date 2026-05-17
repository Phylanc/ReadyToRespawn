using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BulletProjectile : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    private float _speed;
    private float _lifeTime;
    private Vector3 _velocity;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Make all colliders triggers so bullets don't cause physical collisions
        Collider[] colliders = GetComponentsInChildren<Collider>(includeInactive: true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].isTrigger = true;

        // Remove Rigidbody at runtime so bullet movement is non-physical
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }
    }

    private void Update()
    {
        if (rb != null) return;
        transform.position += _velocity * Time.deltaTime;
    }

    public void Launch(Vector3 direction, float speed, float lifeTime)
    {
        _speed = speed;
        _lifeTime = lifeTime;

        Vector3 dir = direction.normalized;
        _velocity = dir * _speed;

        if (rb != null)
            rb.linearVelocity = _velocity;

        Destroy(gameObject, _lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
