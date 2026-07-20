using UnityEngine;

public static class BlockPicker
{
    public static bool TryGetBoardWorldPoint(out Vector3 worldPoint)
    {
        worldPoint = default;
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        GridConfig.EnsureInitialized();
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, GridConfig.BlockY, 0f));

        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        worldPoint = ray.GetPoint(distance);
        return true;
    }

    public static bool TryPickBlockAtWorldPoint(Vector3 worldPoint, out BlockMover block)
    {
        block = null;
        float bestDistanceSquared = float.MaxValue;

        foreach (BlockMover candidate in BlockRegistry.All)
        {
            if (candidate == null || !candidate.isActiveAndEnabled || candidate.HasExited)
            {
                continue;
            }

            if (!candidate.ContainsWorldPoint(worldPoint))
            {
                continue;
            }

            float distanceSquared = candidate.GetSquaredDistanceToPointXZ(worldPoint);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                block = candidate;
            }
        }

        return block != null;
    }

    public static bool TryPickBlockAtMouse(out BlockMover block)
    {
        block = null;
        if (!TryGetBoardWorldPoint(out Vector3 worldPoint))
        {
            return false;
        }

        return TryPickBlockAtWorldPoint(worldPoint, out block);
    }
}
