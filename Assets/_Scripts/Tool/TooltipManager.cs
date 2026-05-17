using UnityEngine;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject tooltipCanvas;
    [SerializeField] private Image tooltipImage;

    private void Awake()
    {
        // Настройка синглтона
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Прячем подсказку на старте
        HideTooltip();
    }

    /// <summary>
    /// Показать подсказку с возможностью динамически менять спрайт
    /// </summary>
    public void ShowTooltip(Sprite customSprite = null)
    {
        if (customSprite != null && tooltipImage != null)
        {
            tooltipImage.sprite = customSprite;
        }

        tooltipCanvas.SetActive(true);
    }

    /// <summary>
    /// Скрыть подсказку
    /// </summary>
    public void HideTooltip()
    {
        tooltipCanvas.SetActive(false);
    }
}