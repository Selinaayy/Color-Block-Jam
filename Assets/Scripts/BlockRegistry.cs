using System.Collections.Generic;
using UnityEngine;

public static class BlockRegistry
{
    private static readonly List<BlockMover> blocks = new List<BlockMover>();
    private static readonly Dictionary<int, BlockMover> colliderToBlock = new Dictionary<int, BlockMover>();

    public static IReadOnlyList<BlockMover> All => blocks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        blocks.Clear();
        colliderToBlock.Clear();
    }

    public static void Register(BlockMover block, Collider collider)
    {
        if (block != null && !blocks.Contains(block))
        {
            blocks.Add(block);
        }

        if (collider != null && block != null)
        {
            colliderToBlock[collider.GetInstanceID()] = block;
        }
    }

    public static void Unregister(BlockMover block, Collider collider)
    {
        if (collider != null)
        {
            colliderToBlock.Remove(collider.GetInstanceID());
        }

        UnregisterBlock(block);
    }

    public static void UnregisterBlock(BlockMover block)
    {
        if (block == null)
        {
            return;
        }

        blocks.Remove(block);

        var colliderIdsToRemove = new List<int>();
        foreach (KeyValuePair<int, BlockMover> entry in colliderToBlock)
        {
            if (entry.Value == block)
            {
                colliderIdsToRemove.Add(entry.Key);
            }
        }

        for (int i = 0; i < colliderIdsToRemove.Count; i++)
        {
            colliderToBlock.Remove(colliderIdsToRemove[i]);
        }
    }

    public static bool TryGetBlockFromCollider(Collider collider, out BlockMover block)
    {
        block = null;
        if (collider == null)
        {
            return false;
        }

        if (colliderToBlock.TryGetValue(collider.GetInstanceID(), out block))
        {
            return block != null;
        }

        Transform current = collider.transform;
        while (current != null)
        {
            if (current.TryGetComponent(out block) && block.isActiveAndEnabled)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
