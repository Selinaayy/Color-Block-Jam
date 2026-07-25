using UnityEngine;
using UnityEngine.Serialization;

public class BlockMover : MonoBehaviour
{
    public enum BlockColor { Red, Blue, Yellow }

    [Header("Block Info")]
    [FormerlySerializedAs("renk")]
    public BlockColor color;

    [Header("Main Bounds (GameArea)")]
    public Transform gameArea;
    [FormerlySerializedAs("sinirlariOtomatikHesapla")]
    public bool autoCalculateBounds = true;
    public float minX = -1f;
    public float maxX = 1f;
    public float minZ = -1f;
    public float maxZ = 1f;

    [Header("Exit Zones")]
    [FormerlySerializedAs("kirmiziExit")]
    public ExitZone redExit;
    [FormerlySerializedAs("maviExit")]
    public ExitZone blueExit;
    [FormerlySerializedAs("sariExit")]
    public ExitZone yellowExit;

    [Header("Grid Alignment")]
    [FormerlySerializedAs("baslangictaGridaHizala")]
    public bool alignToGridOnStart = true;
    [FormerlySerializedAs("birakincaGridaHizala")]
    public bool alignToGridOnRelease = true;
    [FormerlySerializedAs("adimAdimHareket")]
    public bool stepByStepMovement = true;
    public bool smoothSlideMovement = false;

    private bool isDragging = false;
    private bool smoothAxisLocked;
    private bool smoothAxisIsX;
    private Vector3 dragOffset;
    private Vector3 dragStartMouseWorldPosition;
    private int dragStartIndexX;
    private int dragStartIndexZ;
    private int cellCountX = 1;
    private int cellCountZ = 1;
    private int gridStartX;
    private int gridStartZ;
    private bool footprintReady;
    private bool hasExited = false;

    public bool HasExited => hasExited;
    private BoxCollider boxCollider;
    private Rigidbody cachedRigidbody;
    private Collider physicsCollider;
    private Renderer visualRenderer;
    private Bounds playAreaBounds;
    private bool playAreaBoundsReady = false;

    public BoxCollider CachedBoxCollider
    {
        get
        {
            if (boxCollider == null)
            {
                TryGetComponent(out boxCollider);
            }

            return boxCollider;
        }
    }

    public Bounds GetWorldOccupancyBounds()
    {
        if (visualRenderer == null)
        {
            visualRenderer = ComponentCacheUtility.FindFirstRenderer(transform);
        }

        if (visualRenderer != null)
        {
            return visualRenderer.bounds;
        }

        if (boxCollider == null)
        {
            TryGetComponent(out boxCollider);
        }

        return boxCollider != null
            ? boxCollider.bounds
            : new Bounds(transform.position, Vector3.zero);
    }

    private Bounds GetOccupancyBoundsAtPosition(Vector3 position)
    {
        Vector3 previousPosition = transform.position;
        transform.position = position;
        Bounds bounds = GetWorldOccupancyBounds();
        transform.position = previousPosition;
        return bounds;
    }

    [System.Serializable]
    public class ExitZone
    {
        public Transform exitTransform;
        [FormerlySerializedAs("boyut")]
        public Vector2 size = new Vector2(1.2f, 1.2f);

        public Vector2 GetDoorSize()
        {
            return new Vector2(
                Mathf.Max(size.x, GridConfig.CellSize),
                Mathf.Max(size.y, GridConfig.CellSize));
        }

        public bool ContainsBlock(Vector3 blockPosition, BoxCollider blockCollider, Transform blockTransform)
        {
            return DoorOverlap(blockPosition, blockCollider, blockTransform, GetDoorSize());
        }

        public bool IsValidExit(Vector3 blockPosition, BoxCollider blockCollider, Transform blockTransform, Bounds playArea)
        {
            if (exitTransform == null) return false;
            if (!DoorOverlap(blockPosition, blockCollider, blockTransform, GetDoorSize()))
            {
                return false;
            }

            GetBlockHalfExtents(blockCollider, blockTransform, out float blockHalfX, out float blockHalfZ);
            Vector3 exitPosition = exitTransform.position;
            float edgeThreshold = GridConfig.CellSize * 0.15f;

            if (exitPosition.x >= playArea.max.x - edgeThreshold)
            {
                return blockPosition.x + blockHalfX >= playArea.max.x - edgeThreshold;
            }

            if (exitPosition.x <= playArea.min.x + edgeThreshold)
            {
                return blockPosition.x - blockHalfX <= playArea.min.x + edgeThreshold;
            }

            if (exitPosition.z >= playArea.max.z - edgeThreshold)
            {
                return blockPosition.z + blockHalfZ >= playArea.max.z - edgeThreshold;
            }

            if (exitPosition.z <= playArea.min.z + edgeThreshold)
            {
                return blockPosition.z - blockHalfZ <= playArea.min.z + edgeThreshold;
            }

            return false;
        }

