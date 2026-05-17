using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeyPickup : MonoBehaviour
{
    [SerializeField] private int keyAmount = 1;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private bool destroyOnPickup = true;

    private bool _pickedUp;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_pickedUp) return;

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        _pickedUp = true;
        inventory.AddKey(keyAmount);

        // Notify win sequence controller (if present)
        if (WinSequenceController.Instance != null)
            WinSequenceController.Instance.AddKey();

        if (audioSource != null && pickupClip != null)
            audioSource.PlayOneShot(pickupClip);

        if (destroyOnPickup)
        {
            float delay = pickupClip != null ? pickupClip.length : 0f;
            Destroy(gameObject, delay);
        }
    }
}
