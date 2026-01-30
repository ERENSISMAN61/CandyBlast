using UnityEngine;
using DG.Tweening;

public class Block : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem blastParticle;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.2f;

    // block data
    public BlockType BlockType { get; private set; }
    public IconVariant CurrentVariant { get; private set; }
    public Vector2Int GridPosition { get; set; }
    public int GroupSize { get; private set; }

    // state
    public bool IsMoving { get; private set; }
    private Tween moveTween;
    private Tween scaleTween;

    private Color highlightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        scaleTween?.Kill();
        transform.DOKill();
    }
    private void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            // y càng cao, sorting order càng yüksek (üstteki bloklar önde)
            spriteRenderer.sortingOrder = GridPosition.y;
        }
    }

    public void Initialize(BlockType type, Sprite sprite, Vector2Int gridPos)
    {
        BlockType = type;
        GridPosition = gridPos;
        CurrentVariant = IconVariant.Default;
        GroupSize = 1;

        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;

        // set sorting order based on grid position
        UpdateSortingOrder();

        // start with small scale for pop-in effect during fall
        transform.localScale = Vector3.one * 0.8f;
    }

    public void UpdateIcon(int groupSize, IconVariant variant, Sprite newSprite)
    {
        if (GroupSize == groupSize && CurrentVariant == variant)
            return; // no change needed

        GroupSize = groupSize;
        CurrentVariant = variant;

        if (spriteRenderer != null && newSprite != null)
        {
            // quick scale animation for feedback

            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
            spriteRenderer.sprite = newSprite;
        }
    }

    public void MoveTo(Vector2Int newGridPos, Vector3 worldPos, System.Action onComplete = null)
    {
        GridPosition = newGridPos;
        IsMoving = true;

        // update sorting order for new position
        UpdateSortingOrder();

        moveTween?.Kill();
        scaleTween?.Kill();

        // move and scale to full size simultaneously for smooth spawn
        moveTween = transform.DOMove(worldPos, moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                IsMoving = false;
                onComplete?.Invoke();
            });

        // scale to full size while moving (only if not already full scale)
        if (transform.localScale != Vector3.one)
        {
            scaleTween = transform.DOScale(Vector3.one, moveDuration * 0.7f)
                .SetEase(Ease.OutBack);
        }
    }

    public void Blast(System.Action onComplete = null)
    {
        // kill any ongoing animations
        moveTween?.Kill();
        scaleTween?.Kill();

        // play particle effect
        if (blastParticle != null)
        {
            blastParticle.Play();
        }

        // scale down animation
        scaleTween = transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                // return to pool (will be implemented in BlockPool)
                gameObject.SetActive(false);
            });
    }

    public void SetHighlight(bool highlighted)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlighted ? highlightColor : Color.white;
        }
    }

    public void ResetBlock()
    {
        // kill all tweens immediately
        transform.DOKill();
        moveTween?.Kill();
        scaleTween?.Kill();

        IsMoving = false;
        GroupSize = 1;
        CurrentVariant = IconVariant.Default;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        // reset transform completely
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        moveTween?.Kill();
        scaleTween?.Kill();
    }
}
