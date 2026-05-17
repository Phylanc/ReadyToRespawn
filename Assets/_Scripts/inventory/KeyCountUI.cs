using TMPro;
using UnityEngine;

public class KeyCountUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private string format = "Keys: {0}";

    private void Awake()
    {
        if (countText == null)
            countText = GetComponent<TMP_Text>();

        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
    }

    private void OnEnable()
    {
        if (inventory != null)
            inventory.KeyCountChanged += HandleKeyCountChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.KeyCountChanged -= HandleKeyCountChanged;
    }

    private void HandleKeyCountChanged(int newCount)
    {
        SetText(newCount);
    }

    private void Refresh()
    {
        int count = inventory != null ? inventory.KeyCount : 0;
        SetText(count);
    }

    private void SetText(int count)
    {
        if (countText == null) return;
        countText.text = string.Format(format, count);
    }
}
