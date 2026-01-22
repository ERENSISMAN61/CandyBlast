using UnityEngine;
using DG.Tweening;

/// <summary>
/// Represents a single block in the grid
/// Memory efficient: Uses struct-like data, object pooling ready
/// </summary>
public class Block : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem blastParticle;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.2f;

    // Block data
    public BlockType BlockType { get; private set; }
    public IconVariant CurrentVariant { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public int GroupSize { get; private set; }

    // State
    public bool IsMoving { get; private set; }
    private Tween moveTween;
    private Tween scaleTween;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Update sorting order based on grid position
    /// Higher Y = Higher sorting order (drawn on top)
    /// Performance: Only changes render order, not transform
    /// </summary>
    private void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            // Y càng cao, sorting order càng yüksek (üstteki bloklar önde)
            spriteRenderer.sortingOrder = GridPosition.y;
        }
    }

    /// <summary>
    /// Initialize block with type and sprite
    /// </summary>
    public void Initialize(BlockType type, Sprite sprite, Vector2Int gridPos)
    {
        BlockType = type;
        GridPosition = gridPos;
        CurrentVariant = IconVariant.Default;
        GroupSize = 1;

        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;

        // Set sorting order based on grid position
        UpdateSortingOrder();

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Update block icon based on group size
    /// Performance: Only updates if variant actually changes
    /// </summary>
    public void UpdateIcon(int groupSize, IconVariant variant, Sprite newSprite)
    {
        if (GroupSize == groupSize && CurrentVariant == variant)
            return; // No change needed

        GroupSize = groupSize;
        CurrentVariant = variant;

        if (spriteRenderer != null && newSprite != null)
        {
            // Quick scale animation for feedback
            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
            spriteRenderer.sprite = newSprite;
        }
    }

    /// <summary>
    /// Move block to new grid position
    /// GPU optimized: Uses DOTween for smooth interpolation
    /// </summary>
    public void MoveTo(Vector2Int newGridPos, Vector3 worldPos, System.Action onComplete = null)
    {
        GridPosition = newGridPos;
        IsMoving = true;

        // Update sorting order for new position
        UpdateSortingOrder();

        moveTween?.Kill();
        moveTween = transform.DOMove(worldPos, moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                IsMoving = false;
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// Blast animation and destruction
    /// Memory efficient: Returns to pool instead of destroying
    /// </summary>
    public void Blast(System.Action onComplete = null)
    {
        // Kill any ongoing animations
        moveTween?.Kill();
        scaleTween?.Kill();

        // Play particle effect
        if (blastParticle != null)
        {
            blastParticle.Play();
        }

        // Scale down animation
        scaleTween = transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                // Return to pool (will be implemented in BlockPool)
                gameObject.SetActive(false);
            });
    }

    /// <summary>
    /// Highlight block when part of selected group
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlighted ? Color.yellow : Color.white;
        }
    }

    /// <summary>
    /// Reset block state for pooling
    /// </summary>
    public void ResetBlock()
    {
        moveTween?.Kill();
        scaleTween?.Kill();

        IsMoving = false;
        GroupSize = 1;
        CurrentVariant = IconVariant.Default;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        scaleTween?.Kill();
    }
}
