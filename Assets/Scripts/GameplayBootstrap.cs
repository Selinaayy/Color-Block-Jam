using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        EnsureBlockDragInput();
    }

    private static void EnsureBlockDragInput()
    {
        if (BlockDragInput.Instance != null)
        {
            return;
        }

        GameObject inputObject = new GameObject("BlockDragInput");
        inputObject.AddComponent<BlockDragInput>();
    }
}
