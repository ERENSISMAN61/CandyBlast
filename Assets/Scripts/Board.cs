using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;

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

    // Properties
    public int Rows => rows;
    public int Columns => columns;
    public int ColorCount => colorCount;
    public bool IsAnimating { get; private set; }

    // Events
    public System.Action<int> OnBlocksBlasted;
    public System.Action OnBoardStable;
    public System.Action OnDeadlock;

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
        ClearBoard();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                CreateBlockAt(x, y);
            }
        }

        // Update icons after initial creation
        StartCoroutine(UpdateIconsAfterDelay());
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

        OnBlocksBlasted?.Invoke(count);

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
        OnBoardStable?.Invoke();

        // Check for deadlock
        if (!groupDetector.HasAnyValidGroups())
        {
            OnDeadlock?.Invoke();
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
    /// Smart shuffle algorithm
    /// Ensures at least one valid group exists after shuffle
    /// </summary>
    public void ShuffleBoard()
    {
        IsAnimating = true;

        List<Block> allBlocks = new List<Block>();
        List<Vector2Int> allPositions = new List<Vector2Int>();

        // Collect all blocks and positions
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Block block = GetBlock(pos);

                if (block != null)
                {
                    allBlocks.Add(block);
                    allPositions.Add(pos);
                }
            }
        }

        // Shuffle until we have at least one valid group
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            // Fisher-Yates shuffle
            for (int i = allBlocks.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Block temp = allBlocks[i];
                allBlocks[i] = allBlocks[j];
                allBlocks[j] = temp;
            }

            // Place blocks in shuffled positions
            for (int i = 0; i < allBlocks.Count; i++)
            {
                Vector2Int pos = allPositions[i];
                Block block = allBlocks[i];

                grid[pos.x, pos.y] = block;
                block.GridPosition = pos;

                Vector3 worldPos = GridToWorldPosition(pos);
                block.MoveTo(pos, worldPos);
            }

            attempts++;

        } while (!groupDetector.HasAnyValidGroups() && attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("Could not find valid shuffle after " + maxAttempts + " attempts");
        }

        StartCoroutine(FinishShuffle());
    }

    private IEnumerator FinishShuffle()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateAllIcons();
        IsAnimating = false;
        OnBoardStable?.Invoke();
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
