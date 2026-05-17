using UnityEngine;

public class FlashlightDistortable : MonoBehaviour
{
    [Header("Настройки оверлея")]
    public float fadeSpeed    = 2.5f;
    public Color overlayColor = new Color(0.4f, 0f, 0.8f, 1f);
    public Shader overlayShader;

    // Чуть больше чем оригинал чтобы не z-fight
    [Range(1.001f, 1.05f)]
    public float shellScale = 1.01f;

    float    _current;
    bool     _hitThisFrame;

    Material  _overlayMat;
    Renderer  _shellRenderer;
    GameObject _shellObject;

    static readonly int DistortID      = Shader.PropertyToID("_DistortAmount");
    static readonly int OverlayColorID = Shader.PropertyToID("_OverlayColor");

    void Awake()
    {
        // Найти MeshFilter и Renderer на себе или ребёнке
        MeshFilter mf = GetComponent<MeshFilter>();
        Renderer   rend = GetComponent<Renderer>();

        if (mf == null) mf   = GetComponentInChildren<MeshFilter>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        if (mf == null || rend == null)
        {
//            Debug.LogError($"[Distortable] MeshFilter или Renderer не найден на '{name}'");
            enabled = false;
            return;
        }

        // Найти шейдер
        if (overlayShader == null)
            overlayShader = Shader.Find("Custom/FlashlightOverlay");

        if (overlayShader == null)
        {
            Debug.LogError("[Distortable] Шейдер Custom/FlashlightOverlay не найден!");
            enabled = false;
            return;
        }

        // Создаём материал
        _overlayMat = new Material(overlayShader);
        _overlayMat.SetColor(OverlayColorID, overlayColor);
        _overlayMat.SetFloat(DistortID, 0f);

        // Создаём дочерний объект с той же мешью
        _shellObject = new GameObject("_FlashlightOverlayShell");
        _shellObject.transform.SetParent(rend.transform, worldPositionStays: false);
        _shellObject.transform.localPosition = Vector3.zero;
        _shellObject.transform.localRotation = Quaternion.identity;
        _shellObject.transform.localScale    = Vector3.one * shellScale;

        // Вешаем ту же меш
        var shellMF = _shellObject.AddComponent<MeshFilter>();
        shellMF.sharedMesh = mf.sharedMesh;

        // Вешаем renderer только с оверлейным материалом
        _shellRenderer = _shellObject.AddComponent<MeshRenderer>();
        _shellRenderer.material = _overlayMat;
        _shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _shellRenderer.receiveShadows    = false;

        // Начинаем невидимым
        _shellObject.SetActive(false);

        Debug.Log($"[Distortable] '{name}' — shell создан успешно");
    }

    // Вызывается из FlashlightController каждые 0.1 сек
    public void Hit()
    {
        _hitThisFrame = true;
    }

    void Update()
    {
        float target = _hitThisFrame ? 1f : 0f;
        float speed  = (_current < target) ? 999f : fadeSpeed;
        _current     = Mathf.MoveTowards(_current, target, Time.deltaTime * speed);
        _hitThisFrame = false;

        // Включаем/выключаем shell объект
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