using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Смерть")]
    public float disappearDelay = 1.5f;

    const string DIE_TRIGGER = "Die";

    float       _hp;
    bool        _dead;
    Animator    _anim;
    AudioSource _audio;

    void Awake()
    {
        _hp    = maxHP;
        _anim  = GetComponentInChildren<Animator>();
        _audio = GetComponent<AudioSource>();

        Debug.Log($"[Enemy] '{name}' инициализирован | HP = {_hp} | Layer = {LayerMask.LayerToName(gameObject.layer)}");

        if (_anim == null)
            Debug.LogWarning($"[Enemy] ⚠️ '{name}' — Animator не найден!");
        else
            Debug.Log($"[Enemy] ✅ Animator найден: {_anim.name}");
    }

    public void TakeDamage(float amount)
    {
        if (_dead)
        {
            Debug.Log($"[Enemy] '{name}' уже мёртв — урон проигнорирован");
            return;
        }

        _hp -= amount;
        Debug.Log($"[Enemy] '{name}' получил {amount} урона | HP: {_hp + amount:F1} → {_hp:F1} / {maxHP}");

        if (_hp <= 0f)
            Die();
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        Debug.Log($"[Enemy] '{name}' ☠️ УМЕР");

        if (_anim != null)
        {
            _anim.SetTrigger(DIE_TRIGGER);
            Debug.Log($"[Enemy] ▶️ Триггер '{DIE_TRIGGER}' отправлен в Animator");
        }
        else
        {
            Debug.LogWarning($"[Enemy] ⚠️ Animator отсутствует — анимация смерти не сыграет");
        }

        if (_audio != null)
            _audio.Play();
        else
            Debug.LogWarning($"[Enemy] ⚠️ AudioSource отсутствует — звук смерти не сыграет");

        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
            Debug.Log($"[Enemy] 🔒 Коллайдер отключён");
        }
        else
        {
            Debug.LogWarning($"[Enemy] ⚠️ Коллайдер не найден — враг будет продолжать получать урон!");
        }

        StartCoroutine(DisappearAfter());
    }

    IEnumerator DisappearAfter()
    {
        Debug.Log($"[Enemy] '{name}' исчезнет через {disappearDelay} сек...");
        yield return new WaitForSeconds(disappearDelay);
        Debug.Log($"[Enemy] '{name}' SetActive(false)");
        gameObject.SetActive(false);
    }
}