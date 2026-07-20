using UnityEngine;

public class BlockDragInput : MonoBehaviour
{
    private static BlockDragInput instance;
    private BlockMover activeBlock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInputController()
    {
        if (instance != null)
        {
            return;
        }

        GameObject inputObject = new GameObject("BlockDragInput");
        instance = inputObject.AddComponent<BlockDragInput>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
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
