using System;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeProfileSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume targetVolume;

    [Header("Profiles")]
    [SerializeField] private VolumeProfile profileA;
    [SerializeField] private VolumeProfile profileB;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private bool _isUsingA = true;

    public bool IsUsingA => _isUsingA;
    public VolumeProfile CurrentProfile => _isUsingA ? profileA : profileB;
    public event Action<bool> ProfileChanged;

    private void Awake()
    {
        if (targetVolume == null)
        {
            targetVolume = GetComponent<Volume>();
        }

        if (targetVolume == null)
        {
            Debug.LogWarning("[VolumeProfileSwitcher] Volume not assigned and not found on this GameObject.");
            enabled = false;
            return;
        }

        if (profileA == null || profileB == null)
        {
            Debug.LogWarning("[VolumeProfileSwitcher] Assign both profiles in the инспектор.");
            enabled = false;
            return;
        }

        ApplyProfile(_isUsingA ? profileA : profileB);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleProfile();
        }
    }

    public void ToggleProfile()
    {
        _isUsingA = !_isUsingA;
        ApplyProfile(_isUsingA ? profileA : profileB);
    }

    private void ApplyProfile(VolumeProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        targetVolume.profile = profile;
        ProfileChanged?.Invoke(_isUsingA);
    }
}
