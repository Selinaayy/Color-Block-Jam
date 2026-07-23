using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject levelCompletePanel;

    private int remainingBlockCount;
    private bool levelRewardGiven;
    private bool levelCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;

        RuntimeServices.EnsureSettingsManager();
    }

    void Start()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);

            if (!levelCompletePanel.TryGetComponent(out NextLevelPanelController panelController))
            {
                panelController = levelCompletePanel.AddComponent<NextLevelPanelController>();
            }
        }

        remainingBlockCount = 0;

        foreach (BlockMover block in BlockRegistry.All)
        {
            if (block.color == BlockMover.BlockColor.Red ||
                block.color == BlockMover.BlockColor.Blue)
            {
                remainingBlockCount++;
            }
        }
    }

    public void OnBlockExited()
    {
        remainingBlockCount--;

        if (remainingBlockCount <= 0)
        {
            CompleteLevel();
        }
    }

    public void CompleteLevel()
    {
        if (levelCompleted)
        {
            return;
        }

        levelCompleted = true;
        Time.timeScale = 0f;

        if (CountDownTimer.Instance != null)
        {
            CountDownTimer.Instance.StopTimer();
        }

        if (!levelRewardGiven && SettingsManager.Instance != null)
        {
            SettingsManager.Instance.AddGold(SettingsManager.Instance.levelCompleteGoldReward);
            levelRewardGiven = true;
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            Debug.Log("Level complete panel opened.");
        }
    }
}
