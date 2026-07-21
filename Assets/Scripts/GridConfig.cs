using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GridConfig
{
    public const float CellSize = 0.5f;
    public const float BlockY = 0.067015596f;

    private static float[] gridXs;
    private static float[] gridZs;
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetForScene()
    {
        initialized = false;
        gridXs = null;
        gridZs = null;
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (initialized) return;

        Transform gameArea = SceneObjectRegistry.GetGameArea();
        if (gameArea != null)
        {
            List<float> xs = new List<float>();
            List<float> zs = new List<float>();

            CollectGridCoordinates(gameArea, xs, zs);

            if (xs.Count > 0)
            {
                gridXs = NormalizeEvenSpacing(MergeCloseValues(xs));
                gridZs = NormalizeEvenSpacing(MergeCloseValues(zs));
            }
        }

        if (gridXs == null || gridXs.Length == 0)
        {
            gridXs = BuildAxis(-0.991f, 5);
        }

        if (gridZs == null || gridZs.Length == 0)
        {
            gridZs = BuildAxis(0.85f, 8);
        }

        initialized = true;
    }

    private static void CollectGridCoordinates(Transform current, List<float> xs, List<float> zs)
    {
        if (current.name.StartsWith("Grid"))
        {
            xs.Add(current.position.x);
            zs.Add(current.position.z);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectGridCoordinates(current.GetChild(i), xs, zs);
        }
    }

    private static float[] MergeCloseValues(List<float> values)
    {
        List<float> sorted = values.OrderBy(v => v).ToList();
        List<float> merged = new List<float>();

        foreach (float value in sorted)
        {
            if (merged.Count == 0 || Mathf.Abs(value - merged[merged.Count - 1]) > 0.01f)
            {
                merged.Add(value);
            }
        }

        return merged.ToArray();
    }

    private static float[] NormalizeEvenSpacing(float[] merged)
    {
        if (merged == null || merged.Length == 0)
        {
            return merged;
        }

        return BuildAxis(merged[0], merged.Length);
    }

    private static float[] BuildAxis(float origin, int count)
    {
        float[] axis = new float[count];

        for (int i = 0; i < count; i++)
        {
            axis[i] = origin + i * CellSize;
        }

        return axis;
    }

    public static int GetCellCount(float worldSize)
    {
        return Mathf.Max(1, Mathf.RoundToInt(worldSize / CellSize));
    }

    public static float SnapAxis(float position, float[] gridCenters, int cellCount)
    {
        EnsureInitialized();

        if (gridCenters == null || gridCenters.Length == 0)
        {
            return position;
        }

        if (cellCount <= 1)
        {
            int bestIndex = 0;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < gridCenters.Length; i++)
            {
                float distance = Mathf.Abs(position - gridCenters[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return gridCenters[bestIndex];
        }

        float bestCenter = gridCenters[0];
        float bestCenterDistance = float.MaxValue;

        for (int i = 0; i <= gridCenters.Length - cellCount; i++)
        {
            float center = (gridCenters[i] + gridCenters[i + cellCount - 1]) * 0.5f;
            float distance = Mathf.Abs(position - center);

            if (distance < bestCenterDistance)
            {
                bestCenterDistance = distance;
                bestCenter = center;
            }
        }

        return bestCenter;
    }

    public static Vector3 SnapPosition(Vector3 position, BoxCollider collider, Transform blockTransform)
    {
        EnsureInitialized();

        int cellsX = 1;
        int cellsZ = 1;

        if (collider != null)
        {
            Vector3 scale = blockTransform.lossyScale;
            cellsX = GetCellCount(collider.size.x * scale.x);
            cellsZ = GetCellCount(collider.size.z * scale.z);
        }

        return new Vector3(
            SnapAxis(position.x, gridXs, cellsX),
            position.y,
            SnapAxis(position.z, gridZs, cellsZ));
    }

    public static Vector3 SnapVisualPosition(Vector3 position, BoxCollider collider, Transform blockTransform, Renderer visualRenderer = null)
    {
        if (visualRenderer == null)
        {
            visualRenderer = ComponentCacheUtility.FindFirstRenderer(blockTransform);
        }

        if (visualRenderer == null)
        {
            return SnapPosition(position, collider, blockTransform);
        }

        Vector3 visualOffset = visualRenderer.bounds.center - blockTransform.position;
        visualOffset.y = 0f;

        Vector3 snappedFootprint = SnapPosition(position + visualOffset, collider, blockTransform);
        return snappedFootprint - visualOffset;
    }

    public static void AlignVisualFootprint(Transform blockTransform, BoxCollider collider, Vector3 snappedPosition, Renderer visualRenderer = null)
    {
        if (collider == null) return;

        if (visualRenderer == null)
        {
            visualRenderer = ComponentCacheUtility.FindFirstRenderer(blockTransform);
        }

        if (visualRenderer == null)
        {
            blockTransform.position = snappedPosition;
            return;
        }

        blockTransform.position = snappedPosition;

        Vector3 scale = blockTransform.lossyScale;
        Vector3 colliderHalf = Vector3.Scale(collider.size, scale) * 0.5f;
        Vector3 colliderWorldCenter = blockTransform.position + Vector3.Scale(collider.center, scale);

        Vector3 targetMin = new Vector3(
            colliderWorldCenter.x - colliderHalf.x,
            blockTransform.position.y,
            colliderWorldCenter.z - colliderHalf.z);
        Vector3 targetMax = new Vector3(
            colliderWorldCenter.x + colliderHalf.x,
            blockTransform.position.y,
            colliderWorldCenter.z + colliderHalf.z);

        Bounds meshBounds = visualRenderer.bounds;
        Vector3 meshMin = new Vector3(meshBounds.min.x, blockTransform.position.y, meshBounds.min.z);
        Vector3 meshMax = new Vector3(meshBounds.max.x, blockTransform.position.y, meshBounds.max.z);

        Vector3 correction = ((targetMin - meshMin) + (targetMax - meshMax)) * 0.5f;
        correction.y = 0f;
        blockTransform.position += correction;
    }

    public static void NormalizeCollider(BoxCollider collider, Transform blockTransform)
    {
        if (collider == null) return;

        Vector3 scale = blockTransform.lossyScale;
        Vector3 size = collider.size;

        int cellsX = GetCellCount(size.x * scale.x);
        int cellsZ = GetCellCount(size.z * scale.z);

        collider.size = new Vector3(
            cellsX * CellSize / scale.x,
            size.y,
            cellsZ * CellSize / scale.z);
    }

    public static void GetFootprintSize(BoxCollider collider, Transform blockTransform, out int cellsX, out int cellsZ)
    {
        cellsX = 1;
        cellsZ = 1;

        if (collider != null)
        {
            Vector3 scale = blockTransform.lossyScale;
            cellsX = GetCellCount(collider.size.x * scale.x);
            cellsZ = GetCellCount(collider.size.z * scale.z);
        }
    }

    public static Vector3 GetCenterFromFootprint(int startX, int startZ, int cellsX, int cellsZ, float y)
    {
        EnsureInitialized();

        startX = Mathf.Clamp(startX, 0, gridXs.Length - cellsX);
        startZ = Mathf.Clamp(startZ, 0, gridZs.Length - cellsZ);

        float centerX = (gridXs[startX] + gridXs[startX + cellsX - 1]) * 0.5f;
        float centerZ = (gridZs[startZ] + gridZs[startZ + cellsZ - 1]) * 0.5f;

        return new Vector3(centerX, y, centerZ);
    }

    public static bool TryGetFootprintIndices(Vector3 center, int cellsX, int cellsZ, out int startX, out int startZ)
    {
        EnsureInitialized();

        startX = 0;
        startZ = 0;

        float bestDistance = float.MaxValue;
        bool found = false;

        for (int z = 0; z <= gridZs.Length - cellsZ; z++)
        {
            for (int x = 0; x <= gridXs.Length - cellsX; x++)
            {
                Vector3 candidate = GetCenterFromFootprint(x, z, cellsX, cellsZ, center.y);
                float distance = Vector3.Distance(
                    new Vector3(center.x, 0f, center.z),
                    new Vector3(candidate.x, 0f, candidate.z));

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    startX = x;
                    startZ = z;
                    found = true;
                }
            }
        }

        return found;
    }

    public static int GetGridColumnCount()
    {
        EnsureInitialized();
        return gridXs != null ? gridXs.Length : 0;
    }

    public static int GetGridRowCount()
    {
        EnsureInitialized();
        return gridZs != null ? gridZs.Length : 0;
    }

    public static Bounds GetPlayAreaBounds(float y)
    {
        EnsureInitialized();

        float minX = gridXs[0] - CellSize * 0.5f;
        float maxX = gridXs[gridXs.Length - 1] + CellSize * 0.5f;
        float minZ = gridZs[0] - CellSize * 0.5f;
        float maxZ = gridZs[gridZs.Length - 1] + CellSize * 0.5f;

        Vector3 center = new Vector3((minX + maxX) * 0.5f, y, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 1f, maxZ - minZ);
        return new Bounds(center, size);
    }

    public static void GetBlockCenterLimits(int cellsX, int cellsZ, out float minCenterX, out float maxCenterX, out float minCenterZ, out float maxCenterZ)
    {
        EnsureInitialized();

        int maxStartX = gridXs.Length - cellsX;
        int maxStartZ = gridZs.Length - cellsZ;

        minCenterX = GetCenterFromFootprint(0, 0, cellsX, cellsZ, 0f).x;
        maxCenterX = GetCenterFromFootprint(maxStartX, 0, cellsX, cellsZ, 0f).x;
        minCenterZ = GetCenterFromFootprint(0, 0, cellsX, cellsZ, 0f).z;
        maxCenterZ = GetCenterFromFootprint(0, maxStartZ, cellsX, cellsZ, 0f).z;
    }

    public static bool IsFootprintInBounds(int startX, int startZ, int cellsX, int cellsZ)
    {
        EnsureInitialized();

        if (startX < 0 || startZ < 0)
        {
            return false;
        }

        return startX + cellsX <= GetGridColumnCount()
            && startZ + cellsZ <= GetGridRowCount();
    }

    public static void ClampFootprintIndices(int cellsX, int cellsZ, ref int startX, ref int startZ)
    {
        EnsureInitialized();

        int maxStartX = GetGridColumnCount() - cellsX;
        int maxStartZ = GetGridRowCount() - cellsZ;

        startX = Mathf.Clamp(startX, 0, maxStartX);
        startZ = Mathf.Clamp(startZ, 0, maxStartZ);
    }

    public static bool FootprintsOverlap(
        int startAX, int startAZ, int cellsAX, int cellsAZ,
        int startBX, int startBZ, int cellsBX, int cellsBZ)
    {
        return startAX < startBX + cellsBX &&
               startAX + cellsAX > startBX &&
               startAZ < startBZ + cellsBZ &&
               startAZ + cellsAZ > startBZ;
    }
}
