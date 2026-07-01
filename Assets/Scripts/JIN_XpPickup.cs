using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class JIN_XpPickup : MonoBehaviour
{
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
    private bool collected;

    public static JIN_XpPickup CreateRuntimePickup(Vector3 position, int amount)
    {
        GameObject pickupObject = new GameObject("JIN_XpPickup");
        pickupObject.transform.position = position;

        SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        spriteRenderer.color = new Color(0.2f, 0.95f, 1f, 1f);
        spriteRenderer.sortingOrder = 3;
        pickupObject.transform.localScale = new Vector3(0.22f, 0.22f, 1f);

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

        Vector3 targetPosition = targetExperience.transform.position;
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
        }
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