        private static void GetBlockHalfExtents(BoxCollider blockCollider, Transform blockTransform, out float blockHalfX, out float blockHalfZ)
        {
            blockHalfX = 0.25f;
            blockHalfZ = 0.5f;

            if (blockCollider == null) return;

            Vector3 scale = blockTransform.lossyScale;
            blockHalfX = blockCollider.size.x * scale.x / 2f;
            blockHalfZ = blockCollider.size.z * scale.z / 2f;
        }

        private bool DoorOverlap(Vector3 blockPosition, BoxCollider blockCollider, Transform blockTransform, Vector2 doorSize)
        {
            if (exitTransform == null) return false;

            float halfExitX = doorSize.x / 2f;
            float halfExitZ = doorSize.y / 2f;
            Vector3 exitPosition = exitTransform.position;
            GetBlockHalfExtents(blockCollider, blockTransform, out float blockHalfX, out float blockHalfZ);

            return blockPosition.x + blockHalfX >= exitPosition.x - halfExitX &&
                   blockPosition.x - blockHalfX <= exitPosition.x + halfExitX &&
                   blockPosition.z + blockHalfZ >= exitPosition.z - halfExitZ &&
                   blockPosition.z - blockHalfZ <= exitPosition.z + halfExitZ;
        }
    }

    void Awake()
    {
        TryGetComponent(out boxCollider);
        TryGetComponent(out cachedRigidbody);
        TryGetComponent(out physicsCollider);
        visualRenderer = ComponentCacheUtility.FindFirstRenderer(transform);

        EnsurePhysicsSetup();
        RegisterAllCollidersForBlock(this, transform);
    }

    void OnDestroy()
    {
        GridBoard.Unregister(this);
        BlockRegistry.UnregisterBlock(this);
    }

    private void EnsurePhysicsSetup()
    {
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        if (cachedRigidbody == null)
        {
            cachedRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        cachedRigidbody.isKinematic = true;
        cachedRigidbody.useGravity = false;
    }

    private static void RegisterAllCollidersForBlock(BlockMover block, Transform node)
    {
        if (block == null || node == null)
        {
            return;
        }

        if (node.TryGetComponent(out Collider collider))
        {
            collider.isTrigger = false;
            BlockRegistry.Register(block, collider);
        }

        for (int i = 0; i < node.childCount; i++)
        {
            RegisterAllCollidersForBlock(block, node.GetChild(i));
        }
    }

    public bool ContainsWorldPoint(Vector3 worldPoint)
    {
        if (boxCollider == null)
        {
            TryGetComponent(out boxCollider);
        }

        if (boxCollider == null)
        {
            return false;
        }

        Bounds bounds = boxCollider.bounds;
        const float padding = 0.05f;

        return worldPoint.x >= bounds.min.x - padding &&
               worldPoint.x <= bounds.max.x + padding &&
               worldPoint.z >= bounds.min.z - padding &&
               worldPoint.z <= bounds.max.z + padding;
    }

    public float GetSquaredDistanceToPointXZ(Vector3 worldPoint)
    {
        float deltaX = worldPoint.x - transform.position.x;
        float deltaZ = worldPoint.z - transform.position.z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }

    void Start()
    {
        RegisterAllCollidersForBlock(this, transform);

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
            GridConfig.NormalizeCollider(boxCollider, transform);
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }

        if (autoCalculateBounds)
        {
            CalculateBounds();
        }

        if (alignToGridOnStart && GetComponentInParent<BlockGridAligner>() == null)
        {
            AlignToGridCell();
        }
    }

