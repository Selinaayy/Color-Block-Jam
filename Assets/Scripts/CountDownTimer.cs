using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class CountDownTimer : MonoBehaviour
{
    public static CountDownTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [FormerlySerializedAs("toplamSureSaniye")]
    [SerializeField] private float totalDurationSeconds = 179f;
    private float remainingTime;
    private bool isRunning = false;

    [Header("UI Element")]
    [SerializeField] private TextMeshProUGUI timerText;

    public float RemainingTime => remainingTime;
    public float TotalDuration => totalDurationSeconds;
    public bool IsRunning => isRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Start()
    {
        remainingTime = totalDurationSeconds;
        isRunning = true;
    }

    void Update()
    {
        if (isRunning)
        {
            if (remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                UpdateTimerDisplay(remainingTime);
            }
            else
            {
                Debug.Log("Time is up.");
                remainingTime = 0;
                isRunning = false;
                UpdateTimerDisplay(remainingTime);
                OnTimeExpired();
            }
        }
    }

    void UpdateTimerDisplay(float time)
    {
        if (timerText == null)
        {
            return;
        }

        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);

        timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }

    void OnTimeExpired()
    {
    }
}
