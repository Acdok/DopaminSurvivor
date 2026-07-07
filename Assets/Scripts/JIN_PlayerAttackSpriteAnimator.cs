using UnityEngine;

/// <summary>
/// Player 공격 상태에 맞춰 분리된 캐릭터 스프라이트 프레임을 교체한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class JIN_PlayerAttackSpriteAnimator : MonoBehaviour
{
    private const float MinimumFramesPerSecond = 0.1f;
    private const float HorizontalFacingThreshold = 0.01f;

    [Header("Animation")]
    [SerializeField, Min(MinimumFramesPerSecond)]
    private float framesPerSecond = 10f;

    [SerializeField]
    private Sprite[] normalAttackSprites;

    [SerializeField]
    private Sprite[] brimstoneAttackSprites;

    [Header("References")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private WeaponController weaponController;

    private Sprite[] activeSprites;
    private float frameElapsed;
    private int frameIndex;
    private int lastFacingSign = -1;

    private void Awake()
    {
        ResolveReferences();
        ApplyInitialSequence();
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        framesPerSecond = Mathf.Max(MinimumFramesPerSecond, framesPerSecond);
    }

    private void Update()
    {
        ResolveFacing();
        Sprite[] nextSprites = ResolveActiveSprites();

        if (nextSprites != activeSprites)
        {
            SetActiveSequence(nextSprites);
        }

        TickFrame();
        ApplyFacing();
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            TryGetComponent(out spriteRenderer);
        }

        if (playerController == null)
        {
            TryGetComponent(out playerController);
        }

        if (weaponController == null)
        {
            TryGetComponent(out weaponController);
        }
    }

    private void ApplyInitialSequence()
    {
        SetActiveSequence(ResolveActiveSprites());
        ApplyFacing();
    }

    private void ResolveFacing()
    {
        if (playerController == null)
        {
            return;
        }

        float horizontalInput = playerController.MovementInput.x;

        if (horizontalInput < -HorizontalFacingThreshold)
        {
            lastFacingSign = -1;
        }
        else if (horizontalInput > HorizontalFacingThreshold)
        {
            lastFacingSign = 1;
        }
    }

    private Sprite[] ResolveActiveSprites()
    {
        // 혈사포 프레임은 실제 레이저가 유지되는 동안에만 일반 프레임 위로 교체한다.
        if (weaponController != null
            && weaponController.UseBrimstoneLaser
            && weaponController.IsBrimstoneFiring
            && HasSprites(brimstoneAttackSprites))
        {
            return brimstoneAttackSprites;
        }

        return normalAttackSprites;
    }

    private void SetActiveSequence(Sprite[] nextSprites)
    {
        activeSprites = nextSprites;
        frameElapsed = 0f;
        frameIndex = 0;
        ApplyCurrentSprite();
    }

    private void TickFrame()
    {
        if (!HasSprites(activeSprites))
        {
            return;
        }

        frameElapsed += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(MinimumFramesPerSecond, framesPerSecond);

        while (frameElapsed >= frameDuration)
        {
            frameElapsed -= frameDuration;
            frameIndex = (frameIndex + 1) % activeSprites.Length;
            ApplyCurrentSprite();
        }
    }

    private void ApplyCurrentSprite()
    {
        if (spriteRenderer == null || !HasSprites(activeSprites))
        {
            return;
        }

        spriteRenderer.sprite = activeSprites[Mathf.Clamp(frameIndex, 0, activeSprites.Length - 1)];
    }

    private void ApplyFacing()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // 원본은 왼쪽 방향 기준으로 사용하고, 오른쪽 이동만 좌우 반전한다.
        spriteRenderer.flipX = lastFacingSign > 0;
    }

    private static bool HasSprites(Sprite[] sprites)
    {
        return sprites != null && sprites.Length > 0;
    }
}
