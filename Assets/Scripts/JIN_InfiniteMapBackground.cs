using UnityEngine;

/// <summary>
/// 플레이어 주변에 배경 타일을 재배치해 끝없이 이어지는 맵처럼 보이게 한다.
/// </summary>
[DisallowMultipleComponent]
public class JIN_InfiniteMapBackground : MonoBehaviour
{
    private const int TileRange = 1;
    private const int TileCountPerAxis = (TileRange * 2) + 1;
    private const int TileCount = TileCountPerAxis * TileCountPerAxis;

    [Header("Map")]
    [SerializeField]
    private Sprite mapSprite;

    [SerializeField, Min(0.01f)]
    private float tileScale = 3f;

    [SerializeField]
    private int sortingOrder = -10;

    [Header("Follow")]
    [SerializeField]
    private Transform followTarget;

    [SerializeField]
    private Vector2 originOffset;

    private readonly SpriteRenderer[] tileRenderers = new SpriteRenderer[TileCount];
    private SpriteRenderer cachedRootRenderer;
    private Vector2 tileWorldSize = Vector2.one;

    private void Awake()
    {
        ResolveOptionalReferences();
        EnsureTiles();
        UpdateTileWorldSize();
        RefreshTiles();
    }

    private void OnValidate()
    {
        tileScale = Mathf.Max(0.01f, tileScale);
    }

    private void LateUpdate()
    {
        if (mapSprite == null || followTarget == null)
        {
            return;
        }

        UpdateTileWorldSize();
        RefreshTiles();
    }

    private void ResolveOptionalReferences()
    {
        if (cachedRootRenderer == null)
        {
            TryGetComponent(out cachedRootRenderer);
        }

        if (cachedRootRenderer != null)
        {
            cachedRootRenderer.enabled = false;
        }

        if (followTarget == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            followTarget = player != null ? player.transform : null;
        }
    }

    private void EnsureTiles()
    {
        for (int i = 0; i < TileCount; i++)
        {
            if (tileRenderers[i] != null)
            {
                continue;
            }

            GameObject tileObject = new GameObject($"MapTile_{i:00}");
            tileObject.transform.SetParent(transform, false);
            SpriteRenderer tileRenderer = tileObject.AddComponent<SpriteRenderer>();
            tileRenderers[i] = tileRenderer;
        }
    }

    private void UpdateTileWorldSize()
    {
        if (mapSprite == null)
        {
            tileWorldSize = Vector2.one;
            return;
        }

        Vector2 spriteSize = mapSprite.bounds.size;
        tileWorldSize = new Vector2(
            Mathf.Max(0.01f, spriteSize.x * tileScale),
            Mathf.Max(0.01f, spriteSize.y * tileScale));
    }

    private void RefreshTiles()
    {
        Vector2 center = followTarget != null ? (Vector2)followTarget.position : (Vector2)transform.position;
        Vector2 snappedCenter = new Vector2(
            Mathf.Floor(center.x / tileWorldSize.x) * tileWorldSize.x,
            Mathf.Floor(center.y / tileWorldSize.y) * tileWorldSize.y) + originOffset;

        int index = 0;

        for (int y = -TileRange; y <= TileRange; y++)
        {
            for (int x = -TileRange; x <= TileRange; x++)
            {
                SpriteRenderer tileRenderer = tileRenderers[index++];

                if (tileRenderer == null)
                {
                    continue;
                }

                ConfigureTile(tileRenderer, snappedCenter, x, y);
            }
        }
    }

    private void ConfigureTile(SpriteRenderer tileRenderer, Vector2 snappedCenter, int x, int y)
    {
        tileRenderer.sprite = mapSprite;
        tileRenderer.sortingOrder = sortingOrder;
        tileRenderer.transform.localScale = new Vector3(tileScale, tileScale, 1f);

        // 플레이어가 타일 경계를 넘어가면 3x3 타일 묶음을 한 칸씩 옮긴다.
        Vector2 tilePosition = snappedCenter + new Vector2(x * tileWorldSize.x, y * tileWorldSize.y);
        tileRenderer.transform.position = new Vector3(tilePosition.x, tilePosition.y, transform.position.z);
    }
}
