using System.Collections.Generic;
using UnityEngine;

public static class GridBoard
{
    private struct FootprintData
    {
        public int StartX;
        public int StartZ;
        public int CellsX;
        public int CellsZ;
    }

    private static readonly Dictionary<BlockMover, FootprintData> footprints = new Dictionary<BlockMover, FootprintData>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        footprints.Clear();
    }

    public static void Clear()
    {
        footprints.Clear();
    }

    public static void Unregister(BlockMover block)
    {
        if (block != null)
        {
            footprints.Remove(block);
        }
    }

    public static void Register(BlockMover block, int startX, int startZ, int cellsX, int cellsZ)
    {
        if (block == null)
        {
            return;
        }

        footprints[block] = new FootprintData
        {
            StartX = startX,
            StartZ = startZ,
            CellsX = cellsX,
            CellsZ = cellsZ
        };
    }

    public static bool CanPlace(int startX, int startZ, int cellsX, int cellsZ, BlockMover ignore)
    {
        foreach (KeyValuePair<BlockMover, FootprintData> entry in footprints)
        {
            BlockMover other = entry.Key;
            if (other == null || other == ignore || other.HasExited)
            {
                continue;
            }

            FootprintData otherFootprint = entry.Value;
            if (GridConfig.FootprintsOverlap(
                    startX, startZ, cellsX, cellsZ,
                    otherFootprint.StartX, otherFootprint.StartZ,
                    otherFootprint.CellsX, otherFootprint.CellsZ))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryGetFootprint(
        BlockMover block,
        out int startX,
        out int startZ,
        out int cellsX,
        out int cellsZ)
    {
        startX = 0;
        startZ = 0;
        cellsX = 1;
        cellsZ = 1;

        if (block == null || !footprints.TryGetValue(block, out FootprintData footprint))
        {
            return false;
        }

        startX = footprint.StartX;
        startZ = footprint.StartZ;
        cellsX = footprint.CellsX;
        cellsZ = footprint.CellsZ;
        return true;
    }
}