    public void AlignToGridCell()
    {
        if (boxCollider == null)
        {
            TryGetComponent(out boxCollider);
        }

        if (boxCollider == null)
        {
            return;
        }

        GridConfig.NormalizeCollider(boxCollider, transform);
        Vector3 snapped = GridConfig.SnapVisualPosition(transform.position, boxCollider, transform, visualRenderer);
        GridConfig.AlignVisualFootprint(transform, boxCollider, snapped, visualRenderer);
        RefreshFootprint();
    }

    public void RefreshFootprint()
    {
        if (boxCollider == null)
        {
            TryGetComponent(out boxCollider);
        }

        if (boxCollider == null)
        {
            footprintReady = false;
            return;
        }

        GridConfig.EnsureInitialized();
        GridConfig.GetFootprintSize(boxCollider, transform, out cellCountX, out cellCountZ);

        Vector3 logicalCenter = transform.position;
        if (GridConfig.TryGetFootprintIndices(logicalCenter, cellCountX, cellCountZ, out gridStartX, out gridStartZ))
        {
            footprintReady = true;
            GridBoard.Register(this, gridStartX, gridStartZ, cellCountX, cellCountZ);
        }
        else
        {
            footprintReady = false;
        }
    }

    private void SetFootprint(int startX, int startZ)
    {
        gridStartX = startX;
        gridStartZ = startZ;
        footprintReady = true;
        GridBoard.Register(this, startX, startZ, cellCountX, cellCountZ);
    }

    private void CalculateBounds()
    {
        if (gameArea == null)
        {
            Transform area = SceneObjectRegistry.GetGameArea();
            if (area != null)
            {
                gameArea = area;
            }
        }

        if (gameArea != null)
        {
            SceneObjectRegistry.RegisterGameArea(gameArea);
        }

        if (gameArea == null) return;

        bool gridFound = false;
        Bounds bounds = default;

        for (int i = 0; i < gameArea.childCount; i++)
        {
            CollectGridBounds(gameArea.GetChild(i), ref bounds, ref gridFound);
        }

        if (!gridFound)
        {
            var renderers = new System.Collections.Generic.List<Renderer>();
            CollectRenderersInHierarchy(gameArea, renderers);

            if (renderers.Count == 0)
            {
                GridConfig.EnsureInitialized();
                bounds = GridConfig.GetPlayAreaBounds(transform.position.y);
                gridFound = true;
            }
            else
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Count; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
        }

        float halfX = 0.25f;
        float halfZ = 0.25f;
        if (boxCollider != null)
        {
            Vector3 scale = transform.lossyScale;
            halfX = boxCollider.size.x * scale.x / 2f;
            halfZ = boxCollider.size.z * scale.z / 2f;
        }

        minX = bounds.min.x + halfX;
        maxX = bounds.max.x - halfX;
        minZ = bounds.min.z + halfZ;
        maxZ = bounds.max.z - halfZ;

        if (autoCalculateBounds && boxCollider != null)
        {
            GridConfig.EnsureInitialized();
            GridConfig.GetFootprintSize(boxCollider, transform, out int cellsX, out int cellsZ);
            GridConfig.GetBlockCenterLimits(cellsX, cellsZ, out float gridMinX, out float gridMaxX, out float gridMinZ, out float gridMaxZ);
            minX = Mathf.Max(minX, gridMinX);
            maxX = Mathf.Min(maxX, gridMaxX);
            minZ = Mathf.Max(minZ, gridMinZ);
            maxZ = Mathf.Min(maxZ, gridMaxZ);
        }

        playAreaBounds = bounds;
        playAreaBoundsReady = true;
    }

