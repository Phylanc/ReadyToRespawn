using UnityEngine;

public class FlashlightDisappearOnLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlashlightController controller;
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform flashlightTransform;

    [Header("Controller Settings")]
    [SerializeField] private bool useControllerSettings = true;

    [Header("Detection")]
    [SerializeField] private float requiredLitSeconds = 0.2f;
    [SerializeField] private float maxDistance = 10f;
    [Range(0f, 90f)]
    [SerializeField] private float maxAngle = 25f;
    [SerializeField] private LayerMask occlusionMask = ~0;

    private float litTimer;

    private void Reset()
    {
        controller = FindObjectOfType<FlashlightController>();
        maxDistance = 10f;
        maxAngle = 25f;
        requiredLitSeconds = 0.2f;
        occlusionMask = ~0;
    }

    private void Awake()
    {
        if (controller == null)
            controller = FindObjectOfType<FlashlightController>();
    }

    private void Update()
    {
        if (!IsLitByFlashlight())
        {
            litTimer = 0f;
            return;
        }

        litTimer += Time.deltaTime;
        if (litTimer >= requiredLitSeconds)
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsLitByFlashlight()
    {
        if (controller != null)
        {
            if (!controller.IsOn)
                return false;

            float range = useControllerSettings ? controller.range : maxDistance;
            float coneAngle = useControllerSettings ? controller.coneAngle : maxAngle;
            float cosThreshold = Mathf.Cos(coneAngle * Mathf.Deg2Rad);

            Transform origin = controller.transform;
            Vector3 toTargetVec = transform.position - origin.position;
            float sqrDistance = toTargetVec.sqrMagnitude;
            if (sqrDistance > range * range)
                return false;

            float dot = Vector3.Dot(origin.forward, toTargetVec.normalized);
            if (dot < cosThreshold)
                return false;

            float distanceToTarget = Mathf.Sqrt(sqrDistance);
            Ray rayToTarget = new Ray(origin.position, toTargetVec.normalized);
            if (Physics.Raycast(rayToTarget, out RaycastHit hitInfo, distanceToTarget, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                if (!IsSameTarget(hitInfo.transform))
                    return false;
            }

            return true;
        }

        if (flashlight == null || !flashlight.enabled)
        {
            return false;
        }

        Transform lightTf = flashlightTransform != null ? flashlightTransform : flashlight.transform;
        Vector3 toTarget = transform.position - lightTf.position;
        float distance = toTarget.magnitude;
        if (distance > maxDistance)
        {
            return false;
        }

        float angle = Vector3.Angle(lightTf.forward, toTarget);
        if (angle > maxAngle)
        {
            return false;
        }

        Ray ray = new Ray(lightTf.position, toTarget.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsSameTarget(hit.transform))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSameTarget(Transform hitTransform)
    {
        if (hitTransform == transform) return true;
        if (hitTransform.IsChildOf(transform)) return true;
        if (transform.IsChildOf(hitTransform)) return true;
        return false;
    }
}
