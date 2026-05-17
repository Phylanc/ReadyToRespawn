using UnityEngine;

public class ProfileEquipmentSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VolumeProfileSwitcher profileSwitcher;
    [SerializeField] private FlashlightController flashlight;
    [SerializeField] private PistolController pistol;
    [SerializeField] private GameObject pistolRoot;
    [SerializeField] private GameObject pistolPrefab;
    [SerializeField] private Transform pistolSocket;
    [SerializeField] private bool spawnOnProfileB = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = false;
    [SerializeField] private Color socketColor = new Color(0.2f, 0.9f, 1f, 1f);
    [SerializeField] private Color pistolColor = new Color(0.95f, 0.6f, 0.15f, 1f);

    private void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<FlashlightController>();

        if (pistol == null)
            pistol = GetComponentInChildren<PistolController>(includeInactive: true);

        // НЕ переназначаем pistolRoot на игрока, если скрипт PistolController висит на игроке!
        if (pistolRoot == null && pistol != null && pistol.gameObject != this.gameObject)
            pistolRoot = pistol.gameObject;

        if (pistolSocket == null)
            pistolSocket = transform;
    }

    private void OnEnable()
    {
        if (profileSwitcher != null)
        {
            profileSwitcher.ProfileChanged += OnProfileChanged;
            OnProfileChanged(profileSwitcher.IsUsingA);
        }
        else
        {
            Debug.LogWarning("[ProfileEquipmentSwitcher] ProfileSwitcher not assigned.");
        }
    }

    private void OnDisable()
    {
        if (profileSwitcher != null)
            profileSwitcher.ProfileChanged -= OnProfileChanged;
    }

    private void OnProfileChanged(bool isProfileA)
    {
        if (!isProfileA && spawnOnProfileB)
            EnsurePistolInstance();

        if (flashlight != null)
            flashlight.SetAllowed(isProfileA);

        if (pistolRoot != null)
            pistolRoot.SetActive(!isProfileA);

        if (pistol != null)
            pistol.SetAllowed(!isProfileA);
    }

    private void EnsurePistolInstance()
    {
        if (pistolRoot != null) return;
        if (pistolPrefab == null) return;

        Transform parent = pistolSocket != null ? pistolSocket : transform;
        pistolRoot = Instantiate(pistolPrefab, parent, false);
        pistolRoot.transform.localPosition = Vector3.zero;
        pistolRoot.transform.localRotation = Quaternion.identity;

        // Ищем PistolController на префабе ТОЛЬКО если до этого скрипта не было
        if (pistol == null)
            pistol = pistolRoot.GetComponentInChildren<PistolController>(includeInactive: true);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Transform socket = pistolSocket != null ? pistolSocket : transform;
        if (socket != null)
        {
            Gizmos.color = socketColor;
            Gizmos.DrawWireSphere(socket.position, 0.08f);
            Gizmos.DrawLine(socket.position, socket.position + socket.forward * 0.4f);
        }

        if (pistolRoot != null)
        {
            Gizmos.color = pistolColor;
            Gizmos.DrawWireCube(pistolRoot.transform.position, Vector3.one * 0.18f);
        }
    }
}
