using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class BombFuseController : MonoBehaviour
{
    private static readonly string[] LegacyDigitNames = { "1", "2", "3" };
    private const string BombStateName = "Bomb";

    [SerializeField] private float fuseDurationSeconds = 15f;
    [SerializeField] private float explosionAtSeconds = 14f;
    [SerializeField] private float fuseAnimEndTime = 2.8666666f;
    [SerializeField] private float explosionAnimStartTime = 2.8666666f;
    [SerializeField] private TextMeshPro countdownText;
    [SerializeField] private string hostBlockName = "Block_Square2 (3)";

    private float remainingTime;
    private int lastDisplayedSecond = -1;
    private bool explosionTriggered;
    private bool dismissed;
    private Animator animator;
    private Transform bombRoot;
    private float clipLength = 10f;
    private BlockMover hostBlock;
    private Vector3 hostOffset;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        bombRoot = FindBombRoot();
        CacheClipLength();
        SetupCountdownText();
        HideLegacyCountdownObjects();
        ResetFuseAnimation();
    }

    void Start()
    {
        remainingTime = fuseDurationSeconds;
        UpdateCountdownDisplay(Mathf.CeilToInt(remainingTime));
        CacheHostBlock();
    }

    void Update()
    {
        if (dismissed || remainingTime <= 0f || explosionTriggered)
        {
            return;
        }

        if (hostBlock != null && hostBlock.HasExited)
        {
            DismissSilently();
            return;
        }

        remainingTime -= Time.deltaTime;
        float elapsed = fuseDurationSeconds - remainingTime;

        UpdateFuseAnimation(elapsed);

        if (elapsed >= explosionAtSeconds)
        {
            TriggerExplosion();
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
        if (dismissed || explosionTriggered)
        {
            return;
        }

        if (hostBlock != null)
        {
            transform.position = hostBlock.transform.position + hostOffset;
        }

        HideLegacyCountdownObjects();
    }

    private void CacheClipLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name == BombStateName)
            {
                clipLength = clips[i].length;
                return;
            }
        }
    }

    private void ResetFuseAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.Play(BombStateName, 0, 0f);
        animator.speed = 0f;
        animator.Update(0f);
    }

    private void UpdateFuseAnimation(float elapsed)
    {
        if (animator == null || clipLength <= 0f)
        {
            return;
        }

        float fuseProgress = Mathf.Clamp01(elapsed / explosionAtSeconds);
        float animTime = fuseProgress * fuseAnimEndTime;
        float normalizedTime = animTime / clipLength;

        animator.Play(BombStateName, 0, normalizedTime);
        animator.speed = 0f;
        animator.Update(0f);
    }

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
            HideCountdownDisplay();
            return;
        }

        countdownText.text = secondsLeft.ToString();
        countdownText.gameObject.SetActive(true);
    }

    private void HideCountdownDisplay()
    {
        if (bombRoot != null)
        {
            Transform countdownTransform = bombRoot.Find("Countdown");
            if (countdownTransform != null)
            {
                countdownTransform.gameObject.SetActive(false);
            }
        }

        if (countdownText == null)
        {
            return;
        }

        countdownText.text = string.Empty;
        countdownText.gameObject.SetActive(false);
    }

    private void CacheHostBlock()
    {
        if (string.IsNullOrWhiteSpace(hostBlockName))
        {
            return;
        }

        GameObject hostObject = SceneObjectRegistry.FindGameObjectByName(hostBlockName);
        if (hostObject == null || !hostObject.TryGetComponent(out hostBlock))
        {
            return;
        }

        hostOffset = transform.position - hostBlock.transform.position;
    }

    private void DismissSilently()
    {
        if (dismissed || explosionTriggered)
        {
            return;
        }

        dismissed = true;
        HideCountdownDisplay();
        HideLegacyCountdownObjects();

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (bombRoot != null)
        {
            bombRoot.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void TriggerExplosion()
    {
        if (explosionTriggered || dismissed)
        {
            return;
        }

        explosionTriggered = true;
        HideCountdownDisplay();
        PlayExplosionAnimation();
        PlayExplosionEffects();

        LevelCompleteFlow.RequestLevelComplete();
    }

    private void PlayExplosionAnimation()
    {
        if (animator == null || clipLength <= 0f)
        {
            return;
        }

        float normalizedStart = explosionAnimStartTime / clipLength;
        animator.Play(BombStateName, 0, normalizedStart);

        float remainingAnimTime = clipLength - explosionAnimStartTime;
        float remainingRealTime = Mathf.Max(0.01f, fuseDurationSeconds - explosionAtSeconds);
        animator.speed = remainingAnimTime / remainingRealTime;
    }

    private void PlayExplosionEffects()
    {
        if (bombRoot == null)
        {
            return;
        }

        Transform fx = bombRoot.Find("FX");
        if (fx == null)
        {
            return;
        }

        Transform candle = fx.Find("Candle");
        if (candle != null)
        {
            candle.gameObject.SetActive(false);
        }

        Transform explosion = fx.Find("Explosion");
        if (explosion == null)
        {
            return;
        }

        explosion.gameObject.SetActive(true);
        ParticleSystem[] particleSystems = explosion.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play(true);
        }
    }
}
