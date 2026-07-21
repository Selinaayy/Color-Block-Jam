using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NextLevelPanelController : MonoBehaviour
{
    [Header("Animation")]
    public float entranceDuration = 0.45f;

    private Button nextLevelButton;
    private Button closeButton;
    private Button restartButton;
    private TMP_Text levelTitleText;
    private TMP_Text nextLevelButtonText;
    private TMP_Text rewardText;

    private LevelManager levelManager;
    private RestartManager restartManager;
    private CanvasGroup canvasGroup;
    private RectTransform popupRect;
    private RectTransform goldRow;
    private bool uiBuilt;
    private Coroutine showRoutine;
    private Coroutine pulseRoutine;

    void Awake()
    {
        EnsureManagers();

        if (!TryGetComponent(out canvasGroup))
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (TryGetComponent(out RectTransform panelRect))
        {
            StretchFull(panelRect);
        }

        if (TryGetComponent(out Image rootImage))
        {
            rootImage.sprite = null;
            rootImage.color = Color.clear;
            rootImage.raycastTarget = true;
        }

        BuildLevelCompleteUI();
        BindButtons();
    }

    void OnEnable()
    {
        if (!uiBuilt)
        {
            BuildLevelCompleteUI();
            BindButtons();
        }

        SettingsManager.Instance?.RefreshGoldDisplays();

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

    private void EnsureManagers()
    {
        if (LevelManager.Instance == null)
        {
            levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();
        }
        else
        {
            levelManager = LevelManager.Instance;
        }

        if (RestartManager.Instance == null)
        {
            restartManager = new GameObject("RestartManager").AddComponent<RestartManager>();
        }
        else
        {
            restartManager = RestartManager.Instance;
        }

        if (SettingsManager.Instance == null)
        {
            new GameObject("SettingsManager").AddComponent<SettingsManager>();
        }
    }

    private void BuildLevelCompleteUI()
    {
        Transform existingPopup = transform.Find("PopupPanel");
        Transform existingContent = transform.Find("PopupPanel/Content");
        if (uiBuilt && existingPopup != null && existingContent != null && existingPopup.TryGetComponent(out Image existingBackground) && existingBackground.sprite != null)
        {
            return;
        }

        uiBuilt = true;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Image dimOverlay = CreateStretchImage("DimOverlay", new Color(0f, 0f, 0f, 0.72f));
        dimOverlay.raycastTarget = true;
        dimOverlay.transform.SetAsFirstSibling();

        popupRect = CreateRect("PopupPanel", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1180f));
        Image popupBackground = popupRect.gameObject.AddComponent<Image>();
        ApplySprite(popupBackground, LevelCompleteUILoader.Get("Bg_popup_"), false);

        RectTransform banner = CreateRect("Banner", popupRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 48f), new Vector2(820f, 200f));
        Image bannerImage = banner.gameObject.AddComponent<Image>();
        ApplySprite(bannerImage, LevelCompleteUILoader.Get("Banner"), true);

        TMP_Text completeTitleText = CreateText("CompleteTitle", banner, "LEVEL COMPLETE!", 44f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.93f, 0.28f));
        StretchFull(completeTitleText.rectTransform);
        completeTitleText.rectTransform.offsetMin = new Vector2(40f, 12f);
        completeTitleText.rectTransform.offsetMax = new Vector2(-40f, -12f);

        closeButton = CreateCornerButton("CloseButton", popupRect, LevelCompleteUILoader.Get("Close"), new Vector2(-36f, -36f), new Vector2(92f, 92f));

        RectTransform content = CreateRect("Content", popupRect, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -220f));
        content.offsetMin = new Vector2(48f, 80f);
        content.offsetMax = new Vector2(-48f, -200f);

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 36f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(24, 24, 24, 24);

        RectTransform levelTitleRect = CreateLayoutItem("LevelTitleRow", content, new Vector2(560f, 72f));
        levelTitleText = CreateText("LevelTitle", levelTitleRect, "LEVEL 1", 50f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        StretchFull(levelTitleText.rectTransform);

        goldRow = CreateLayoutItem("GoldReward", content, new Vector2(460f, 100f));
        HorizontalLayoutGroup goldLayout = goldRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        goldLayout.childAlignment = TextAnchor.MiddleCenter;
        goldLayout.spacing = 16f;
        goldLayout.childControlWidth = false;
        goldLayout.childControlHeight = false;
        goldLayout.childForceExpandWidth = false;
        goldLayout.childForceExpandHeight = false;

        RectTransform coinRect = CreateRect("GoldCoin", goldRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(88f, 88f));
        LayoutElement coinLayout = coinRect.gameObject.AddComponent<LayoutElement>();
        coinLayout.preferredWidth = 88f;
        coinLayout.preferredHeight = 88f;
        Image coinImage = coinRect.gameObject.AddComponent<Image>();
        ApplySprite(coinImage, LevelCompleteUILoader.Get("Gold"), true);

        rewardText = CreateText("RewardAmount", goldRow, "+100", 56f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.92f, 0.35f));
        LayoutElement rewardLayout = rewardText.gameObject.AddComponent<LayoutElement>();
        rewardLayout.preferredWidth = 220f;
        rewardLayout.preferredHeight = 80f;

        RectTransform nextLevelRect = CreateLayoutItem("NextLevelButtonRow", content, new Vector2(620f, 150f));
        nextLevelButton = CreateSpriteButton("NextLevelButton", nextLevelRect, LevelCompleteUILoader.Get("LevelButton"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 150f));
        StretchFull(nextLevelButton.GetComponent<RectTransform>());
        nextLevelButtonText = CreateText("NextLevelLabel", nextLevelButton.transform, "NEXT LEVEL (2)", 38f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        StretchFull(nextLevelButtonText.rectTransform);
        nextLevelButtonText.rectTransform.offsetMin = new Vector2(24f, 8f);
        nextLevelButtonText.rectTransform.offsetMax = new Vector2(-24f, -8f);

        RectTransform replayRect = CreateLayoutItem("ReplayButtonRow", content, new Vector2(200f, 200f));
        restartButton = CreateSpriteButton("ReplayButton", replayRect, LevelCompleteUILoader.Get("SmallButtonBlue"), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
        StretchFull(restartButton.GetComponent<RectTransform>());

        Sprite replayIcon = LevelCompleteUILoader.Get("Restart");
        if (replayIcon != null)
        {
            RectTransform iconRect = CreateRect("ReplayIcon", restartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(88f, 88f));
            Image iconImage = iconRect.gameObject.AddComponent<Image>();
            ApplySprite(iconImage, replayIcon, true);
        }

        TMP_Text replayLabel = CreateText("ReplayLabel", restartButton.transform, "REPLAY", 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetAnchoredRect(replayLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 40f));

        if (LevelCompleteUILoader.Get("Bg_popup_") == null)
        {
            Debug.LogError("Level complete UI sprites missing. Select Assets/Resources/LevelCompleteUI folder in Unity, then reimport all PNG files as Sprite (2D and UI).");
        }
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
    }

    private IEnumerator PlayShowAnimation()
    {
        UpdateDynamicContent();

        canvasGroup.alpha = 0f;
        if (popupRect != null)
        {
            popupRect.localScale = Vector3.one * 0.82f;
        }

        if (goldRow != null)
        {
            goldRow.localScale = Vector3.zero;
        }

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);
            float eased = EaseOutBack(t);

            canvasGroup.alpha = t;
            if (popupRect != null)
            {
                popupRect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, eased);
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (popupRect != null)
        {
            popupRect.localScale = Vector3.one;
        }

        yield return AnimateScaleIn(goldRow, 0.25f);
        yield return AnimateRewardCount();

        if (nextLevelButton != null)
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseButton(nextLevelButton.transform));
        }

        SettingsManager.Instance?.Vibrate();
    }

    private IEnumerator AnimateRewardCount()
    {
        if (rewardText == null || SettingsManager.Instance == null)
        {
            yield break;
        }

        int reward = SettingsManager.Instance.levelCompleteGoldReward;
        int displayed = 0;
        float elapsed = 0f;
        const float duration = 0.55f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            displayed = Mathf.RoundToInt(Mathf.Lerp(0f, reward, t));
            rewardText.text = "+" + displayed;
            yield return null;
        }

        rewardText.text = "+" + reward;
    }

    private IEnumerator AnimateScaleIn(RectTransform target, float duration)
    {
        if (target == null)
        {
            yield break;
        }

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

        string nextLevelLabel = hasNextLevel ? "NEXT LEVEL (" + nextLevel + ")" : "COMPLETED";

        if (nextLevelButtonText != null)
        {
            nextLevelButtonText.text = nextLevelLabel;
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = hasNextLevel;
        }

        if (rewardText != null)
        {
            rewardText.text = "+0";
        }
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect)
    {
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = preserveAspect;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.enabled = sprite != null;
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

    private RectTransform CreateLayoutItem(string objectName, Transform parent, Vector2 size)
    {
        GameObject itemObject = new GameObject(objectName, typeof(RectTransform));
        itemObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)itemObject.transform;
        rect.sizeDelta = size;

        LayoutElement layoutElement = itemObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
        layoutElement.minHeight = size.y;

        return rect;
    }

    private RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)panelObject.transform;
        SetAnchoredRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        return rect;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private Button CreateSpriteButton(string objectName, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        SetAnchoredRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta);

        Image image = buttonObject.AddComponent<Image>();
        ApplySprite(image, sprite, false);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private Button CreateCornerButton(string objectName, Transform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.AddComponent<Image>();
        ApplySprite(image, sprite, true);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

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
