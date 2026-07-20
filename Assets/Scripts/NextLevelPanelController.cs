using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NextLevelPanelController : MonoBehaviour
{
    [Header("Buttons (resolved by name if empty)")]
    public Button nextLevelButton;
    public Button closeButton;
    public Button restartButton;
    public Button soundOnButton;
    public Button soundOffButton;
    public Button vibrationButton;

    [Header("Sound/Vibration visuals")]
    public GameObject soundOnObject;
    public GameObject soundOffObject;

    [Header("Texts (resolved by name if empty)")]
    public TMP_Text levelTitleText;
    public TMP_Text nextLevelButtonText;
    public TMP_Text rewardText;
    public TMP_Text statsText;

    [Header("Animation")]
    public float entranceDuration = 0.45f;

    private LevelManager levelManager;
    private RestartManager restartManager;
    private CanvasGroup canvasGroup;
    private RectTransform panelRect;
    private Image dimOverlay;
    private RectTransform rewardCard;
    private RectTransform statsCard;
    private bool enhancedUiBuilt;
    private Coroutine showRoutine;
    private Coroutine pulseRoutine;

    void Awake()
    {
        levelManager = LevelManager.Instance;
        if (levelManager == null)
        {
            GameObject managerObject = new GameObject("LevelManager");
            levelManager = managerObject.AddComponent<LevelManager>();
        }

        restartManager = RestartManager.Instance;
        if (restartManager == null)
        {
            GameObject restartObject = new GameObject("RestartManager");
            restartManager = restartObject.AddComponent<RestartManager>();
        }

        if (SettingsManager.Instance == null)
        {
            GameObject settingsObject = new GameObject("SettingsManager");
            settingsObject.AddComponent<SettingsManager>();
        }

        TryGetComponent(out panelRect);
        if (!TryGetComponent(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        BuildEnhancedUI();
        ResolveButtonsIfNeeded();
        ResolveTextsIfNeeded();
        ApplyPanelFontToDynamicTexts();
        BindButtons();
    }

    void Start()
    {
        UpdateSoundButtons();
    }

    void OnEnable()
    {
        if (!enhancedUiBuilt)
        {
            BuildEnhancedUI();
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.RefreshGoldDisplays();
            UpdateSoundButtons();
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(PlayShowAnimation());
    }

    void OnDisable()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private void BuildEnhancedUI()
    {
        if (enhancedUiBuilt) return;
        enhancedUiBuilt = true;

        dimOverlay = CreateStretchImage("DimOverlay", new Color(0f, 0f, 0f, 0.62f));
        dimOverlay.transform.SetAsFirstSibling();
        dimOverlay.raycastTarget = true;

        Transform existingStars = transform.Find("StarsContainer");
        if (existingStars != null)
        {
            Destroy(existingStars.gameObject);
        }

        rewardCard = CreateAnchoredPanel("RewardCard", new Vector2(0.5f, 0.5f), new Vector2(520f, 110f), new Vector2(0f, -470f));
        Image rewardBackground = rewardCard.gameObject.AddComponent<Image>();
        rewardBackground.color = CardGreen;
        rewardBackground.raycastTarget = false;

        Outline rewardOutline = rewardCard.gameObject.AddComponent<Outline>();
        rewardOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        rewardOutline.effectDistance = new Vector2(3f, -3f);

        GameObject rewardLabelObject = new GameObject("RewardLabel", typeof(RectTransform));
        rewardLabelObject.transform.SetParent(rewardCard, false);
        RectTransform rewardLabelRect = (RectTransform)rewardLabelObject.transform;
        StretchFull(rewardLabelRect);

        rewardText = rewardLabelObject.AddComponent<TextMeshProUGUI>();
        rewardText.text = "+100 GOLD";
        rewardText.fontSize = 42f;
        rewardText.fontStyle = FontStyles.Bold;
        rewardText.alignment = TextAlignmentOptions.Center;
        rewardText.color = Color.white;
        rewardText.raycastTarget = false;

        statsCard = CreateAnchoredPanel("StatsCard", new Vector2(0.5f, 0.5f), new Vector2(460f, 70f), new Vector2(0f, 330f));
        Image statsBackground = statsCard.gameObject.AddComponent<Image>();
        statsBackground.color = CardBlue;
        statsBackground.raycastTarget = false;

        GameObject statsLabelObject = new GameObject("StatsLabel", typeof(RectTransform));
        statsLabelObject.transform.SetParent(statsCard, false);
        RectTransform statsLabelRect = (RectTransform)statsLabelObject.transform;
        StretchFull(statsLabelRect);

        statsText = statsLabelObject.AddComponent<TextMeshProUGUI>();
        statsText.text = "Süre: 02:59";
        statsText.fontSize = 30f;
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.color = new Color(0.85f, 0.95f, 1f);
        statsText.raycastTarget = false;

        if (restartButton == null)
        {
            restartButton = CreateActionButton(
                "RestartButton",
                "TEKRAR OYNA",
                new Vector2(0.5f, 0.5f),
                new Vector2(360f, 90f),
                new Vector2(-210f, -120f),
                new Color(0.75f, 0.28f, 0.28f, 1f));
        }
    }

    private void ResolveButtonsIfNeeded()
    {
        if (nextLevelButton == null)
        {
            Transform buttonTransform = transform.Find("Button");
            if (buttonTransform != null && buttonTransform.TryGetComponent(out Button button))
            {
                nextLevelButton = button;
            }
        }

        if (closeButton == null)
        {
            Transform closeTransform = transform.Find("CloseButton");
            if (closeTransform != null && closeTransform.TryGetComponent(out Button closeButtonComponent))
            {
                closeButton = closeButtonComponent;
            }
        }

        if (soundOnButton == null)
        {
            Transform soundOnTransform = transform.Find("soundOn");
            if (soundOnTransform != null && soundOnTransform.TryGetComponent(out Button soundOnButtonComponent))
            {
                soundOnButton = soundOnButtonComponent;
                soundOnObject = soundOnTransform.gameObject;
            }
        }

        if (soundOffButton == null)
        {
            Transform soundOffTransform = transform.Find("soundOff");
            if (soundOffTransform != null && soundOffTransform.TryGetComponent(out Button soundOffButtonComponent))
            {
                soundOffButton = soundOffButtonComponent;
                soundOffObject = soundOffTransform.gameObject;
            }
        }

        if (vibrationButton == null)
        {
            Transform vibrationTransform = transform.Find("vibration");
            if (vibrationTransform != null && vibrationTransform.TryGetComponent(out Button vibrationButtonComponent))
            {
                vibrationButton = vibrationButtonComponent;
            }
        }
    }

    private void ApplyPanelFontToDynamicTexts()
    {
        if (levelTitleText == null || levelTitleText.font == null) return;

        if (rewardText != null) rewardText.font = levelTitleText.font;
        if (statsText != null) statsText.font = levelTitleText.font;
    }

    private void ResolveTextsIfNeeded()
    {
        if (levelTitleText == null)
        {
            Transform levelInfoTransform = transform.Find("levelbilgi");
            if (levelInfoTransform != null && levelInfoTransform.TryGetComponent(out TMP_Text titleText))
            {
                levelTitleText = titleText;
            }
        }

        if (nextLevelButtonText == null && nextLevelButton != null)
        {
            nextLevelButtonText = FindTextInChildren(nextLevelButton.transform);
        }

        Transform completeTextTransform = transform.Find("Text (TMP)");
        if (completeTextTransform != null && completeTextTransform.TryGetComponent(out TMP_Text completeText))
        {
            completeText.text = "LEVEL COMPLETE!";
            completeText.fontStyle = FontStyles.Bold;
        }
    }

    private static TMP_Text FindTextInChildren(Transform root)
    {
        if (root.TryGetComponent(out TMP_Text text))
        {
            return text;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            TMP_Text childText = FindTextInChildren(root.GetChild(i));
            if (childText != null)
            {
                return childText;
            }
        }

        return null;
    }

    private void BindButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(levelManager.NextLevel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(restartManager.RestartGame);
        }

        if (soundOnButton != null)
        {
            soundOnButton.onClick.RemoveAllListeners();
            soundOnButton.onClick.AddListener(() =>
            {
                SettingsManager.Instance.SetSoundEnabled(false);
                UpdateSoundButtons();
            });
        }

        if (soundOffButton != null)
        {
            soundOffButton.onClick.RemoveAllListeners();
            soundOffButton.onClick.AddListener(() =>
            {
                SettingsManager.Instance.SetSoundEnabled(true);
                UpdateSoundButtons();
            });
        }

        if (vibrationButton != null)
        {
            vibrationButton.onClick.RemoveAllListeners();
            vibrationButton.onClick.AddListener(() =>
            {
                SettingsManager.Instance.ToggleVibration();
                SettingsManager.Instance.Vibrate();
            });
        }
    }

    private IEnumerator PlayShowAnimation()
    {
        UpdateDynamicContent();

        canvasGroup.alpha = 0f;
        panelRect.localScale = Vector3.one * 0.82f;

        if (rewardCard != null) rewardCard.localScale = Vector3.zero;
        if (statsCard != null) statsCard.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = EaseOutBack(t);

            canvasGroup.alpha = t;
            panelRect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panelRect.localScale = Vector3.one;

        yield return AnimateScaleIn(statsCard, 0.25f);
        yield return AnimateScaleIn(rewardCard, 0.22f);
        yield return AnimateRewardCount();

        if (nextLevelButton != null)
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseButton(nextLevelButton.transform));
        }

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.Vibrate();
        }
    }

    private IEnumerator AnimateRewardCount()
    {
        if (rewardText == null || SettingsManager.Instance == null) yield break;

        int reward = SettingsManager.Instance.levelCompleteGoldReward;
        int displayed = 0;
        float elapsed = 0f;
        const float duration = 0.55f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            displayed = Mathf.RoundToInt(Mathf.Lerp(0f, reward, t));
            rewardText.text = "+" + displayed + " GOLD";
            yield return null;
        }

        rewardText.text = "+" + reward + " GOLD";
    }

    private IEnumerator AnimateScaleIn(RectTransform target, float duration)
    {
        if (target == null) yield break;

        target.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.one * EaseOutBack(t);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private IEnumerator PulseButton(Transform buttonTransform)
    {
        Vector3 baseScale = Vector3.one;
        while (isActiveAndEnabled)
        {
            float elapsed = 0f;
            const float duration = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = 1f + Mathf.Sin(elapsed / duration * Mathf.PI * 2f) * 0.04f;
                buttonTransform.localScale = baseScale * wave;
                yield return null;
            }
        }
    }

    private void UpdateDynamicContent()
    {
        int currentLevel = SceneManager.GetActiveScene().buildIndex + 1;
        int nextLevel = currentLevel + 1;
        bool hasNextLevel = SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;

        if (levelTitleText != null)
        {
            levelTitleText.text = "LEVEL " + currentLevel;
        }

        if (nextLevelButtonText != null)
        {
            nextLevelButtonText.text = hasNextLevel ? "NEXT LEVEL " + nextLevel : "TAMAMLANDI";
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = hasNextLevel;
        }

        if (statsText != null)
        {
            if (CountDownTimer.Instance != null)
            {
                float remaining = Mathf.Max(0f, CountDownTimer.Instance.RemainingTime);
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                statsText.text = "Kalan Süre: " + minutes.ToString("0") + ":" + seconds.ToString("00");
            }
            else
            {
                statsText.text = "Harika iş çıkardın!";
            }
        }

        if (rewardText != null && SettingsManager.Instance != null)
        {
            rewardText.text = "+0 GOLD";
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void UpdateSoundButtons()
    {
        if (SettingsManager.Instance == null) return;

        bool soundOn = SettingsManager.Instance.SoundEnabled;

        if (soundOnObject != null) soundOnObject.SetActive(soundOn);
        if (soundOffObject != null) soundOffObject.SetActive(!soundOn);
    }

    private Image CreateStretchImage(string objectName, Color color)
    {
        GameObject overlayObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        overlayObject.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)overlayObject.transform;
        StretchFull(rect);

        Image image = overlayObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private RectTransform CreateAnchoredPanel(string objectName, Vector2 anchor, Vector2 size, Vector2 position)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
        panelObject.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)panelObject.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private Button CreateActionButton(string objectName, string label, Vector2 anchor, Vector2 size, Vector2 position, Color color)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        StretchFull(textRect);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 28f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        return button;
    }

    private static readonly Color CardBlue = new Color(0.06f, 0.23f, 0.43f, 0.95f);
    private static readonly Color CardGreen = new Color(0.12f, 0.55f, 0.35f, 0.95f);

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
