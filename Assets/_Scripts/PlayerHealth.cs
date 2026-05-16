using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("Animation")]
    public string attackTrigger = "Attack";
    public string dieTrigger = "Die";

    [Header("Sounds")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    public float deathSoundDelay = 0f;

    public bool IsDead => _dead;

    float _hp;
    bool _dead;
    Animator _anim;
    AudioSource _audio;
    Coroutine _deathRoutine;

    void Awake()
    {
        _hp = maxHP;
        _anim = GetComponentInChildren<Animator>();

        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    public void TakeDamage(float amount)
    {
        if (_dead) return;

        _hp -= amount;
        if (hurtSound != null) _audio.PlayOneShot(hurtSound);

        if (_hp <= 0f && _deathRoutine == null)
            _deathRoutine = StartCoroutine(DeathSequence());
    }

    public void TriggerAttack()
    {
        if (_dead) return;
        _anim?.SetTrigger(attackTrigger);
    }

    IEnumerator DeathSequence()
    {
        if (_dead) yield break;
        _dead = true;

        if (TryGetComponent<PlayerController>(out var controller))
            controller.enabled = false;

        if (deathSound != null)
        {
            _audio.PlayOneShot(deathSound);
            float waitTime = deathSound.length + Mathf.Max(0f, deathSoundDelay);
            if (waitTime > 0f) yield return new WaitForSeconds(waitTime);
        }

        _anim?.SetTrigger(dieTrigger);
    }
}