    private static void CollectGridBounds(Transform current, ref Bounds bounds, ref bool gridFound)
    {
        if (current.name.StartsWith("Grid"))
        {
            if (current.TryGetComponent(out Renderer renderer))
            {
                if (!gridFound)
                {
                    bounds = renderer.bounds;
                    gridFound = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectGridBounds(current.GetChild(i), ref bounds, ref gridFound);
        }
    }

    private static void CollectRenderersInHierarchy(Transform current, System.Collections.Generic.List<Renderer> renderers)
    {
        if (current.TryGetComponent(out Renderer renderer))
        {
            renderers.Add(renderer);
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CollectRenderersInHierarchy(current.GetChild(i), renderers);
        }
    }

    private Bounds GetPlayAreaBounds()
    {
        if (playAreaBoundsReady)
        {
            return playAreaBounds;
        }

        CalculateBounds();

        if (!playAreaBoundsReady)
        {
            playAreaBounds = new Bounds(
                new Vector3((minX + maxX) / 2f, transform.position.y, (minZ + maxZ) / 2f),
                new Vector3(Mathf.Max(maxX - minX, 1f), 1f, Mathf.Max(maxZ - minZ, 1f)));
            playAreaBoundsReady = true;
        }

        return playAreaBounds;
    }

    public bool TryBeginDrag()
    {
        if (isDragging || hasExited || Time.timeScale == 0f || Camera.main == null)
        {
            return false;
        }

        if (color == BlockColor.Yellow)
        {
            StartCoroutine(LockedBlockShake());
            return false;
        }

        isDragging = true;
        smoothAxisLocked = false;
        dragStartMouseWorldPosition = GetMouseWorldPosition();

        if (autoCalculateBounds)
        {
            CalculateBounds();
        }

        if (UsesGridFootprintTracking())
        {
            PrepareGridFootprintForDrag();
        }

        dragOffset = transform.position - dragStartMouseWorldPosition;
        return true;
    }

    public void ConfigureMovement(bool stepByStep, bool smoothSlide)
    {
        stepByStepMovement = stepByStep;
        smoothSlideMovement = smoothSlide;
    }

    private bool UsesGridFootprintTracking()
    {
        return stepByStepMovement || smoothSlideMovement;
    }

    private void PrepareGridFootprintForDrag()
    {
        GridConfig.EnsureInitialized();
        GridConfig.GetFootprintSize(boxCollider, transform, out cellCountX, out cellCountZ);

        foreach (BlockMover block in BlockRegistry.All)
        {
            if (block != null && !block.HasExited)
            {
                block.RefreshFootprint();
            }
        }

        if (!footprintReady)
        {
            RefreshFootprint();
        }

        if (footprintReady)
        {
            dragStartIndexX = gridStartX;
            dragStartIndexZ = gridStartZ;
        }
        else
        {
            GridConfig.TryGetFootprintIndices(transform.position, cellCountX, cellCountZ,
                out dragStartIndexX, out dragStartIndexZ);
        }
    }

    public void UpdateDrag()
    {
        if (!isDragging || hasExited || Camera.main == null)
        {
            return;
        }

        if (stepByStepMovement)
        {
            DragStepByStep();
            return;
        }

        if (smoothSlideMovement)
        {
            DragSmoothSlide();
            return;
        }

        DragFree();
    }

    public void EndDrag()
    {
        if (!isDragging || hasExited)
        {
            return;
        }

        isDragging = false;

        if (IsInsideOwnExit(transform.position))
        {
            OnExitDoor();
            return;
        }

        Vector3 clampedPosition = ClampPosition(transform.position);

        if (alignToGridOnRelease)
        {
            Vector3 snappedPosition = GridConfig.SnapVisualPosition(clampedPosition, boxCollider, transform, visualRenderer);
            snappedPosition = ClampPosition(snappedPosition);

            if (!HasOtherBlockAtPosition(snappedPosition))
            {
                clampedPosition = snappedPosition;
            }
        }

        GridConfig.AlignVisualFootprint(transform, boxCollider, clampedPosition, visualRenderer);
        transform.position = ClampPosition(transform.position);
        RefreshFootprint();
    }

    private void DragStepByStep()
    {
        Vector3 mouseDelta = GetMouseWorldPosition() - dragStartMouseWorldPosition;
        mouseDelta.y = 0f;

        int targetIndexX = dragStartIndexX;
        int targetIndexZ = dragStartIndexZ;
        bool xAxis = Mathf.Abs(mouseDelta.x) >= Mathf.Abs(mouseDelta.z);

        if (xAxis)
        {
            targetIndexX += Mathf.RoundToInt(mouseDelta.x / GridConfig.CellSize);
        }
        else
        {
            targetIndexZ += Mathf.RoundToInt(mouseDelta.z / GridConfig.CellSize);
        }

        int indexX = dragStartIndexX;
        int indexZ = dragStartIndexZ;

        if (xAxis)
        {
            indexX = StepIndex(indexX, targetIndexX, indexZ, true);
        }
        else
        {
            indexZ = StepIndex(indexZ, targetIndexZ, indexX, false);
        }

        Vector3 targetPosition = GridConfig.GetCenterFromFootprint(
            indexX, indexZ, cellCountX, cellCountZ, transform.position.y);
        targetPosition = ClampPosition(targetPosition);

        ApplyTargetPosition(indexX, indexZ, targetPosition);
    }

    private void ApplyTargetPosition(int startX, int startZ, Vector3 targetPosition)
    {
        if (IsInsideOwnExit(targetPosition))
        {
            MoveToFootprint(startX, startZ, targetPosition);
            OnExitDoor();
            return;
        }

        if (IsInsideOtherColorExit(targetPosition))
        {
            return;
        }

        if (HasBlockFootprintOverlap(startX, startZ, cellCountX, cellCountZ))
        {
            return;
        }

        if (HasWallAtPosition(targetPosition))
        {
            return;
        }

        MoveToFootprint(startX, startZ, targetPosition);
    }

    private void MoveToFootprint(int startX, int startZ, Vector3 targetPosition)
    {
        GridConfig.AlignVisualFootprint(transform, boxCollider, targetPosition, visualRenderer);
        transform.position = ClampPosition(transform.position);
        SetFootprint(startX, startZ);

        if (isDragging && stepByStepMovement)
        {
            dragStartIndexX = startX;
            dragStartIndexZ = startZ;
            dragStartMouseWorldPosition = GetMouseWorldPosition();
        }
    }

    private int StepIndex(int current, int target, int fixedOtherAxisIndex, bool xAxis)
    {
        if (current == target) return current;

        int step = current < target ? 1 : -1;
        int maxIndex = xAxis
            ? GridConfig.GetGridColumnCount() - cellCountX
            : GridConfig.GetGridRowCount() - cellCountZ;

        while (current != target)
        {
            int next = current + step;
            if (next < 0 || next > maxIndex)
            {
                break;
            }

            Vector3 testPosition = xAxis
                ? GridConfig.GetCenterFromFootprint(next, fixedOtherAxisIndex, cellCountX, cellCountZ, transform.position.y)
                : GridConfig.GetCenterFromFootprint(fixedOtherAxisIndex, next, cellCountX, cellCountZ, transform.position.y);

            testPosition = ClampPosition(testPosition);

            int testStartX = xAxis ? next : fixedOtherAxisIndex;
            int testStartZ = xAxis ? fixedOtherAxisIndex : next;

            if (HasBlockFootprintOverlap(testStartX, testStartZ, cellCountX, cellCountZ) ||
                IsInsideOtherColorExit(testPosition) ||
                HasWallAtPosition(testPosition))
            {
                break;
            }

            current = next;
        }

        return current;
    }

    private void DragSmoothSlide()
    {
        Vector3 mouseTarget = GetMouseWorldPosition() + dragOffset;
        mouseTarget.y = transform.position.y;
        Vector3 delta = mouseTarget - transform.position;

        if (!smoothAxisLocked)
        {
            float axisPickThreshold = GridConfig.CellSize * 0.15f;
            if (delta.sqrMagnitude < axisPickThreshold * axisPickThreshold)
            {
                return;
            }

            smoothAxisIsX = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z);
            smoothAxisLocked = true;
        }

        Vector3 targetPosition = transform.position;
        if (smoothAxisIsX)
        {
            targetPosition.x = mouseTarget.x;
        }
        else
        {
            targetPosition.z = mouseTarget.z;
        }

        if (IsInsideOwnExit(targetPosition))
        {
            transform.position = targetPosition;
            OnExitDoor();
            return;
        }

        ApplyDragPosition(ResolveAxisLockedPosition(targetPosition));
    }

    private Vector3 ResolveAxisLockedPosition(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.position;
        targetPosition = ClampPosition(targetPosition);
        targetPosition.y = currentPosition.y;

        if (smoothAxisIsX)
        {
            targetPosition.z = currentPosition.z;
        }
        else
        {
            targetPosition.x = currentPosition.x;
        }

        if (!HasObstructionAtPosition(targetPosition))
        {
            return targetPosition;
        }

        float validAxis = smoothAxisIsX ? currentPosition.x : currentPosition.z;
        float targetAxis = smoothAxisIsX ? targetPosition.x : targetPosition.z;

        if (Mathf.Approximately(validAxis, targetAxis))
        {
            return currentPosition;
        }

        float invalidAxis = targetAxis;

        for (int i = 0; i < 20; i++)
        {
            float midAxis = (validAxis + invalidAxis) * 0.5f;
            Vector3 testPosition = currentPosition;
            if (smoothAxisIsX)
            {
                testPosition.x = midAxis;
            }
            else
            {
                testPosition.z = midAxis;
            }

            if (HasObstructionAtPosition(testPosition))
            {
                invalidAxis = midAxis;
            }
            else
            {
                validAxis = midAxis;
            }
        }

        Vector3 resolvedPosition = currentPosition;
        if (smoothAxisIsX)
        {
            resolvedPosition.x = validAxis;
        }
        else
        {
            resolvedPosition.z = validAxis;
        }

        return ClampPosition(resolvedPosition);
    }

    private bool HasObstructionAtPosition(Vector3 position)
    {
        return IsInsideOtherColorExit(position)
            || HasBlockColliderOverlapAtPosition(position)
            || HasWallAtPosition(position);
    }

    private void DragFree()
    {
        Vector3 targetPosition = GetMouseWorldPosition() + dragOffset;
        targetPosition.y = transform.position.y;
        TryMoveToPosition(targetPosition);
    }

    private void TryMoveToPosition(Vector3 targetPosition)
    {
        if (IsInsideOwnExit(targetPosition))
        {
            transform.position = targetPosition;
            OnExitDoor();
            return;
        }

        if (IsInsideOtherColorExit(targetPosition))
        {
            SlideMove(targetPosition);
            return;
        }

        targetPosition = ClampPosition(targetPosition);

        if (!HasOtherBlockAtPosition(targetPosition))
        {
            ApplyDragPosition(targetPosition);
            return;
        }

        SlideMove(targetPosition);
    }

    private void SlideMove(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.position;

        Vector3 testX = ClampPosition(new Vector3(targetPosition.x, currentPosition.y, currentPosition.z));
        if (!HasOtherBlockAtPosition(testX) && !IsInsideOtherColorExit(testX))
        {
            ApplyDragPosition(testX);
            return;
        }

        Vector3 testZ = ClampPosition(new Vector3(currentPosition.x, currentPosition.y, targetPosition.z));
        if (!HasOtherBlockAtPosition(testZ) && !IsInsideOtherColorExit(testZ))
        {
            ApplyDragPosition(testZ);
        }
    }

    private void ApplyDragPosition(Vector3 position)
    {
        transform.position = position;

        if (smoothSlideMovement && isDragging)
        {
            RefreshFootprint();
        }
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        if (minX <= maxX)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minZ <= maxZ)
        {
            position.z = Mathf.Clamp(position.z, minZ, maxZ);
        }

        return position;
    }

