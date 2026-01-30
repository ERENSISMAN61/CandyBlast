using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Sirenix.OdinInspector;
using DG.Tweening;

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


    private Block[,] grid;
    private GroupDetector groupDetector;


    private List<Block> shuffleBlocksList = new List<Block>();
    private List<Vector2Int> shufflePositionsList = new List<Vector2Int>();
    private Dictionary<BlockType, List<Vector2Int>> blocksByTypeDict = new Dictionary<BlockType, List<Vector2Int>>();

    // properties
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

        // wait for blocks to be created, then check for deadlock
        yield return StartCoroutine(InitializeBoardWithDeadlockCheck());
    }

    private IEnumerator InitializeBoardWithDeadlockCheck()
    {
        // wait for all blocks to be created and positioned
        yield return new WaitForSeconds(0.8f);

        // check if board has any valid groups
        if (!groupDetector.HasAnyValidGroups())
        {
            Debug.Log("Initial board created with deadlock - fixing...");
            ForceCreateValidGroup();

            // wait for blocks to move to new positions
            yield return new WaitForSeconds(0.3f);
        }

        // update icons after ensuring valid groups exist
        UpdateAllIcons();
    }

    private void CreateBlockAt(int x, int y)
    {
        Block block = blockPool.GetBlock();
        BlockType randomType = GetRandomBlockType();
        Sprite sprite = spriteManager.GetDefaultSprite(randomType);

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorldPosition(gridPos);

        block.transform.position = worldPos + Vector3.up * (rows + 2); // start from above
        block.Initialize(randomType, sprite, gridPos);

        grid[x, y] = block;

        // animate fall
        block.MoveTo(gridPos, worldPos);
    }

    public Block GetBlock(Vector2Int pos)
    {
        if (!IsValidPosition(pos))
            return null;

        return grid[pos.x, pos.y];
    }

    public Block GetBlock(int x, int y)
    {
        return GetBlock(new Vector2Int(x, y));
    }

    public bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < columns && pos.y >= 0 && pos.y < rows;
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float totalWidth = columns * (cellSize + spacing);
        float totalHeight = rows * (cellSize + spacing);

        float x = gridPos.x * (cellSize + spacing) - totalWidth / 2f + cellSize / 2f;
        float y = gridPos.y * (cellSize + spacing) - totalHeight / 2f + cellSize / 2f;

        return new Vector3(x, y, 0) + transform.position;
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;

        float totalWidth = columns * (cellSize + spacing);
        float totalHeight = rows * (cellSize + spacing);

        int x = Mathf.RoundToInt((localPos.x + totalWidth / 2f - cellSize / 2f) / (cellSize + spacing));
        int y = Mathf.RoundToInt((localPos.y + totalHeight / 2f - cellSize / 2f) / (cellSize + spacing));

        return new Vector2Int(x, y);
    }

    public int BlastGroup(List<Vector2Int> group)
    {
        if (group == null || group.Count < 2)
            return 0;

        IsAnimating = true;
        int count = group.Count;

        // blast all blocks in group
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

        // apply gravity and fill after blast
        StartCoroutine(ApplyGravityAndFill());

        return count;
    }

    private IEnumerator ApplyGravityAndFill()
    {
        yield return new WaitForSeconds(0.3f); // wait for blast animation

        bool hasMovement = true;

        while (hasMovement)
        {
            hasMovement = false;

            // process each column bottom to top
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows - 1; y++)
                {
                    // if current cell is empty
                    if (grid[x, y] == null)
                    {
                        // look for block above
                        for (int yAbove = y + 1; yAbove < rows; yAbove++)
                        {
                            if (grid[x, yAbove] != null)
                            {
                                // move block down
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

        // fill empty spaces with new blocks
        yield return StartCoroutine(FillEmptySpaces());
    }

    private IEnumerator FillEmptySpaces()
    {
        // don't fill if level is no longer active (win/fail)
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

        // wait for all blocks to finish moving
        yield return new WaitForSeconds(0.5f);

        // update icons
        UpdateAllIcons();

        // check for cascade (auto-blast chains)
        yield return StartCoroutine(CheckForCascade());
    }

    private IEnumerator CheckForCascade()
    {
        var allGroups = groupDetector.FindAllGroups();

        // find largest group for auto-blast
        // for now, just check for deadlock

        IsAnimating = false;
        if (EventManager.Instance != null)
            EventManager.Instance.TriggerBoardStable();

        // check for deadlock
        if (!groupDetector.HasAnyValidGroups())
        {
            if (EventManager.Instance != null)
                EventManager.Instance.TriggerDeadlock();
        }

        yield return null;
    }

    private BlockType GetRandomBlockType()
    {
        int randomIndex = Random.Range(0, colorCount);
        return (BlockType)randomIndex;
    }

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

    public List<Vector2Int> GetGroupAt(Vector2Int pos)
    {
        return groupDetector.FindGroup(pos);
    }

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

    public void ShuffleBoard()
    {
        IsAnimating = true;

        // reuse existing lists 
        shuffleBlocksList.Clear();
        shufflePositionsList.Clear();

        // collect all blocks and positions
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

                    // reset to default icon before shuffle
                    Sprite defaultSprite = spriteManager.GetDefaultSprite(block.BlockType);
                    block.UpdateIcon(1, IconVariant.Default, defaultSprite);
                }
            }
        }

        // fisher-Yates shuffle
        for (int i = shuffleBlocksList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Block temp = shuffleBlocksList[i];
            shuffleBlocksList[i] = shuffleBlocksList[j];
            shuffleBlocksList[j] = temp;
        }

        // place blocks in shuffled positions
        for (int i = 0; i < shuffleBlocksList.Count; i++)
        {
            Vector2Int pos = shufflePositionsList[i];
            Block block = shuffleBlocksList[i];

            grid[pos.x, pos.y] = block;
            block.GridPosition = pos;

            Vector3 worldPos = GridToWorldPosition(pos);
            block.MoveTo(pos, worldPos);
        }

        // guarantee at least one valid group exists
        if (!groupDetector.HasAnyValidGroups())
        {
            ForceCreateValidGroup();
            Debug.Log("Forced valid group creation after shuffle");
        }

        StartCoroutine(FinishShuffle());
    }

    private void ForceCreateValidGroup()
    {
        // group blocks by type 
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

        // find a color with at least 2 blocks
        foreach (var kvp in blocksByTypeDict)
        {
            if (kvp.Value.Count >= 2)
            {
                // get two positions of this color
                Vector2Int pos1 = kvp.Value[0];
                Vector2Int pos2 = kvp.Value[1];

                // find two adjacent positions on the board
                Vector2Int adjacentPos1 = Vector2Int.zero;
                Vector2Int adjacentPos2 = Vector2Int.zero;
                bool foundAdjacent = false;

                // try to find horizontal adjacency
                for (int y = 0; y < rows && !foundAdjacent; y++)
                {
                    for (int x = 0; x < columns - 1 && !foundAdjacent; x++)
                    {
                        adjacentPos1 = new Vector2Int(x, y);
                        adjacentPos2 = new Vector2Int(x + 1, y);
                        foundAdjacent = true;
                    }
                }

                // if no horizontal found, try vertical
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
                    // swap blocks to create adjacent group
                    Block block1 = GetBlock(pos1);
                    Block block2 = GetBlock(pos2);
                    Block targetBlock1 = GetBlock(adjacentPos1);
                    Block targetBlock2 = GetBlock(adjacentPos2);

                    // swap in grid
                    grid[pos1.x, pos1.y] = targetBlock1;
                    grid[pos2.x, pos2.y] = targetBlock2;
                    grid[adjacentPos1.x, adjacentPos1.y] = block1;
                    grid[adjacentPos2.x, adjacentPos2.y] = block2;

                    // update block positions and move them
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

                    return; // successfully created a valid group
                }
            }
        }
    }

    private IEnumerator FinishShuffle()
    {
        // wait for blocks to finish moving
        yield return new WaitForSeconds(0.8f);

        // update all icons based on new groups
        UpdateAllIcons();

        Debug.Log("Shuffle complete - Icons updated");

        IsAnimating = false;
        if (EventManager.Instance != null)
            EventManager.Instance.TriggerBoardStable();
    }

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

    public void StopLevel()
    {
        IsLevelActive = false;
        IsAnimating = false;
    }

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
