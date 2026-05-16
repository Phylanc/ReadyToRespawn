using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Анимация")]
    public string dieTrigger = "Die";

    [Header("Звук смерти")]
    public AudioClip deathSound;

    public enum SoundTiming { BeforeAnimation, AfterDelay, WithAnimation }
    public SoundTiming soundTiming = SoundTiming.WithAnimation;

    [Tooltip("Задержка в секундах (для BeforeAnimation — пауза до анимации, для AfterDelay — пауза после старта анимации)")]
    public float soundDelay = 0f;

    [Header("Dissolve")]
    public float dissolveSpeed  = 1.2f;
    public float dissolveDelay  = 0.2f;

    // ── приватное ───────────────────────────────────────────────────
    static readonly int CutoffID = Shader.PropertyToID("_CutoffHeight");
    const string DIE_TRIGGER     = "Die";

    float          _hp;
    bool           _dead;
    Animator       _anim;
    AudioSource    _audioSource;
    SpriteRenderer _sprite;

    void Awake()
    {
        _hp          = maxHP;
        _anim        = GetComponentInChildren<Animator>();
        _sprite      = GetComponentInChildren<SpriteRenderer>();

        // AudioSource — берём существующий или создаём
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;

        if (_sprite == null)
            Debug.LogWarning($"[Enemy] SpriteRenderer не найден на '{name}'");
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;
        _hp -= amount;
        Debug.Log($"[Enemy] '{name}' HP: {_hp:F1} / {maxHP}");
        if (_hp <= 0f) Die();
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        if (TryGetComponent<Collider>(out var col))     col.enabled   = false;
        if (TryGetComponent<Collider2D>(out var col2d)) col2d.enabled = false;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        switch (soundTiming)
        {
            // Звук → пауза → анимация
            case SoundTiming.BeforeAnimation:
                PlaySound();
                Debug.Log($"[Enemy] Звук сыгран. Ждём {soundDelay} сек до анимации...");
                yield return new WaitForSeconds(soundDelay);
                PlayAnimation();
                break;

            // Анимация + звук одновременно
            case SoundTiming.WithAnimation:
                PlayAnimation();
                PlaySound();
                break;

            // Анимация → пауза → звук
            case SoundTiming.AfterDelay:
                PlayAnimation();
                Debug.Log($"[Enemy] Анимация запущена. Ждём {soundDelay} сек до звука...");
                yield return new WaitForSeconds(soundDelay);
                PlaySound();
                break;
        }

        yield return StartCoroutine(DissolveRoutine());
    }

    void PlayAnimation()
    {
        if (_anim != null)
        {
            _anim.SetTrigger(dieTrigger);
            Debug.Log($"[Enemy] Триггер '{dieTrigger}' отправлен");
        }
        else
        {
            Debug.LogWarning($"[Enemy] Animator не найден на '{name}'");
        }
    }

    void PlaySound()
    {
        if (deathSound != null && _audioSource != null)
        {
            _audioSource.clip = deathSound;
            _audioSource.Play();
            Debug.Log($"[Enemy] Звук '{deathSound.name}' воспроизведён");
        }
        else
        {
            Debug.LogWarning($"[Enemy] deathSound не назначен на '{name}'");
        }
    }

    IEnumerator DissolveRoutine()
    {
        yield return new WaitForSeconds(dissolveDelay);

        if (_sprite == null) { gameObject.SetActive(false); yield break; }

        Material mat = _sprite.material;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dissolveSpeed;
            mat.SetFloat(CutoffID, Mathf.Lerp(0f, 1.1f, t));
            yield return null;
        }

        gameObject.SetActive(false);
    }
}