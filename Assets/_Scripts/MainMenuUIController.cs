using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuUIController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string startSceneName = "Game";

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SfxVolume";

    [Header("UI")]
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField] private float referenceHeight = 900f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.6f;
    [SerializeField] private float navRepeatDelay = 0.18f;
    [SerializeField] private float navAxisThreshold = 0.5f;
    [SerializeField] private string selectedButtonClass = "menu-btn-selected";

    private UIDocument uiDocument;
    private Label pressAnyLabel;
    private VisualElement menuButtons;
    private VisualElement menuHints;
    private VisualElement settingsPanel;
    private VisualElement leaderboardPanel;
    private Button startButton;
    private Button settingsButton;
    private Button leaderboardButton;
    private Button exitButton;
    private Button settingsBackButton;
    private Button leaderboardBackButton;
    private Slider musicSlider;
    private Slider sfxSlider;

    private bool menuUnlocked;
    private Vector2Int lastScreenSize;
    private List<Button> mainMenuButtons;
    private List<Button> settingsButtons;
    private List<Button> leaderboardButtons;
    private List<Button> activeButtons;
    private int selectedIndex = -1;
    private float lastNavTime;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("MainMenuUIController: UIDocument missing.");
            enabled = false;
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        pressAnyLabel = root.Q<Label>("pressAnyLabel");
        menuButtons = root.Q<VisualElement>("menuButtons");
        menuHints = root.Q<VisualElement>("menuHints");
        settingsPanel = root.Q<VisualElement>("settingsPanel");
        leaderboardPanel = root.Q<VisualElement>("leaderboardPanel");

        startButton = root.Q<Button>("startButton");
        settingsButton = root.Q<Button>("settingsButton");
        leaderboardButton = root.Q<Button>("leaderboardButton");
        exitButton = root.Q<Button>("exitButton");
        settingsBackButton = root.Q<Button>("settingsBackButton");
        leaderboardBackButton = root.Q<Button>("leaderboardBackButton");
        musicSlider = root.Q<Slider>("musicSlider");
        sfxSlider = root.Q<Slider>("sfxSlider");

        mainMenuButtons = BuildButtonsList(startButton, settingsButton, leaderboardButton, exitButton);
        settingsButtons = BuildButtonsList(settingsBackButton);
        leaderboardButtons = BuildButtonsList(leaderboardBackButton);
    }

    private void OnEnable()
    {
        ApplyPanelScale();
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (startButton != null)
        {
            startButton.clicked += StartGame;
        }

        if (settingsButton != null)
        {
            settingsButton.clicked += OpenSettings;
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.clicked += OpenLeaderboard;
        }

        if (exitButton != null)
        {
            exitButton.clicked += ShowPressAny;
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.clicked += ShowMainMenu;
        }

        if (leaderboardBackButton != null)
        {
            leaderboardBackButton.clicked += ShowMainMenu;
        }

        if (musicSlider != null)
        {
            musicSlider.RegisterValueChangedCallback(evt => ApplyMixerVolume(musicVolumeParam, evt.newValue));
            ApplyMixerVolume(musicVolumeParam, musicSlider.value);
        }

        if (sfxSlider != null)
        {
            sfxSlider.RegisterValueChangedCallback(evt => ApplyMixerVolume(sfxVolumeParam, evt.newValue));
            ApplyMixerVolume(sfxVolumeParam, sfxSlider.value);
        }

        ShowPressAny();
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.clicked -= StartGame;
        }

        if (settingsButton != null)
        {
            settingsButton.clicked -= OpenSettings;
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.clicked -= OpenLeaderboard;
        }

        if (exitButton != null)
        {
            exitButton.clicked -= ShowPressAny;
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.clicked -= ShowMainMenu;
        }

        if (leaderboardBackButton != null)
        {
            leaderboardBackButton.clicked -= ShowMainMenu;
        }
    }

    private void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            ApplyPanelScale();
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        if (!menuUnlocked && Input.anyKeyDown)
        {
            ShowMainMenu();
        }

        if (menuUnlocked)
        {
            HandleNavigation();
        }

        if (pressAnyLabel != null && !menuUnlocked)
        {
            float t = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
            pressAnyLabel.style.opacity = Mathf.Lerp(0.2f, 1f, t);
        }
    }

    private void ShowPressAny()
    {
        menuUnlocked = false;
        SetHidden(menuButtons, true);
        SetHidden(menuHints, true);
        SetHidden(settingsPanel, true);
        SetHidden(leaderboardPanel, true);
        SetActiveButtons(null);

        if (pressAnyLabel != null)
        {
            pressAnyLabel.style.display = DisplayStyle.Flex;
        }
    }

    private void ShowMainMenu()
    {
        menuUnlocked = true;
        SetHidden(menuButtons, false);
        SetHidden(menuHints, false);
        SetHidden(settingsPanel, true);
        SetHidden(leaderboardPanel, true);
        SetActiveButtons(mainMenuButtons);

        if (pressAnyLabel != null)
        {
            pressAnyLabel.style.display = DisplayStyle.None;
        }
    }

    private void OpenSettings()
    {
        SetHidden(menuButtons, true);
        SetHidden(menuHints, true);
        SetHidden(settingsPanel, false);
        SetHidden(leaderboardPanel, true);
        SetActiveButtons(settingsButtons);
    }

    private void OpenLeaderboard()
    {
        SetHidden(menuButtons, true);
        SetHidden(menuHints, true);
        SetHidden(settingsPanel, true);
        SetHidden(leaderboardPanel, false);
        SetActiveButtons(leaderboardButtons);
    }

    private void StartGame()
    {
        if (string.IsNullOrWhiteSpace(startSceneName))
        {
            Debug.LogWarning("MainMenuUIController: startSceneName is empty.");
            return;
        }

        SceneManager.LoadScene(startSceneName);
    }

    private void SetHidden(VisualElement element, bool hidden)
    {
        if (element == null)
        {
            return;
        }

        if (hidden)
        {
            element.AddToClassList("is-hidden");
        }
        else
        {
            element.RemoveFromClassList("is-hidden");
        }
    }

    private void ApplyMixerVolume(string parameter, float linearValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(parameter))
        {
            return;
        }

        float clamped = Mathf.Clamp(linearValue, 0.0001f, 1f);
        float db = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(parameter, db);
    }

    private void ApplyPanelScale()
    {
        if (uiDocument == null || uiDocument.panelSettings == null)
        {
            return;
        }

        float scale = Mathf.Clamp(Screen.height / referenceHeight, minScale, maxScale);
        uiDocument.panelSettings.scale = scale;
    }

    private void HandleNavigation()
    {
        if (activeButtons == null || activeButtons.Count == 0)
        {
            return;
        }

        float now = Time.unscaledTime;
        float axis = Input.GetAxisRaw("Vertical");
        bool moveUp = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || axis > navAxisThreshold;
        bool moveDown = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || axis < -navAxisThreshold;
        bool canRepeat = now - lastNavTime >= navRepeatDelay;

        if (canRepeat && (moveUp || moveDown))
        {
            lastNavTime = now;
            int direction = moveUp ? -1 : 1;
            MoveSelection(direction);
        }

        bool submit = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
        if (submit)
        {
            ActivateSelection();
        }

        bool cancel = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
        if (cancel)
        {
            if (IsVisible(settingsPanel) || IsVisible(leaderboardPanel))
            {
                ShowMainMenu();
            }
            else if (IsVisible(menuButtons))
            {
                ShowPressAny();
            }
        }
    }

    private void MoveSelection(int direction)
    {
        if (activeButtons == null || activeButtons.Count == 0)
        {
            return;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = (selectedIndex + direction + activeButtons.Count) % activeButtons.Count;
        }

        ApplySelection();
    }

    private void ActivateSelection()
    {
        if (selectedIndex < 0 || activeButtons == null || selectedIndex >= activeButtons.Count)
        {
            return;
        }

        Button selected = activeButtons[selectedIndex];
        if (selected == startButton)
        {
            StartGame();
        }
        else if (selected == settingsButton)
        {
            OpenSettings();
        }
        else if (selected == leaderboardButton)
        {
            OpenLeaderboard();
        }
        else if (selected == exitButton)
        {
            ShowPressAny();
        }
        else if (selected == settingsBackButton || selected == leaderboardBackButton)
        {
            ShowMainMenu();
        }
    }

    private void SetActiveButtons(List<Button> buttons)
    {
        ClearSelection();
        activeButtons = buttons;
        selectedIndex = -1;

        if (activeButtons != null && activeButtons.Count > 0)
        {
            selectedIndex = 0;
            ApplySelection();
        }
    }

    private void ApplySelection()
    {
        if (activeButtons == null)
        {
            return;
        }

        for (int i = 0; i < activeButtons.Count; i++)
        {
            Button button = activeButtons[i];
            if (button == null)
            {
                continue;
            }

            if (i == selectedIndex)
            {
                button.AddToClassList(selectedButtonClass);
                button.Focus();
            }
            else
            {
                button.RemoveFromClassList(selectedButtonClass);
            }
        }
    }

    private void ClearSelection()
    {
        if (activeButtons == null)
        {
            return;
        }

        foreach (Button button in activeButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.RemoveFromClassList(selectedButtonClass);
        }
    }

    private List<Button> BuildButtonsList(params Button[] buttons)
    {
        List<Button> result = new List<Button>();
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                result.Add(button);
            }
        }

        return result;
    }

    private bool IsVisible(VisualElement element)
    {
        return element != null && !element.ClassListContains("is-hidden");
    }
}
