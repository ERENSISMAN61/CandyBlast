using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Handles player input for clicking/tapping blocks
/// Performance optimized: Uses raycasts efficiently
/// </summary>
public class InputManager : MonoBehaviour
{
    [SerializeField] private Board board;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask blockLayer;

    private List<Vector2Int> currentHighlightedGroup;
    private bool isProcessingInput = false;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (board == null)
        {
            Debug.LogWarning("Board reference missing in InputManager!");
            return;
        }
        
        // Don't allow input if board is animating, processing, or level is not active
        if (board.IsAnimating || isProcessingInput || !board.IsLevelActive)
            return;

        HandleInput();
    }

    /// <summary>
    /// Handle mouse/touch input
    /// </summary>
    private void HandleInput()
    {
        // Mouse/Touch down - show preview
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            Vector2Int gridPos = board.WorldToGridPosition(worldPos);

            if (board.IsValidPosition(gridPos))
            {
                var group = board.GetGroupAt(gridPos);

                if (group != null && group.Count >= 2) // Minimum group size
                {
                    currentHighlightedGroup = group;
                    board.HighlightGroup(group, true);
                }
            }
        }

        // Mouse/Touch up - blast group
        if (Input.GetMouseButtonUp(0))
        {
            if (currentHighlightedGroup != null && currentHighlightedGroup.Count >= 2)
            {
                board.HighlightGroup(currentHighlightedGroup, false);
                board.BlastGroup(currentHighlightedGroup);
                isProcessingInput = true;

                // Reset after board stabilizes
                Invoke(nameof(ResetInputProcessing), 0.5f);
            }

            currentHighlightedGroup = null;
        }

        // Mouse moved - update preview
        if (Input.GetMouseButton(0) && currentHighlightedGroup != null)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0;

            Vector2Int gridPos = board.WorldToGridPosition(worldPos);

            if (board.IsValidPosition(gridPos))
            {
                var newGroup = board.GetGroupAt(gridPos);

                if (newGroup != null && newGroup.Count >= 2)
                {
                    // If different group, update highlight
                    if (!GroupsAreEqual(currentHighlightedGroup, newGroup))
                    {
                        board.HighlightGroup(currentHighlightedGroup, false);
                        currentHighlightedGroup = newGroup;
                        board.HighlightGroup(currentHighlightedGroup, true);
                    }
                }
            }
        }

        // Cancel on right click or escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentHighlightedGroup != null)
            {
                board.HighlightGroup(currentHighlightedGroup, false);
                currentHighlightedGroup = null;
            }
        }
    }

    private void ResetInputProcessing()
    {
        isProcessingInput = false;
    }

    /// <summary>
    /// Compare two groups for equality
    /// </summary>
    private bool GroupsAreEqual(List<Vector2Int> group1, List<Vector2Int> group2)
    {
        if (group1.Count != group2.Count)
            return false;

        foreach (var pos in group1)
        {
            if (!group2.Contains(pos))
                return false;
        }

        return true;
    }
}
