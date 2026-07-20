using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string GoldKey = "PlayerGold";
    private const string SoundKey = "SoundEnabled";
    private const string VibrationKey = "VibrationEnabled";

    public int levelCompleteGoldReward = 100;

    private int gold;
    private bool soundEnabled = true;
    private bool vibrationEnabled = true;
    private bool goldDisplaysCached;
    private readonly List<TMP_Text> goldDisplays = new List<TMP_Text>();

    public bool SoundEnabled => soundEnabled;
    public bool VibrationEnabled => vibrationEnabled;
    public int Gold => gold;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        gold = PlayerPrefs.GetInt(GoldKey, 0);
        soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        vibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;

        ApplySound();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        PlayerPrefs.SetInt(GoldKey, gold);
        PlayerPrefs.Save();
        RefreshGoldDisplays();
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        PlayerPrefs.SetInt(SoundKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySound();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        vibrationEnabled = enabled;
        PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleSound()
    {
        SetSoundEnabled(!soundEnabled);
    }

    public void ToggleVibration()
    {
        SetVibrationEnabled(!vibrationEnabled);
    }

    public void Vibrate()
    {
        if (!vibrationEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    public void RefreshGoldDisplays()
    {
        CacheGoldDisplaysIfNeeded();

        foreach (TMP_Text text in goldDisplays)
        {
            if (text != null)
            {
                text.text = "     " + gold;
            }
        }
    }

    private void CacheGoldDisplaysIfNeeded()
    {
        if (goldDisplaysCached)
        {
            return;
        }

        goldDisplays.Clear();
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            CollectGoldDisplays(roots[i].transform);
        }

        goldDisplaysCached = true;
    }

    private void CollectGoldDisplays(Transform current)
    {
        if (current.name == "Text (TMP)" &&
            current.parent != null &&
            current.parent.name == "gold" &&
            current.TryGetComponent(out TMP_Text text))
        {
            goldDisplays.Add(text);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectGoldDisplays(current.GetChild(i));
        }
    }

    private void ApplySound()
    {
        AudioListener.volume = soundEnabled ? 1f : 0f;
    }
}