    private bool IsInsideOtherColorExit(Vector3 position)
    {
        if (color != BlockColor.Red && redExit != null && redExit.ContainsBlock(position, boxCollider, transform)) return true;
        if (color != BlockColor.Blue && blueExit != null && blueExit.ContainsBlock(position, boxCollider, transform)) return true;
        if (color != BlockColor.Yellow && yellowExit != null && yellowExit.ContainsBlock(position, boxCollider, transform)) return true;
        return false;
    }

    private bool HasBlockFootprintOverlap(int startX, int startZ, int cellsX, int cellsZ)
    {
        return !GridBoard.CanPlace(startX, startZ, cellsX, cellsZ, this);
    }

    private bool HasWallAtPosition(Vector3 position)
    {
        if (boxCollider == null)
        {
            return false;
        }

        Vector3 previousPosition = transform.position;
        transform.position = position;

        Vector3 center = boxCollider.bounds.center;
        Vector3 halfExtents = boxCollider.bounds.extents * 0.9f;
        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, transform.rotation);

        transform.position = previousPosition;

        foreach (Collider overlap in overlaps)
        {
            if (overlap == null || !overlap.enabled || overlap.isTrigger)
            {
                continue;
            }

            if (overlap.transform == transform || overlap.transform.IsChildOf(transform))
            {
                continue;
            }

            if (BlockRegistry.TryGetBlockFromCollider(overlap, out _))
            {
                continue;
            }

            if (IsWallCollider(overlap))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasOtherBlockAtPosition(Vector3 position)
    {
        if (boxCollider == null)
        {
            return false;
        }

        if (smoothSlideMovement)
        {
            return HasObstructionAtPosition(position);
        }

        GridConfig.GetFootprintSize(boxCollider, transform, out int cellsX, out int cellsZ);

        Vector3 previousPosition = transform.position;
        transform.position = position;
        Vector3 center = boxCollider.bounds.center;
        transform.position = previousPosition;

        if (GridConfig.TryGetFootprintIndices(center, cellsX, cellsZ, out int startX, out int startZ) &&
            HasBlockFootprintOverlap(startX, startZ, cellsX, cellsZ))
        {
            return true;
        }

        return HasWallAtPosition(position);
    }

    private bool HasBlockColliderOverlapAtPosition(Vector3 position)
    {
        Bounds testBounds = GetOccupancyBoundsAtPosition(position);

        foreach (BlockMover other in BlockRegistry.All)
        {
            if (other == null || other == this || other.HasExited)
            {
                continue;
            }

            if (BoundsOverlapXZ(testBounds, other.GetWorldOccupancyBounds(), BlockSeparationGap))
            {
                return true;
            }
        }

        return false;
    }

    private const float BlockSeparationGap = 0.005f;

    private static bool BoundsOverlapXZ(Bounds a, Bounds b, float separationGap)
    {
        return a.min.x < b.max.x - separationGap && a.max.x > b.min.x + separationGap &&
               a.min.z < b.max.z - separationGap && a.max.z > b.min.z + separationGap;
    }

    private static bool IsWallCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        return collider.gameObject.CompareTag("Wall");
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (BlockPicker.TryGetBoardWorldPoint(out Vector3 worldPoint))
        {
            worldPoint.y = transform.position.y;
            return worldPoint;
        }

        if (Camera.main == null)
        {
            return transform.position;
        }

        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = 10f;
        return Camera.main.ScreenToWorldPoint(screenPosition);
    }

