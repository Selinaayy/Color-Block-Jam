using UnityEngine;

public static class RuntimeServices
{
    public static void EnsureSettingsManager()
    {
        if (SettingsManager.Instance != null)
        {
            return;
        }

        new GameObject("SettingsManager").AddComponent<SettingsManager>();
    }

    public static LevelManager EnsureLevelManager()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance;
        }

        return new GameObject("LevelManager").AddComponent<LevelManager>();
    }

    public static RestartManager EnsureRestartManager()
    {
        if (RestartManager.Instance != null)
        {
            return RestartManager.Instance;
        }

        return new GameObject("RestartManager").AddComponent<RestartManager>();
    }

    public static void EnsureLevelCompleteManagers(out LevelManager levelManager, out RestartManager restartManager)
    {
        levelManager = EnsureLevelManager();
        restartManager = EnsureRestartManager();
        EnsureSettingsManager();
    }
}
