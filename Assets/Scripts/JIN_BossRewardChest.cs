using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class JIN_BossRewardChest : MonoBehaviour
{
    [SerializeField, Range(1, 3)]
    private int rewardChoiceCount = 3;

    [SerializeField]
    private string playerTag = "Player";

    private bool opened;

    public static JIN_BossRewardChest CreateRuntimeChest(Vector3 position, int choiceCount)
    {
        GameObject chestObject = new GameObject("JIN_BossRewardChest");
        chestObject.transform.position = position;

        SpriteRenderer spriteRenderer = chestObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = JIN_RuntimeSpriteUtility.WhiteSprite;
        spriteRenderer.color = new Color(1f, 0.72f, 0.18f, 1f);
        spriteRenderer.sortingOrder = 3;
        chestObject.transform.localScale = new Vector3(0.7f, 0.5f, 1f);

        Rigidbody2D body = chestObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;

        BoxCollider2D collider = chestObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        JIN_BossRewardChest chest = chestObject.AddComponent<JIN_BossRewardChest>();
        chest.SetRewardChoiceCount(choiceCount);
        return chest;
    }

    public void SetRewardChoiceCount(int choiceCount)
    {
        rewardChoiceCount = Mathf.Clamp(choiceCount, 1, 3);
    }

    private void OnValidate()
    {
        rewardChoiceCount = Mathf.Clamp(rewardChoiceCount, 1, 3);
    }

    private void Reset()
    {
        Collider2D chestCollider = GetComponent<Collider2D>();

        if (chestCollider != null)
        {
            chestCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryOpen(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryOpen(other);
    }

    private void TryOpen(Collider2D other)
    {
        if (opened || !IsPlayerCollider(other))
        {
            return;
        }

        JIN_LevelUpRewardController rewardController = FindAnyObjectByType<JIN_LevelUpRewardController>();

        if (rewardController == null)
        {
            return;
        }

        // 보스 상자는 일반 레벨업과 다른 티어 확률로 보상 선택창을 연다.
        if (!rewardController.TryOpenBossChestReward(rewardChoiceCount))
        {
            return;
        }

        opened = true;
        Destroy(gameObject);
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag)
            || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            || (other.transform.root != null && other.transform.root.CompareTag(playerTag));
    }
}
