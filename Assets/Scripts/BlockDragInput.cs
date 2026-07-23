using UnityEngine;

public class BlockDragInput : MonoBehaviour
{
    public static BlockDragInput Instance { get; private set; }

    private BlockMover activeBlock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (BlockPicker.TryPickBlockAtMouse(out BlockMover block) && block.TryBeginDrag())
            {
                activeBlock = block;
            }
        }

        if (Input.GetMouseButton(0) && activeBlock != null)
        {
            activeBlock.UpdateDrag();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (activeBlock != null)
            {
                activeBlock.EndDrag();
                activeBlock = null;
            }
        }
    }
}
