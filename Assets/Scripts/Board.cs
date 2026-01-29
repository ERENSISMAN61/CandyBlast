using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// Main board/grid management system
/// Performance optimized with 2D array for O(1) access
/// Memory efficient: Uses object pooling
/// </summary>
public class Board : MonoBehaviour
{
    [Title("Board Settings")]
    [SerializeField, Range(2, 10)] private int rows = 10;
    [SerializeField, Range(2, 10)] private int columns = 12;
    [SerializeField, Range(1, 6)] private int colorCount = 6;

    [Title("Spacing")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float spacing = 0.1f;

    [Title("References")]
    [SerializeField] private BlockPool blockPool;
    [SerializeField] private SpriteManager spriteManager;

    [Title("Animation")]
    [SerializeField] private float fallDelay = 0.05f;

    // Grid data structure - O(1) access time
    private Block[,] grid;
    private GroupDetector groupDetector;

    // Reusable collections to avoid GC - allocated once, reused via Clear()
    private List<Block> shuffleBlocksList = new List<Block>();
    private List<Vector2Int> shufflePositionsList = new List<Vector2Int>();
    private Dictionary<BlockType, List<Vector2Int>> blocksByTypeDict = new Dictionary<BlockType, List<Vector2Int>>();

    // Properties
    public int Rows => rows;
    public int Columns => columns;
    public int ColorCount => colorCount;
    public bool IsAnimating { get; private set; }
    public bool IsLevelActive { get; private set; } = true;

    private void Awake()
    {
        grid = new Block[columns, rows];
        groupDetector = new GroupDetector(this);
    }

    /// <summary>
    /// Initialize the board with random blocks
    /// </summary>
    public void InitializeBoard()
    {
        IsLevelActive = true;
        ClearBoard();
        StartCoroutine(InitializeBoardDelayed());
    }

    private IEnumerator InitializeBoardDelayed()
    {
        // wait three frame
        yield return null;
        yield return null;
        yield return null;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                CreateBlockAt(x, y);
            }
        }

