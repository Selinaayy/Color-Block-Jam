using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-100)]
[ExecuteAlways]
public class BlockGridAligner : MonoBehaviour
{
    [FormerlySerializedAs("acilisdaHizala")]
    public bool alignOnStart = true;

    private void Awake()
    {
        if (TryGetComponent(out BoxCollider parentCollider))
        {
            parentCollider.enabled = false;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying || !alignOnStart)
        {
            return;
        }

        StartCoroutine(AlignWhenReady());
    }

    private IEnumerator AlignWhenReady()
    {
        yield return null;
        AlignAllBlocks(false);
    }

    public void AlignAllBlocks(bool writeLog = true)
    {
        GridBoard.Clear();

        var blocks = new List<BlockMover>();
        ComponentCacheUtility.CollectBlocksInChildren(transform, transform, blocks, true);
        int count = AlignBlockCollection(blocks, transform);
        RefreshAllBlockFootprints();

#if UNITY_EDITOR
        if (!Application.isPlaying && count > 0)
        {
            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif

#if UNITY_EDITOR
        if (writeLog && !Application.isPlaying)
        {
            Debug.Log($"BlockGridAligner: aligned {count} blocks to grid.");
        }
#endif
    }

    public static int AlignBlockCollection(IReadOnlyList<BlockMover> blocks, Transform skipTransform = null)
    {
        int count = 0;

        for (int i = 0; i < blocks.Count; i++)
        {
            BlockMover block = blocks[i];
            if (block == null || block.transform == skipTransform)
            {
                continue;
            }

            BoxCollider collider = block.CachedBoxCollider;
            if (collider == null)
            {
                continue;
            }

            GridConfig.NormalizeCollider(collider, block.transform);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(block.transform, "Grid Align Block");
            }
#endif

            block.AlignToGridCell();
            count++;
        }

        return count;
    }

    public static void RefreshAllBlockFootprints()
    {
        IReadOnlyList<BlockMover> blocks = BlockRegistry.All;
        for (int i = 0; i < blocks.Count; i++)
        {
            BlockMover block = blocks[i];
            if (block != null && block.isActiveAndEnabled && !block.HasExited)
            {
                block.RefreshFootprint();
            }
        }
    }
}
