using UnityEngine;

public class SpriteRotationLock : MonoBehaviour
{
    [Header("Какие оси блокировать")]
    public bool lockX = true;
    public bool lockY = false;  // Y обычно не нужен для 2D спрайта в 3D
    public bool lockZ = true;

    Quaternion _lockedRotation;

    void Start()
    {
        // Запоминаем начальный поворот
        _lockedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // LateUpdate — после всех движений, чтобы перезаписать последним
        Vector3 euler = transform.eulerAngles;

        if (lockX) euler.x = _lockedRotation.eulerAngles.x;
        if (lockY) euler.y = _lockedRotation.eulerAngles.y;
        if (lockZ) euler.z = _lockedRotation.eulerAngles.z;

        transform.eulerAngles = euler;
    }
}