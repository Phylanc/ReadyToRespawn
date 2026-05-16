using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Анимация")]
    public string dieTrigger = "Die";
    public string attackTrigger = "Attack";
    public string walkBool = "Walk";

    [Header("Звук смерти")]
    public AudioClip deathSound;

    public enum SoundTiming { BeforeAnimation, AfterDelay, WithAnimation }
    public SoundTiming soundTiming = SoundTiming.WithAnimation;

    [Tooltip("Задержка в секундах")]
    public float soundDelay = 0f;

    [Header("Звук фонарика (луп)")]
    public AudioClip flashlightLoopSound;
    [Range(0f, 1f)]
    public float loopVolume      = 0.7f;
    public float loopFadeOutDelay = 0.2f;

    [Header("Dissolve")]
    public float dissolveSpeed = 1.2f;
    public float dissolveDelay = 0.2f;

    [Header("Debug")]
    [SerializeField] bool debugLogs = false;

    // Читается из EnemyAI
    public bool IsDead => _dead;

    // ── приватное ───────────────────────────────────────────────────
    static readonly int CutoffID = Shader.PropertyToID("_CutoffHeight");

    float          _hp;
    bool           _dead;

    Animator       _anim;
    AudioSource    _deathAudio;
    AudioSource    _loopAudio;
    SpriteRenderer _sprite;
    int            _walkBoolId;

    float _lastHitTime = -999f;
    bool  _loopPlaying;
    Coroutine _fadeCoroutine;

    void Awake()
    {
        _hp     = maxHP;
        _anim   = GetComponentInChildren<Animator>();
        _sprite = GetComponentInChildren<SpriteRenderer>();

        _walkBoolId = Animator.StringToHash(walkBool);

        var bitcrusherTemplate = GetComponent<AudioBitcrusher>();
        if (bitcrusherTemplate != null)
        {
            bitcrusherTemplate.enabled = false;
        }

        _deathAudio = CreateAudioSourceChild("DeathAudio", false, deathSound, 1f, bitcrusherTemplate, true);
        _loopAudio  = CreateAudioSourceChild("LoopAudio", true, flashlightLoopSound, 0f, bitcrusherTemplate, false);
    }

    AudioSource CreateAudioSourceChild(string childName, bool loop, AudioClip clip, float volume, AudioBitcrusher template, bool applyBitcrusher)
    {
        var child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop        = loop;
        source.volume      = volume;
        source.clip        = clip;

        if (applyBitcrusher && template != null)
        {
            var bitcrusher = child.AddComponent<AudioBitcrusher>();
            bitcrusher.bitDepth  = template.bitDepth;
            bitcrusher.downsample = template.downsample;
            bitcrusher.mix = template.mix;
        }

        return source;
    }

    // ── Вызывается из FlashlightController ─────────────────────────
    public void OnFlashlightHit()
    {
        if (_dead) return;

        _lastHitTime = Time.time;

        if (flashlightLoopSound == null) return;

        if (!_loopPlaying)
        {
            _loopPlaying = true;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (!_loopAudio.isPlaying) _loopAudio.Play();

            _fadeCoroutine = StartCoroutine(FadeLoop(0f, loopVolume, 0.1f));
        }
    }

    void Update()
    {
        if (_dead || flashlightLoopSound == null) return;

        if (_loopPlaying && Time.time - _lastHitTime > loopFadeOutDelay)
        {
            _loopPlaying = false;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeLoop(loopVolume, 0f, 0.3f));
        }
    }

    IEnumerator FadeLoop(float from, float to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            _loopAudio.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }
        _loopAudio.volume = to;
        if (to <= 0f && _loopAudio.isPlaying) _loopAudio.Stop();
    }

    // ── Урон ────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (_dead) return;
        _hp -= amount;
        if (debugLogs) Debug.Log($"[Enemy] '{name}' HP: {_hp:F1} / {maxHP}");
        if (_hp <= 0f) Die();
    }

    // ── Смерть ──────────────────────────────────────────────────────
    void Die()
    {
        if (_dead) return;
        _dead = true;

        StopAllCoroutines();
        _loopAudio.Stop();

        if (TryGetComponent<Collider>(out var col))     col.enabled   = false;
        if (TryGetComponent<Collider2D>(out var col2d)) col2d.enabled = false;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        switch (soundTiming)
        {
            case SoundTiming.BeforeAnimation:
                PlayDeathSound();
                yield return new WaitForSeconds(soundDelay);
                PlayAnimation();
                break;

            case SoundTiming.WithAnimation:
                PlayAnimation();
                PlayDeathSound();
                break;

            case SoundTiming.AfterDelay:
                PlayAnimation();
                yield return new WaitForSeconds(soundDelay);
                PlayDeathSound();
                break;
        }

        yield return StartCoroutine(DissolveRoutine());
    }

    void PlayAnimation()
    {
        _anim?.SetTrigger(dieTrigger);
        if (debugLogs) Debug.Log($"[Enemy] Триггер '{dieTrigger}' отправлен");
    }

    public void TriggerAttack()
    {
        if (_dead) return;
        _anim?.SetTrigger(attackTrigger);
        if (debugLogs) Debug.Log($"[Enemy] Триггер '{attackTrigger}' отправлен");
    }

    public void SetWalk(bool value)
    {
        if (_dead) return;
        if (_anim == null) return;
        if (string.IsNullOrWhiteSpace(walkBool)) return;
        _anim.SetBool(_walkBoolId, value);
    }

    void PlayDeathSound()
    {
        if (deathSound == null) return;
        _deathAudio.clip = deathSound;
        _deathAudio.Play();
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