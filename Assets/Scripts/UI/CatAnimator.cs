using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CatAnimator : MonoBehaviour
{
    [SerializeField] private Image imageAnimator;

    [Header("Animation Sprites")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] winSprites;
    [SerializeField] private Sprite[] failSprites;

    [Header("Animation Settings")]
    [SerializeField] private float frameRate = 12f;
    [SerializeField] private bool loopAnimation = true;

    [Header("References")]
    [SerializeField] private EventManager eventManager;

    private Coroutine currentAnimation;

    private void OnEnable()
    {
        eventManager.OnLevelStart += PlayIdleAnimation;

    }

    private void OnDisable()
    {
        eventManager.OnLevelStart -= PlayIdleAnimation;

    }

    private void OnDestroy()
    {
        eventManager.OnLevelStart -= PlayIdleAnimation;

    }

    private void PlayIdleAnimation()
    {
        PlayIdleAnimationWithTransform();
    }

    // private void PlayWinAnimation()
    // {
    //     PlayAnimation(winSprites, false);
    // }

    // private void PlayFailAnimation()
    // {
    //     PlayAnimation(failSprites, false);
    // }

    // private void PlayAnimation(Sprite[] sprites, bool loop)
    // {
    //     if (sprites == null || sprites.Length == 0)
    //     {
    //         Debug.LogWarning("Sprite array is empty or null!");
    //         return;
    //     }

    //     if (currentAnimation != null)
    //     {
    //         StopCoroutine(currentAnimation);
    //     }

    //     currentAnimation = StartCoroutine(AnimateSprites(sprites, loop));
    // }

    private void PlayIdleAnimationWithTransform()
    {
        if (idleSprites == null || idleSprites.Length == 0)
        {
            Debug.LogWarning("Idle sprite array is empty or null!");
            return;
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(AnimateIdleSprites());
    }

    private IEnumerator AnimateIdleSprites()
    {
        float frameDuration = 1f / frameRate;
        RectTransform rectTransform = imageAnimator.GetComponent<RectTransform>();

        do
        {
            for (int i = 0; i < idleSprites.Length; i++)
            {
                imageAnimator.sprite = idleSprites[i];

                yield return new WaitForSeconds(frameDuration);
            }
        } while (loopAnimation);
    }

    private IEnumerator AnimateSprites(Sprite[] sprites, bool loop)
    {
        float frameDuration = 1f / frameRate;

        do
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                imageAnimator.sprite = sprites[i];
                yield return new WaitForSeconds(frameDuration);
            }
        } while (loop);
    }
}
