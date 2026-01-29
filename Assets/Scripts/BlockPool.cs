using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pooling system for blocks
/// Memory optimization: Reuses blocks instead of instantiate/destroy
/// CPU optimization: Reduces GC pressure
/// </summary>
public class BlockPool : MonoBehaviour
{
    [SerializeField] private Block blockPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private Transform poolParent;

    private Queue<Block> availableBlocks = new Queue<Block>();
    private List<Block> activeBlocks = new List<Block>();

    // Reusable list to avoid GC during ClearAll
    private List<Block> tempBlocksList = new List<Block>();

    private void Awake()
    {
        if (poolParent == null)
            poolParent = transform;

        // Pre-warm pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBlock();
        }
    }

    /// <summary>
    /// Get block from pool or create new one
    /// </summary>
    public Block GetBlock()
    {
        Block block;

        if (availableBlocks.Count > 0)
        {
            block = availableBlocks.Dequeue();
        }
        else
        {
            block = CreateNewBlock();
        }

        block.gameObject.SetActive(true);
        activeBlocks.Add(block);
        return block;
    }

    /// <summary>
    /// Return block to pool
    /// </summary>
    public void ReturnBlock(Block block)
    {
        if (block == null) return;

        // Reset before deactivating to ensure clean state
        block.ResetBlock();

        // Reset transform to default position
        block.transform.localPosition = Vector3.zero;
        block.transform.localScale = Vector3.one;

        block.gameObject.SetActive(false);
        block.transform.SetParent(poolParent);

        activeBlocks.Remove(block);
        availableBlocks.Enqueue(block);
    }

    /// <summary>
    /// Return multiple blocks at once
    /// </summary>
    public void ReturnBlocks(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            ReturnBlock(block);
        }
    }

    /// <summary>
    /// Clear all active blocks
    /// </summary>
    public void ClearAll()
    {
        // Reuse temp list to avoid allocation
        tempBlocksList.Clear();
        tempBlocksList.AddRange(activeBlocks);

        foreach (var block in tempBlocksList)
        {
            ReturnBlock(block);
        }
    }

    private Block CreateNewBlock()
    {
        Block block = Instantiate(blockPrefab, poolParent);
        block.gameObject.SetActive(false);
        availableBlocks.Enqueue(block);
        return block;
    }

    /// <summary>
    /// Get pool statistics for debugging
    /// </summary>
    public string GetPoolStats()
    {
        return $"Active: {activeBlocks.Count}, Available: {availableBlocks.Count}, Total: {activeBlocks.Count + availableBlocks.Count}";
    }
}
