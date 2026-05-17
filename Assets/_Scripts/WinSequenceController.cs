using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class WinSequenceController : MonoBehaviour
{
    public static WinSequenceController Instance { get; private set; }

    [Header("Keys")]
    public int keysRequired = 3;
    [SerializeField] private int currentKeys = 0;

    [Header("UI")]
    public TMP_Text winText;
    public CanvasGroup darkPanel;
    public float textDuration = 3f;
    public float panelFadeDuration = 1f;
    [Tooltip("Scene name to load after fade (set in inspector)")]
    public string targetSceneName;

    private Tween blinkTween;
    private bool sequenceStarted = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (winText)
        {
            winText.gameObject.SetActive(false);
            Color c = winText.color;
            c.a = 0f;
            winText.color = c;
        }

        if (darkPanel)
        {
            darkPanel.alpha = 0f;
            darkPanel.blocksRaycasts = true;
            darkPanel.interactable = true;
        }
    }

    // Call this when a key is picked up
    public void AddKey()
    {
        if (sequenceStarted) return;
        currentKeys++;
        if (currentKeys >= keysRequired)
        {
            StartCoroutine(RunWinSequence());
        }
    }

    private IEnumerator RunWinSequence()
    {
        sequenceStarted = true;

        if (winText)
        {
            winText.gameObject.SetActive(true);
            winText.text = "Поздравляю! Вы прошли игру!!";
            Color c = winText.color;
            c.a = 1f;
            winText.color = c;
            blinkTween = winText.DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
        }

        yield return new WaitForSeconds(textDuration);

        if (blinkTween != null)
        {
            blinkTween.Kill();
            blinkTween = null;
        }

        if (winText)
        {
            Color c2 = winText.color;
            c2.a = 1f;
            winText.color = c2;
        }

        if (darkPanel)
        {
            darkPanel.DOFade(1f, panelFadeDuration).OnComplete(() =>
            {
                if (!string.IsNullOrEmpty(targetSceneName))
                    SceneManager.LoadScene(targetSceneName);
            });
        }
        else
        {
            if (!string.IsNullOrEmpty(targetSceneName))
                SceneManager.LoadScene(targetSceneName);
        }
    }
}
