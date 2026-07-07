using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class JIN_XpPickup : MonoBehaviour
{
    private const string EditorExpSpritePath = "Assets/Image/Exp.png";
    private const float RuntimePickupScale = 1.5f;
    private static readonly Vector2 LeftFacingMouthOffset = new Vector2(-0.55f, 0.2f);

    [SerializeField, Min(1)]
    private int experienceValue = 1;

    [SerializeField, Min(0f)]
    private float attractRadius = 4f;

    [SerializeField, Min(0.05f)]
    private float collectRadius = 0.45f;

    [SerializeField, Min(0f)]
    private float moveSpeed = 8f;

    [SerializeField]
    private string playerTag = "Player";

    private JIN_PlayerExperience targetExperience;
    private PlayerController targetPlayerController;
    private SpriteRenderer targetSpriteRenderer;
    private bool collected;

    public static JIN_XpPickup CreateRuntimePickup(Vector3 position, int amount)
    {
        GameObject pickupObject = new GameObject("JIN_XpPickup");
        pickupObject.transform.position = position;

        SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
        Sprite pickupSprite = ResolveRuntimePickupSprite();
        spriteRenderer.sprite = pickupSprite;
        spriteRenderer.color = pickupSprite == JIN_RuntimeSpriteUtility.WhiteSprite
            ? new Color(0.2f, 0.95f, 1f, 1f)
            : Color.white;
        spriteRenderer.sortingOrder = 1;
        pickupObject.transform.localScale = new Vector3(RuntimePickupScale, RuntimePickupScale, 1f);

        Rigidbody2D body = pickupObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;

        CircleCollider2D collider = pickupObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        JIN_XpPickup pickup = pickupObject.AddComponent<JIN_XpPickup>();
        pickup.SetExperienceValue(amount);
        return pickup;
    }

    private static Sprite ResolveRuntimePickupSprite()
    {
#if UNITY_EDITOR
        // 프로토타입에서는 프리팹 없이도 Image 폴더의 경험치 이미지를 바로 확인할 수 있게 한다.
        Sprite editorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(EditorExpSpritePath);

        if (editorSprite != null)
        {
            return editorSprite;
        }
#endif

        return JIN_RuntimeSpriteUtility.WhiteSprite;
    }

    public void SetExperienceValue(int amount)
    {
        experienceValue = Mathf.Max(1, amount);
    }

    private void OnValidate()
    {
        experienceValue = Mathf.Max(1, experienceValue);
        attractRadius = Mathf.Max(0f, attractRadius);
        collectRadius = Mathf.Max(0.05f, collectRadius);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (collected)
        {
            return;
        }

        ResolveTarget();

        if (targetExperience == null)
        {
            return;
        }

        Vector3 targetPosition = ResolveTargetMouthPosition();
        float sqrDistance = (targetPosition - transform.position).sqrMagnitude;

        if (sqrDistance <= collectRadius * collectRadius)
        {
            Collect(targetExperience);
            return;
        }

        if (sqrDistance > attractRadius * attractRadius)
        {
            return;
        }

        // 가까이 온 경험치는 플레이어에게 빨려 들어가도록 이동시킨다.
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollectFromCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollectFromCollider(other);
    }

    private void ResolveTarget()
    {
        if (targetExperience != null)
        {
            return;
        }

        JIN_PlayerExperience[] candidates = FindObjectsByType<JIN_PlayerExperience>(FindObjectsInactive.Exclude);
        float bestSqrDistance = float.PositiveInfinity;

        foreach (JIN_PlayerExperience candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            bestSqrDistance = sqrDistance;
            targetExperience = candidate;
            targetPlayerController = candidate.GetComponent<PlayerController>();
            targetSpriteRenderer = candidate.GetComponent<SpriteRenderer>();
        }
    }

    private Vector3 ResolveTargetMouthPosition()
    {
        if (targetExperience == null)
        {
            return transform.position;
        }

        Vector2 mouthOffset = LeftFacingMouthOffset;

        if (ShouldUseRightFacingMouth())
        {
            mouthOffset.x = -mouthOffset.x;
        }

        // 경험치 쿠키는 플레이어 중심이 아니라 입 위치로 빨려 들어가게 한다.
        return targetExperience.transform.position + (Vector3)mouthOffset;
    }

    private bool ShouldUseRightFacingMouth()
    {
        if (targetPlayerController != null)
        {
            if (targetPlayerController.MovementInput.x > 0.01f)
            {
                return true;
            }

            if (targetPlayerController.MovementInput.x < -0.01f)
            {
                return false;
            }
        }

        return targetSpriteRenderer != null && targetSpriteRenderer.flipX;
    }

    private void TryCollectFromCollider(Collider2D other)
    {
        if (collected || other == null)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        JIN_PlayerExperience experience = ResolveExperience(other);

        if (experience != null)
        {
            Collect(experience);
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            || (other.transform.root != null && other.transform.root.CompareTag(playerTag));
    }

    private JIN_PlayerExperience ResolveExperience(Collider2D other)
    {
        if (other.TryGetComponent(out JIN_PlayerExperience experience))
        {
            return experience;
        }

        if (other.attachedRigidbody != null
            && other.attachedRigidbody.TryGetComponent(out experience))
        {
            return experience;
        }

        return other.GetComponentInParent<JIN_PlayerExperience>();
    }

    private void Collect(JIN_PlayerExperience experience)
    {
        if (collected || experience == null)
        {
            return;
        }

        collected = true;
        experience.AddExperience(experienceValue);
        Destroy(gameObject);
    }
}

public static class JIN_RuntimeSpriteUtility
{
    private static Sprite whiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                Texture2D texture = Texture2D.whiteTexture;
                Rect rect = new Rect(0f, 0f, texture.width, texture.height);
                whiteSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 1f);
            }

            return whiteSprite;
        }
    }
}
