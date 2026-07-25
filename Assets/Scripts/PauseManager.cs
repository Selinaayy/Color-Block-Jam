using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;

    public GameObject pauseMenuPanel;

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }
        else
        {
            Time.timeScale = 1f;

            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        }
    }
}
