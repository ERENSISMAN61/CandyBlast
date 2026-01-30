using UnityEngine;
using System.Collections.Generic;

public class GroupDetector
{
    private Board board;
    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private List<Vector2Int> currentGroup = new List<Vector2Int>();

    // reusable collections for FindAllGroups to avoid GC
    private Dictionary<int, List<List<Vector2Int>>> allGroupsDict = new Dictionary<int, List<List<Vector2Int>>>();
    private HashSet<Vector2Int> globalVisited = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> processedBlocks = new HashSet<Vector2Int>();

    // directions for flood fill (up, down, left, right)
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(1, 0)    // Right
    };

    public GroupDetector(Board board)
    {
        this.board = board;
    }

    public List<Vector2Int> FindGroup(Vector2Int startPos)
    {
        currentGroup.Clear();
        visited.Clear();

        Block startBlock = board.GetBlock(startPos);
        if (startBlock == null)
            return currentGroup;

        BlockType targetType = startBlock.BlockType;

        // flood fill
        FloodFill(startPos, targetType);

        return new List<Vector2Int>(currentGroup);
    }

    private void FloodFill(Vector2Int pos, BlockType targetType)
    {
        // boundary checks
        if (!board.IsValidPosition(pos))
            return;

        // already visited
        if (visited.Contains(pos))
            return;

        // get block at position
        Block block = board.GetBlock(pos);
        if (block == null || block.BlockType != targetType)
            return;

        // mark as visited and add to group
        visited.Add(pos);
        currentGroup.Add(pos);

        // check all adjacent blocks
        foreach (var dir in directions)
        {
            FloodFill(pos + dir, targetType);
        }
    }

    public Dictionary<int, List<List<Vector2Int>>> FindAllGroups()
    {
        // reuse collections - avoid GC
        allGroupsDict.Clear();
        globalVisited.Clear();

        for (int x = 0; x < board.Columns; x++)
        {
            for (int y = 0; y < board.Rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (globalVisited.Contains(pos))
                    continue;

                Block block = board.GetBlock(pos);
                if (block == null)
                    continue;

                // find group starting from this position
                currentGroup.Clear();
                visited.Clear();
                FloodFill(pos, block.BlockType);

                if (currentGroup.Count >= 2) // minimum group size
                {
                    int size = currentGroup.Count;
                    if (!allGroupsDict.ContainsKey(size))
                        allGroupsDict[size] = new List<List<Vector2Int>>();

                    allGroupsDict[size].Add(new List<Vector2Int>(currentGroup));
                }

                // mark all blocks in this group as globally visited
                foreach (var groupPos in currentGroup)
                {
                    globalVisited.Add(groupPos);
                }
            }
        }

        return allGroupsDict;
    }

    public void UpdateAllGroupIcons(int thresholdA, int thresholdB, int thresholdC, SpriteManager spriteManager)
    {
        var allGroups = FindAllGroups();
        processedBlocks.Clear(); // reuse hashset

        // update blocks that are in groups
        foreach (var groupSizePair in allGroups)
        {
            int groupSize = groupSizePair.Key;
            IconVariant variant = spriteManager.GetVariantForGroupSize(groupSize, thresholdA, thresholdB, thresholdC);

            foreach (var group in groupSizePair.Value)
            {
                foreach (var pos in group)
                {
                    Block block = board.GetBlock(pos);
                    if (block != null)
                    {
                        Sprite newSprite = spriteManager.GetSprite(block.BlockType, variant);
                        block.UpdateIcon(groupSize, variant, newSprite);
                        processedBlocks.Add(pos);
                    }
                }
            }
        }

        // reset single blocks (not in any group) to default icon
        for (int x = 0; x < board.Columns; x++)
        {
            for (int y = 0; y < board.Rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (!processedBlocks.Contains(pos))
                {
                    Block block = board.GetBlock(pos);
                    if (block != null)
                    {
                        Sprite defaultSprite = spriteManager.GetDefaultSprite(block.BlockType);
                        block.UpdateIcon(1, IconVariant.Default, defaultSprite);
                    }
                }
            }
        }
    }

    public bool HasAnyValidGroups()
    {
        globalVisited.Clear(); // reuse hashset

        for (int x = 0; x < board.Columns; x++)
        {
            for (int y = 0; y < board.Rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (globalVisited.Contains(pos))
                    continue;

                Block block = board.GetBlock(pos);
                if (block == null)
                    continue;

                // find group
                currentGroup.Clear();
                visited.Clear();
                FloodFill(pos, block.BlockType);

                // if we find any group with 2+ blocks, board is not deadlocked
                if (currentGroup.Count >= 2)
                    return true;

                // mark as visited
                foreach (var groupPos in currentGroup)
                {
                    globalVisited.Add(groupPos);
                }
            }
        }

        return false;
    }
}
