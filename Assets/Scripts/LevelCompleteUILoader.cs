using System.Collections.Generic;
using UnityEngine;

public static class LevelCompleteUILoader
{
    private const string ResourcesFolder = "LevelCompleteUI";
    private static Dictionary<string, Sprite> sprites;
    private static bool loaded;

    public static Sprite Get(string spriteName)
    {
        EnsureLoaded();
        sprites.TryGetValue(spriteName, out Sprite sprite);
        return sprite;
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        sprites = new Dictionary<string, Sprite>();

        Sprite[] allSprites = Resources.LoadAll<Sprite>(ResourcesFolder);
        foreach (Sprite sprite in allSprites)
        {
            if (sprite != null && !sprites.ContainsKey(sprite.name))
            {
                sprites.Add(sprite.name, sprite);
            }
        }

        string[] expectedNames =
        {
            "Banner",
            "Bg_popup_",
            "bg_level_top_Blue",
            "BarBlue",
            "ButtonBlue",
            "LevelButton",
            "Gold",
            "SmallButtonBlue",
            "Close",
            "Restart"
        };

        foreach (string name in expectedNames)
        {
            if (sprites.ContainsKey(name))
            {
                continue;
            }

            Sprite sprite = Resources.Load<Sprite>(ResourcesFolder + "/" + name);
            if (sprite != null)
            {
                sprites.Add(name, sprite);
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogError("LevelCompleteUILoader: No sprites found in Resources/" + ResourcesFolder + ". Reimport PNG files as Sprite (2D and UI).");
        }
    }
}
