using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Detects groups of connected blocks with same color
/// Performance optimized: Uses flood fill algorithm with visited array
/// CPU: O(M*N) worst case, typically much faster
/// Memory: Reuses HashSet to avoid allocations
/// </summary>
public class GroupDetector
{
    private Board board;
    private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    private List<Vector2Int> currentGroup = new List<Vector2Int>();

    // Directions for flood fill (up, down, left, right)
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

    /// <summary>
    /// Find all blocks in the same group as the clicked block
    /// Uses flood fill algorithm for connected component detection
    /// </summary>
    public List<Vector2Int> FindGroup(Vector2Int startPos)
    {
        currentGroup.Clear();
        visited.Clear();

        Block startBlock = board.GetBlock(startPos);
        if (startBlock == null)
            return currentGroup;

        BlockType targetType = startBlock.BlockType;

        // Flood fill
        FloodFill(startPos, targetType);

        return new List<Vector2Int>(currentGroup);
    }

    /// <summary>
    /// Recursive flood fill to find connected blocks
    /// </summary>
    private void FloodFill(Vector2Int pos, BlockType targetType)
    {
        // Boundary checks
        if (!board.IsValidPosition(pos))
            return;

        // Already visited
        if (visited.Contains(pos))
            return;

        // Get block at position
        Block block = board.GetBlock(pos);
        if (block == null || block.BlockType != targetType)
            return;

        // Mark as visited and add to group
        visited.Add(pos);
        currentGroup.Add(pos);

        // Check all adjacent blocks
        foreach (var dir in directions)
        {
            FloodFill(pos + dir, targetType);
        }
    }

    /// <summary>
    /// Find all groups on the board
    /// Used for deadlock detection
    /// Returns: Dictionary of group size -> list of groups
    /// </summary>
    public Dictionary<int, List<List<Vector2Int>>> FindAllGroups()
    {
        var allGroups = new Dictionary<int, List<List<Vector2Int>>>();
        var globalVisited = new HashSet<Vector2Int>();

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

                // Find group starting from this position
                currentGroup.Clear();
                visited.Clear();
                FloodFill(pos, block.BlockType);

                if (currentGroup.Count >= 2) // Minimum group size
                {
                    int size = currentGroup.Count;
                    if (!allGroups.ContainsKey(size))
                        allGroups[size] = new List<List<Vector2Int>>();

                    allGroups[size].Add(new List<Vector2Int>(currentGroup));
                }

                // Mark all blocks in this group as globally visited
                foreach (var groupPos in currentGroup)
                {
                    globalVisited.Add(groupPos);
                }
            }
        }

        return allGroups;
    }

    /// <summary>
    /// Update icon variants for all blocks in groups
    /// Performance: Updates all blocks - groups get special icons, singles get default
    /// </summary>
    public void UpdateAllGroupIcons(int thresholdA, int thresholdB, int thresholdC, SpriteManager spriteManager)
    {
        var allGroups = FindAllGroups();
        var processedBlocks = new HashSet<Vector2Int>();

        // Update blocks that are in groups
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

        // Reset single blocks (not in any group) to default icon
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

    /// <summary>
    /// Check if there are any valid groups (size >= 2) on the board
    /// Used for deadlock detection
    /// </summary>
    public bool HasAnyValidGroups()
    {
        var globalVisited = new HashSet<Vector2Int>();

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

                // Find group
                currentGroup.Clear();
                visited.Clear();
                FloodFill(pos, block.BlockType);

                // If we find any group with 2+ blocks, board is not deadlocked
                if (currentGroup.Count >= 2)
                    return true;

                // Mark as visited
                foreach (var groupPos in currentGroup)
                {
                    globalVisited.Add(groupPos);
                }
            }
        }

        return false;
    }
}
