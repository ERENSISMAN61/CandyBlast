using UnityEngine;
using System.Collections.Generic;

public class BlockPool : MonoBehaviour
{
    [SerializeField] private Block blockPrefab;
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private Transform poolParent;

    private Queue<Block> availableBlocks = new Queue<Block>();
    private List<Block> activeBlocks = new List<Block>();

    // reusable list to avoid GC during ClearAll
    private List<Block> tempBlocksList = new List<Block>();

    private void Awake()
    {
        if (poolParent == null)
            poolParent = transform;

        // pre-warm pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBlock();
        }
    }

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

    public void ReturnBlock(Block block)
    {
        if (block == null) return;

        // reset before deactivating to ensure clean state
        block.ResetBlock();

        // reset transform to default position
        block.transform.localPosition = Vector3.zero;
        block.transform.localScale = Vector3.one;

        block.gameObject.SetActive(false);
        block.transform.SetParent(poolParent);

        activeBlocks.Remove(block);
        availableBlocks.Enqueue(block);
    }

    public void ReturnBlocks(List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            ReturnBlock(block);
        }
    }

    public void ClearAll()
    {
        // reuse temp list to avoid allocation
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

    public string GetPoolStats()
    {
        return $"Active: {activeBlocks.Count}, Available: {availableBlocks.Count}, Total: {activeBlocks.Count + availableBlocks.Count}";
    }
}
