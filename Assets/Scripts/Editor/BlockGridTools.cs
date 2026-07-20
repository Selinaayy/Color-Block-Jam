using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class BlockGridTools
{
    [MenuItem("Color Block Jam/Align Blocks To Grid")]
    public static void AlignAllBlocks()
    {
        var blocks = new List<BlockMover>();
        CollectAllBlockMovers(blocks);
        int count = 0;

        foreach (BlockMover block in blocks)
        {
            BoxCollider collider = block.CachedBoxCollider;
            GridConfig.NormalizeCollider(collider, block.transform);

            Undo.RecordObject(block.transform, "Grid Align Block");
            block.AlignToGridCell();
            count++;
        }

        Debug.Log($"Aligned {count} blocks to grid.");
    }

    private static void CollectAllBlockMovers(List<BlockMover> blocks)
    {
        for (int sceneIndex = 0; sceneIndex < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; sceneIndex++)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                ComponentCacheUtility.CollectBlocksInChildren(roots[i].transform, null, blocks, true);
            }
        }
    }
}
