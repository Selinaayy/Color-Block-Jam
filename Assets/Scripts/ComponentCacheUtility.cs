using UnityEngine;

public static class ComponentCacheUtility
{
    public static Renderer FindFirstRenderer(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        if (root.TryGetComponent(out Renderer renderer))
        {
            return renderer;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Renderer childRenderer = FindFirstRenderer(root.GetChild(i));
            if (childRenderer != null)
            {
                return childRenderer;
            }
        }

        return null;
    }

    public static Rigidbody[] CollectRigidbodiesInChildren(Transform root, bool includeInactive)
    {
        if (root == null)
        {
            return System.Array.Empty<Rigidbody>();
        }

        var results = new System.Collections.Generic.List<Rigidbody>();
        CollectRigidbodiesRecursive(root, includeInactive, results);
        return results.ToArray();
    }

    private static void CollectRigidbodiesRecursive(Transform current, bool includeInactive, System.Collections.Generic.List<Rigidbody> results)
    {
        if (!includeInactive && !current.gameObject.activeInHierarchy)
        {
            return;
        }

        if (current.TryGetComponent(out Rigidbody rigidbody))
        {
            results.Add(rigidbody);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectRigidbodiesRecursive(current.GetChild(i), includeInactive, results);
        }
    }

    public static void CollectBlocksInChildren(Transform root, Transform skipRoot, System.Collections.Generic.List<BlockMover> results, bool includeInactive)
    {
        if (root == null || results == null)
        {
            return;
        }

        if (root != skipRoot && (includeInactive || root.gameObject.activeInHierarchy))
        {
            if (root.TryGetComponent(out BlockMover block))
            {
                results.Add(block);
            }
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectBlocksInChildren(root.GetChild(i), skipRoot, results, includeInactive);
        }
    }
}