        // Wait for blocks to be created, then check for deadlock
        yield return StartCoroutine(InitializeBoardWithDeadlockCheck());
    }

    /// <summary>
    /// Check for deadlock after initial board creation and fix if needed
    /// </summary>
    private IEnumerator InitializeBoardWithDeadlockCheck()
    {
        // Wait for all blocks to be created and positioned
        yield return new WaitForSeconds(0.8f);

        // Check if board has any valid groups
        if (!groupDetector.HasAnyValidGroups())
        {
            Debug.Log("Initial board created with deadlock - fixing...");
            ForceCreateValidGroup();

            // Wait for blocks to move to new positions
            yield return new WaitForSeconds(0.3f);
        }

        // Update icons after ensuring valid groups exist
        UpdateAllIcons();
    }

    /// <summary>
    /// Create a new block at grid position
    /// </summary>
    private void CreateBlockAt(int x, int y)
    {
        Block block = blockPool.GetBlock();
        BlockType randomType = GetRandomBlockType();
        Sprite sprite = spriteManager.GetDefaultSprite(randomType);

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorldPosition(gridPos);

        block.transform.position = worldPos + Vector3.up * (rows + 2); // Start from above
        block.Initialize(randomType, sprite, gridPos);

        grid[x, y] = block;

        // Animate fall
        block.MoveTo(gridPos, worldPos);
    }

    /// <summary>
    /// Get block at grid position
    /// O(1) access time
    /// </summary>
    public Block GetBlock(Vector2Int pos)
    {
        if (!IsValidPosition(pos))
            return null;

        return grid[pos.x, pos.y];
    }

    /// <summary>
    /// Get block at grid position
    /// </summary>
    public Block GetBlock(int x, int y)
    {
        return GetBlock(new Vector2Int(x, y));
    }

    /// <summary>
    /// Check if position is within board bounds
    /// </summary>
    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }

    /// <summary>
    /// Convert grid position to world position
    /// </summary>
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float totalWidth = columns * (cellSize + spacing);
        float totalHeight = rows * (cellSize + spacing);

        float x = gridPos.x * (cellSize + spacing) - totalWidth / 2f + cellSize / 2f;
        float y = gridPos.y * (cellSize + spacing) - totalHeight / 2f + cellSize / 2f;

        return new Vector3(x, y, 0) + transform.position;
    }

    /// <summary>
    /// Convert world position to grid position
    /// </summary>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        float totalWidth = columns * (cellSize + spacing);
        float totalHeight = rows * (cellSize + spacing);

        int x = Mathf.RoundToInt((localPos.x + totalWidth / 2f - cellSize / 2f) / (cellSize + spacing));
        int y = Mathf.RoundToInt((localPos.y + totalHeight / 2f - cellSize / 2f) / (cellSize + spacing));

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Blast a group of blocks
    /// Returns number of blocks blasted
    /// </summary>
    public int BlastGroup(List<Vector2Int> group)
    {
        if (group == null || group.Count < 2)
            return 0;

        IsAnimating = true;
        int count = group.Count;

        // Blast all blocks in group
        foreach (var pos in group)
        {
            Block block = GetBlock(pos);
            if (block != null)
            {
                block.Blast(() => blockPool.ReturnBlock(block));
                grid[pos.x, pos.y] = null;
            }
        }

        if (EventManager.Instance != null)
            EventManager.Instance.TriggerBlocksBlasted(count);

        // Apply gravity and fill after blast
        StartCoroutine(ApplyGravityAndFill());

        return count;
    }

    /// <summary>
    /// Apply gravity: Move blocks down to fill empty spaces
    /// CPU optimized: Processes column by column
    /// </summary>
    private IEnumerator ApplyGravityAndFill()
    {
        yield return new WaitForSeconds(0.3f); // Wait for blast animation

        bool hasMovement = true;

        while (hasMovement)
        {
            hasMovement = false;

            // Process each column bottom to top
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows - 1; y++)
                {
                    // If current cell is empty
                    if (grid[x, y] == null)
                    {
                        // Look for block above
                        for (int yAbove = y + 1; yAbove < rows; yAbove++)
                        {
                            if (grid[x, yAbove] != null)
                            {
                                // Move block down
                                Block block = grid[x, yAbove];
                                Vector2Int newPos = new Vector2Int(x, y);
                                Vector3 worldPos = GridToWorldPosition(newPos);

                                block.MoveTo(newPos, worldPos);

                                grid[x, y] = block;
                                grid[x, yAbove] = null;

                                hasMovement = true;
                                break;
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(fallDelay);
        }

        // Fill empty spaces with new blocks
        yield return StartCoroutine(FillEmptySpaces());
    }

    /// <summary>
    /// Fill empty spaces with new blocks from top
    /// </summary>
    private IEnumerator FillEmptySpaces()
    {
        // Don't fill if level is no longer active (win/fail)
        if (!IsLevelActive)
        {
            IsAnimating = false;
            yield break;
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] == null)
                {
                    CreateBlockAt(x, y);
                    yield return new WaitForSeconds(fallDelay);
                }
            }
        }

        // Wait for all blocks to finish moving
        yield return new WaitForSeconds(0.5f);

        // Update icons
        UpdateAllIcons();

        // Check for cascade (auto-blast chains)
        yield return StartCoroutine(CheckForCascade());
    }

    /// <summary>
    /// Check for automatic cascade matches
    /// </summary>
    private IEnumerator CheckForCascade()
    {
        var allGroups = groupDetector.FindAllGroups();

        // Find largest group for auto-blast (optional feature)
        // For now, just check for deadlock

        IsAnimating = false;
        if (EventManager.Instance != null)
            EventManager.Instance.TriggerBoardStable();

        // Check for deadlock
        if (!groupDetector.HasAnyValidGroups())
        {
            if (EventManager.Instance != null)
                EventManager.Instance.TriggerDeadlock();
        }

        yield return null;
    }

    /// <summary>
    /// Get random block type based on color count
    /// </summary>
    private BlockType GetRandomBlockType()
    {
        int randomIndex = Random.Range(0, colorCount);
        return (BlockType)randomIndex;
    }

    /// <summary>
    /// Update all block icons based on current groups
    /// </summary>
    public void UpdateAllIcons()
    {
        if (LevelManager.Instance != null)
        {
            groupDetector.UpdateAllGroupIcons(
                LevelManager.Instance.ThresholdA,
                LevelManager.Instance.ThresholdB,
                LevelManager.Instance.ThresholdC,
                spriteManager
            );
        }
    }

    private IEnumerator UpdateIconsAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateAllIcons();
    }

    /// <summary>
    /// Get group of blocks at position
    /// </summary>
    public List<Vector2Int> GetGroupAt(Vector2Int pos)
    {
        return groupDetector.FindGroup(pos);
    }

    /// <summary>
    /// Highlight a group of blocks
    /// </summary>
    public void HighlightGroup(List<Vector2Int> group, bool highlight)
    {
        foreach (var pos in group)
        {
            Block block = GetBlock(pos);
            if (block != null)
            {
                block.SetHighlight(highlight);
            }
        }
    }

    /// <summary>
    /// Smart shuffle algorithm with guaranteed valid group
    /// Uses deterministic approach - no blind retry loops
    /// Algorithm:
    /// 1. Shuffle all blocks randomly (Fisher-Yates)
    /// 2. Place blocks on grid
    /// 3. If no valid groups exist, force create one by strategic swap
    /// </summary>
    public void ShuffleBoard()
    {
        IsAnimating = true;

        // Reuse existing lists - avoid GC
        shuffleBlocksList.Clear();
        shufflePositionsList.Clear();

        // Collect all blocks and positions
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Block block = GetBlock(pos);

                if (block != null)
                {
                    shuffleBlocksList.Add(block);
                    shufflePositionsList.Add(pos);

                    // Reset to default icon before shuffle
                    Sprite defaultSprite = spriteManager.GetDefaultSprite(block.BlockType);
                    block.UpdateIcon(1, IconVariant.Default, defaultSprite);
                }
            }
        }

        // Fisher-Yates shuffle - single pass, O(n)
        for (int i = shuffleBlocksList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Block temp = shuffleBlocksList[i];
            shuffleBlocksList[i] = shuffleBlocksList[j];
            shuffleBlocksList[j] = temp;
        }

        // Place blocks in shuffled positions
        for (int i = 0; i < shuffleBlocksList.Count; i++)
        {
            Vector2Int pos = shufflePositionsList[i];
            Block block = shuffleBlocksList[i];

            grid[pos.x, pos.y] = block;
            block.GridPosition = pos;

            Vector3 worldPos = GridToWorldPosition(pos);
            block.MoveTo(pos, worldPos);
        }

        // Guarantee at least one valid group exists
        if (!groupDetector.HasAnyValidGroups())
        {
            ForceCreateValidGroup();
            Debug.Log("Forced valid group creation after shuffle");
        }

        StartCoroutine(FinishShuffle());
    }

    /// <summary>
    /// Deterministically creates at least one valid group on the board
    /// Strategy: Find blocks of same color and swap them to be adjacent
    /// This guarantees a solvable board without random retries
    /// </summary>
    private void ForceCreateValidGroup()
    {
        // Group blocks by type - reuse dictionary to avoid GC
        blocksByTypeDict.Clear();
        foreach (var list in blocksByTypeDict.Values)
        {
            list.Clear();
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Block block = GetBlock(pos);

                if (block != null)
                {
                    if (!blocksByTypeDict.ContainsKey(block.BlockType))
                        blocksByTypeDict[block.BlockType] = new List<Vector2Int>();

                    blocksByTypeDict[block.BlockType].Add(pos);
                }
            }
        }

        // Find a color with at least 2 blocks
        foreach (var kvp in blocksByTypeDict)
        {
            if (kvp.Value.Count >= 2)
            {
                // Get two positions of this color
                Vector2Int pos1 = kvp.Value[0];
                Vector2Int pos2 = kvp.Value[1];

                // Find two adjacent positions on the board
                Vector2Int adjacentPos1 = Vector2Int.zero;
                Vector2Int adjacentPos2 = Vector2Int.zero;
                bool foundAdjacent = false;

                // Try to find horizontal adjacency
                for (int y = 0; y < rows && !foundAdjacent; y++)
                {
                    for (int x = 0; x < columns - 1 && !foundAdjacent; x++)
                    {
                        adjacentPos1 = new Vector2Int(x, y);
                        adjacentPos2 = new Vector2Int(x + 1, y);
                        foundAdjacent = true;
                    }
                }

                // If no horizontal found, try vertical
                if (!foundAdjacent)
                {
                    for (int x = 0; x < columns && !foundAdjacent; x++)
                    {
                        for (int y = 0; y < rows - 1 && !foundAdjacent; y++)
                        {
                            adjacentPos1 = new Vector2Int(x, y);
                            adjacentPos2 = new Vector2Int(x, y + 1);
                            foundAdjacent = true;
                        }
                    }
                }

                if (foundAdjacent)
                {
                    // Swap blocks to create adjacent group
                    Block block1 = GetBlock(pos1);
                    Block block2 = GetBlock(pos2);
                    Block targetBlock1 = GetBlock(adjacentPos1);
                    Block targetBlock2 = GetBlock(adjacentPos2);

                    // Swap in grid
                    grid[pos1.x, pos1.y] = targetBlock1;
                    grid[pos2.x, pos2.y] = targetBlock2;
                    grid[adjacentPos1.x, adjacentPos1.y] = block1;
                    grid[adjacentPos2.x, adjacentPos2.y] = block2;

                    // Update block positions and move them
                    if (targetBlock1 != null)
                    {
                        targetBlock1.GridPosition = pos1;
                        targetBlock1.MoveTo(pos1, GridToWorldPosition(pos1));
                    }
                    if (targetBlock2 != null)
                    {
                        targetBlock2.GridPosition = pos2;
                        targetBlock2.MoveTo(pos2, GridToWorldPosition(pos2));
                    }

                    block1.GridPosition = adjacentPos1;
                    block1.MoveTo(adjacentPos1, GridToWorldPosition(adjacentPos1));

                    block2.GridPosition = adjacentPos2;
                    block2.MoveTo(adjacentPos2, GridToWorldPosition(adjacentPos2));

                    return; // Successfully created a valid group
                }
            }
        }
    }

    private IEnumerator FinishShuffle()
    {
        // Wait for blocks to finish moving
        yield return new WaitForSeconds(0.8f);

        // Update all icons based on new groups
        UpdateAllIcons();

        Debug.Log("Shuffle complete - Icons updated");

        IsAnimating = false;
        if (EventManager.Instance != null)
            EventManager.Instance.TriggerBoardStable();
    }

    /// <summary>
    /// Clear all blocks from board
    /// </summary>
    public void ClearBoard()
    {

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Block block = grid[x, y];
                if (block != null)
                {
                    blockPool.ReturnBlock(block);
                    grid[x, y] = null;
                }
            }
        }
    }

    /// <summary>
    /// Stop level activity - prevents new blocks from spawning
    /// </summary>
    public void StopLevel()
    {
        IsLevelActive = false;
        IsAnimating = false;
    }

    /// <summary>
    /// Set board parameters (for level configuration)
    /// </summary>
    public void SetBoardParameters(int newRows, int newColumns, int newColorCount)
    {
        rows = Mathf.Clamp(newRows, 2, 10);
        columns = Mathf.Clamp(newColumns, 2, 10);
        colorCount = Mathf.Clamp(newColorCount, 1, 6);

        grid = new Block[columns, rows];
        groupDetector = new GroupDetector(this);
    }

#if UNITY_EDITOR
    [Button("Initialize Board")]
    private void EditorInitializeBoard()
    {
        if (!Application.isPlaying) return;
        InitializeBoard();
    }

    [Button("Shuffle Board")]
    private void EditorShuffleBoard()
    {
        if (!Application.isPlaying) return;
        ShuffleBoard();
    }
#endif
}
