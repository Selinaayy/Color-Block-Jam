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

        if (scene.name == "Level1" || scene.name == "Level2")
        {
            ApplySmoothSlideMovement();
        }
    }

    private static void ApplySmoothSlideMovement()
    {
        BlockMover[] blocks = Object.FindObjectsOfType<BlockMover>();
        for (int i = 0; i < blocks.Length; i++)
        {
            BlockMover block = blocks[i];
            if (block != null)
            {
                block.ConfigureMovement(stepByStep: false, smoothSlide: true);
            }
        }
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
