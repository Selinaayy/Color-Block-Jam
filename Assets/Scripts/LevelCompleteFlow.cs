using UnityEngine;

public static class LevelCompleteFlow
{
    public static void RequestLevelComplete()
    {
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

        NextLevelPanelController panel = Object.FindObjectOfType<NextLevelPanelController>(true);
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
        }
    }
}
