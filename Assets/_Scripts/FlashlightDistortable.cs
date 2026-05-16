using UnityEngine;

public class FlashlightDistortable : MonoBehaviour
{
    [Header("Настройки оверлея")]
    public float fadeSpeed    = 2.5f;
    public Color overlayColor = new Color(0.4f, 0f, 0.8f, 1f);
    public Shader overlayShader;

    float      _current;
    bool       _hitThisFrame;

    Material       _overlayMat;
    SpriteRenderer _shellSprite;
    GameObject     _shellObject;

    SpriteRenderer _sourceSprite;

    static readonly int DistortID      = Shader.PropertyToID("_DistortAmount");
    static readonly int OverlayColorID = Shader.PropertyToID("_OverlayColor");

    void Awake()
    {
        // Ищем SpriteRenderer
        _sourceSprite = GetComponent<SpriteRenderer>();
        if (_sourceSprite == null)
            _sourceSprite = GetComponentInChildren<SpriteRenderer>();

        if (_sourceSprite == null)
        {
            Debug.LogError($"[Distortable] SpriteRenderer не найден на '{name}'");
            enabled = false;
            return;
        }

        // Ищем шейдер
        if (overlayShader == null)
            overlayShader = Shader.Find("Custom/FlashlightSpriteOverlay");

        if (overlayShader == null)
        {
            Debug.LogError("[Distortable] Шейдер Custom/FlashlightSpriteOverlay не найден!");
            enabled = false;
            return;
        }

        // Создаём материал оверлея
        _overlayMat = new Material(overlayShader);
        _overlayMat.SetColor(OverlayColorID, overlayColor);
        _overlayMat.SetFloat(DistortID, 0f);

        // Создаём дочерний объект-копию спрайта
        _shellObject = new GameObject("_FlashlightOverlayShell");
        _shellObject.transform.SetParent(_sourceSprite.transform, worldPositionStays: false);
        _shellObject.transform.localPosition = new Vector3(0f, 0f, -0.01f); // чуть перед оригиналом
        _shellObject.transform.localRotation = Quaternion.identity;
        _shellObject.transform.localScale    = Vector3.one;

        // SpriteRenderer с тем же спрайтом
        _shellSprite              = _shellObject.AddComponent<SpriteRenderer>();
        _shellSprite.sprite       = _sourceSprite.sprite;
        _shellSprite.material     = _overlayMat;
        _shellSprite.sortingLayerID = _sourceSprite.sortingLayerID;
        _shellSprite.sortingOrder = _sourceSprite.sortingOrder + 1; // поверх оригинала

        // Синхронизируем флип со источником
        _shellSprite.flipX = _sourceSprite.flipX;
        _shellSprite.flipY = _sourceSprite.flipY;

        _shellObject.SetActive(false);

        Debug.Log($"[Distortable] '{name}' — спрайт-оверлей создан успешно");
    }

    public void Hit()
    {
        _hitThisFrame = true;
    }

    void Update()
    {
        // Синхронизируем спрайт если он мог смениться (анимация)
        if (_shellSprite != null && _sourceSprite != null)
        {
            if (_shellSprite.sprite != _sourceSprite.sprite)
                _shellSprite.sprite = _sourceSprite.sprite;

            // Синхронизируем флип (если враг разворачивается)
            _shellSprite.flipX = _sourceSprite.flipX;
            _shellSprite.flipY = _sourceSprite.flipY;
        }

        float target  = _hitThisFrame ? 1f : 0f;
        float speed   = (_current < target) ? 999f : fadeSpeed;
        _current      = Mathf.MoveTowards(_current, target, Time.deltaTime * speed);
        _hitThisFrame = false;

        bool shouldShow = _current > 0.001f;
        if (_shellObject.activeSelf != shouldShow)
            _shellObject.SetActive(shouldShow);

        if (shouldShow && _overlayMat != null)
            _overlayMat.SetFloat(DistortID, _current);
    }

    void OnDestroy()
    {
        if (_shellObject != null) Destroy(_shellObject);
        if (_overlayMat  != null) Destroy(_overlayMat);
    }
}