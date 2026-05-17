using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int keyCount = 0;

    public int KeyCount => keyCount;

    public event Action<int> KeyCountChanged;

    public void AddKey(int amount = 1)
    {
        if (amount <= 0) return;

        keyCount += amount;
        KeyCountChanged?.Invoke(keyCount);
    }

    public bool HasKeys(int amount = 1)
    {
        return keyCount >= amount;
    }

    public bool TrySpendKeys(int amount = 1)
    {
        if (amount <= 0) return true;
        if (keyCount < amount) return false;

        keyCount -= amount;
        KeyCountChanged?.Invoke(keyCount);
        return true;
    }
}
