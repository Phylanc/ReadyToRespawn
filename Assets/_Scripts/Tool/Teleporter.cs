using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Teleporter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Тег объекта, который может телепортироваться (например, 'Player')")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("Точка, куда будет перемещен объект")]
    [SerializeField] private Transform teleportDestination;

    private void Awake()
    {
        // Гарантируем, что коллайдер установлен как триггер
        if (TryGetComponent<Collider>(out var collider))
        {
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Проверяем тег вошедшего объекта
        if (other.CompareTag(targetTag))
        {
            TeleportObject(other.gameObject);
        }
    }

    private void TeleportObject(GameObject obj)
    {
        if (teleportDestination == null)
        {
            Debug.LogWarning($"[Teleporter] Точка назначения не назначена на объекте {gameObject.name}!");
            return;
        }

        // Если у персонажа есть CharacterController, его нужно временно отключить
        if (obj.TryGetComponent<CharacterController>(out var controller))
        {
            controller.enabled = false;
            obj.transform.position = teleportDestination.position;
            obj.transform.rotation = teleportDestination.rotation;
            controller.enabled = true;
        }
        // Если используется Rigidbody, сбрасываем его скорость при переносе
        else if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = teleportDestination.position;
            rb.rotation = teleportDestination.rotation;
            rb.linearVelocity = Vector3.zero; // В Unity 6 вместо velocity используется linearVelocity
            rb.angularVelocity = Vector3.zero;
        }
        // Для обычных объектов без физики
        else
        {
            obj.transform.position = teleportDestination.position;
            obj.transform.rotation = teleportDestination.rotation;
        }
    }
}