    private ExitZone GetOwnExitZone()
    {
        if (color == BlockColor.Red) return redExit;
        if (color == BlockColor.Blue) return blueExit;
        if (color == BlockColor.Yellow) return yellowExit;
        return null;
    }

    private bool IsInsideOwnExit(Vector3 position)
    {
        ExitZone ownExit = GetOwnExitZone();
        if (ownExit == null) return false;
        return ownExit.IsValidExit(position, boxCollider, transform, GetPlayAreaBounds());
    }

    void OnExitDoor()
    {
        if (hasExited) return;

        hasExited = true;
        isDragging = false;

        if (cachedRigidbody != null)
        {
            if (!cachedRigidbody.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                cachedRigidbody.linearVelocity = Vector3.zero;
#else
                cachedRigidbody.velocity = Vector3.zero;
#endif
                cachedRigidbody.angularVelocity = Vector3.zero;
            }

            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
        }

        if (physicsCollider != null) physicsCollider.enabled = false;

        GridBoard.Unregister(this);

        StartCoroutine(FadeOutAndDestroy());
    }

    private System.Collections.IEnumerator FadeOutAndDestroy()
    {
        float elapsed = 0f;
        float duration = 0.25f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBlockExited();
        }

        Destroy(gameObject);
    }

    private bool isShaking = false;
    private System.Collections.IEnumerator LockedBlockShake()
    {
        if (isShaking) yield break;
        isShaking = true;

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.Vibrate();
        }

        Vector3 originalPosition = transform.position;
        float duration = 0.25f;
        float amplitude = 0.05f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Mathf.Sin(elapsed * 40f) * amplitude * (1f - elapsed / duration);
            transform.position = originalPosition + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }

        transform.position = originalPosition;
        isShaking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2f, transform.position.y, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 0.05f, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);

        ExitZone ownExit = GetOwnExitZone();
        if (ownExit != null && ownExit.exitTransform != null)
        {
            Vector2 doorSize = ownExit.GetDoorSize();
            Gizmos.color = Color.green;
            Vector3 exitCenter = new Vector3(
                ownExit.exitTransform.position.x,
                transform.position.y,
                ownExit.exitTransform.position.z);
            Gizmos.DrawWireCube(exitCenter, new Vector3(doorSize.x, 0.1f, doorSize.y));
        }
    }
}
