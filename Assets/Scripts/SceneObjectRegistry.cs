using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneObjectRegistry
{
    private static Transform gameArea;
    private static AutoExplodeOnPlay autoExplode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        gameArea = null;
        autoExplode = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= OnActiveSceneLoaded;
        SceneManager.sceneLoaded += OnActiveSceneLoaded;
    }

    private static void OnActiveSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearSceneObjects();
    }

    public static void ClearSceneObjects()
    {
        gameArea = null;
        autoExplode = null;
    }

    public static void RegisterGameArea(Transform area)
    {
        if (area != null && gameArea == null)
        {
            gameArea = area;
        }
    }

    public static Transform GetGameArea()
    {
        if (gameArea != null)
        {
            return gameArea;
        }

        Transform found = FindTransformByName("GameArea");
        if (found != null)
        {
            gameArea = found;
        }

        return gameArea;
    }

    public static void RegisterAutoExplode(AutoExplodeOnPlay explode)
    {
        if (explode != null && autoExplode == null)
        {
            autoExplode = explode;
        }
    }

    public static void UnregisterAutoExplode(AutoExplodeOnPlay explode)
    {
        if (autoExplode == explode)
        {
            autoExplode = null;
        }
    }

    public static AutoExplodeOnPlay GetAutoExplode()
    {
        if (autoExplode != null)
        {
            return autoExplode;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            AutoExplodeOnPlay found = FindAutoExplodeInHierarchy(roots[i].transform);
            if (found != null)
            {
                autoExplode = found;
                return autoExplode;
            }
        }

        return null;
    }

    private static AutoExplodeOnPlay FindAutoExplodeInHierarchy(Transform current)
    {
        if (current.TryGetComponent(out AutoExplodeOnPlay explode))
        {
            return explode;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            AutoExplodeOnPlay found = FindAutoExplodeInHierarchy(current.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        Transform gameAreaTransform = FindTransformByName("GameArea");
        if (gameAreaTransform != null)
        {
            RegisterGameArea(gameAreaTransform);
        }
    }

    public static Transform FindTransformByName(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindTransformInHierarchy(roots[i].transform, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static GameObject FindGameObjectByName(string objectName)
    {
        Transform found = FindTransformByName(objectName);
        return found != null ? found.gameObject : null;
    }

    private static Transform FindTransformInHierarchy(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformInHierarchy(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public static void CollectTransformsInHierarchy(Transform root, List<Transform> results, bool includeInactive)
    {
        if (root == null || results == null)
        {
            return;
        }

        if (includeInactive || root.gameObject.activeInHierarchy)
        {
            results.Add(root);
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectTransformsInHierarchy(root.GetChild(i), results, includeInactive);
        }
    }
}
