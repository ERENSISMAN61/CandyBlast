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

    private Coroutine currentAnimation;

    private void OnEnable()
    {
        EventManager.Instance.OnLevelStart += PlayIdleAnimation;
        EventManager.Instance.OnWin += PlayWinAnimation;
        EventManager.Instance.OnFail += PlayFailAnimation;
    }

    private void OnDisable()
    {
        EventManager.Instance.OnLevelStart -= PlayIdleAnimation;
        EventManager.Instance.OnWin -= PlayWinAnimation;
        EventManager.Instance.OnFail -= PlayFailAnimation;
    }

    private void OnDestroy()
    {
        EventManager.Instance.OnLevelStart -= PlayIdleAnimation;
        EventManager.Instance.OnWin -= PlayWinAnimation;
        EventManager.Instance.OnFail -= PlayFailAnimation;
    }

    void Start()
    {
        PlayIdleAnimation();
    }
    private void PlayIdleAnimation()
    {
        PlayIdleAnimationWithTransform();
    }

    private void PlayWinAnimation()
    {
        PlayAnimation(winSprites, false);
    }

    private void PlayFailAnimation()
    {
        PlayAnimation(failSprites, false);
    }

    private void PlayAnimation(Sprite[] sprites, bool loop)
    {
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("Sprite array is empty or null!");
            return;
        }

        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }

        currentAnimation = StartCoroutine(AnimateSprites(sprites, loop));
    }

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

                // İlk 36 sprite: scale 1, rotation 0
                if (i < 36)
                {
                    rectTransform.localScale = Vector3.one;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
                }
                // 36-72 arası sprite'lar: scale 1.15, rotation Z -3.6
                else if (i >= 36 && i <= 72)
                {
                    rectTransform.localScale = Vector3.one * 1.15f;
                    rectTransform.localRotation = Quaternion.Euler(0, 0, -3.6f);
                }

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
