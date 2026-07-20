using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class BombFuseController : MonoBehaviour
{
    private static readonly string[] LegacyDigitNames = { "1", "2", "3" };

    [SerializeField] private float fuseDurationSeconds = 15f;
    [SerializeField] private float explosionAtSeconds = 1f;
    [SerializeField] private float referenceExplosionTime = 3.6333334f;
    [SerializeField] private TextMeshPro countdownText;

    private float remainingTime;
    private int lastDisplayedSecond = -1;
    private bool explosionTriggered;
    private Animator animator;
    private Transform bombRoot;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        bombRoot = FindBombRoot();
        SetupCountdownText();
        HideLegacyCountdownObjects();
        ApplyAnimatorSpeed();
    }

    void Start()
    {
        remainingTime = fuseDurationSeconds;
        UpdateCountdownDisplay(Mathf.CeilToInt(remainingTime));
    }

    void Update()
    {
        if (remainingTime <= 0f)
        {
            return;
        }

        remainingTime -= Time.deltaTime;

        if (!explosionTriggered && fuseDurationSeconds - remainingTime >= explosionAtSeconds)
        {
            TriggerExplosion();
        }

        if (explosionTriggered)
        {
            return;
        }

        int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(remainingTime));

        if (secondsLeft != lastDisplayedSecond)
        {
            UpdateCountdownDisplay(secondsLeft);
        }
    }

    void LateUpdate()
    {
        HideLegacyCountdownObjects();
        if (countdownText != null && remainingTime > 0f && !explosionTriggered)
        {
            countdownText.gameObject.SetActive(true);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ApplyAnimatorSpeed();
    }
#endif

    private Transform FindBombRoot()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Bomb")
            {
                return child;
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<TextMeshPro>(true) != null)
            {
                return child;
            }
        }

        return transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    private void SetupCountdownText()
    {
        if (countdownText == null && bombRoot != null)
        {
            countdownText = FindCountdownText(bombRoot);
        }

        if (countdownText == null)
        {
            return;
        }

        countdownText.gameObject.name = "Countdown";
        countdownText.gameObject.SetActive(true);
        countdownText.enableAutoSizing = true;
        countdownText.fontSizeMax = countdownText.fontSize > 0f ? countdownText.fontSize : 36f;
        countdownText.fontSizeMin = 1.9f;
    }

    private static TextMeshPro FindCountdownText(Transform root)
    {
        TextMeshPro[] texts = root.GetComponentsInChildren<TextMeshPro>(true);
        TextMeshPro fallback = null;

        foreach (TextMeshPro text in texts)
        {
            string objectName = text.gameObject.name;
            if (objectName == "Countdown" || objectName == "3" || objectName == "Text (TMP)")
            {
                return text;
            }

            if (objectName != "1" && objectName != "2" && fallback == null)
            {
                fallback = text;
            }
        }

        return fallback;
    }

    private void HideLegacyCountdownObjects()
    {
        if (bombRoot == null)
        {
            return;
        }

        TextMeshPro[] texts = bombRoot.GetComponentsInChildren<TextMeshPro>(true);
        foreach (TextMeshPro text in texts)
        {
            if (text == countdownText)
            {
                continue;
            }

            for (int i = 0; i < LegacyDigitNames.Length; i++)
            {
                if (text.gameObject.name == LegacyDigitNames[i])
                {
                    text.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    private void UpdateCountdownDisplay(int secondsLeft)
    {
        lastDisplayedSecond = secondsLeft;

        if (countdownText == null)
        {
            return;
        }

        if (secondsLeft <= 0)
        {
            countdownText.text = string.Empty;
            countdownText.gameObject.SetActive(false);
            return;
        }

        countdownText.text = secondsLeft.ToString();
        countdownText.gameObject.SetActive(true);
    }

    private void HideCountdownDisplay()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.text = string.Empty;
        countdownText.gameObject.SetActive(false);
    }

    private void TriggerExplosion()
    {
        if (explosionTriggered)
        {
            return;
        }

        explosionTriggered = true;
        HideCountdownDisplay();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
            return;
        }

        Time.timeScale = 0f;

        if (CountDownTimer.Instance != null)
        {
            CountDownTimer.Instance.StopTimer();
        }

        NextLevelPanelController panel = FindObjectOfType<NextLevelPanelController>(true);
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
        }
    }

    private void ApplyAnimatorSpeed()
    {
        if (animator == null || explosionAtSeconds <= 0f)
        {
            return;
        }

        animator.speed = referenceExplosionTime / explosionAtSeconds;
    }
}
