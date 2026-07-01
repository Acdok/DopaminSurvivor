using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class JIN_PlayerItemInventory : MonoBehaviour
{
    private readonly Dictionary<string, int> itemLevels = new Dictionary<string, int>();
    private readonly List<JIN_OwnedItemInfo> ownedItems = new List<JIN_OwnedItemInfo>();

    public event Action<JIN_PlayerItemInventory> InventoryChanged;
    public event Action<JIN_ItemDefinition, int> ItemAdded;

    public IReadOnlyList<JIN_OwnedItemInfo> OwnedItems => ownedItems;

    public bool AddItem(string itemId)
    {
        if (!JIN_ItemCatalog.TryGetItem(itemId, out JIN_ItemDefinition definition))
        {
            return false;
        }

        int currentLevel = GetItemLevel(itemId);

        if (currentLevel >= definition.MaxLevel)
        {
            return false;
        }

        int nextLevel = currentLevel + 1;
        itemLevels[itemId] = nextLevel;
        RebuildOwnedItems();

        // 아이템 획득과 강화는 같은 진입점으로 처리해 UI와 효과 갱신을 단순화한다.
        ItemAdded?.Invoke(definition, nextLevel);
        InventoryChanged?.Invoke(this);
        return true;
    }

    public int GetItemLevel(string itemId)
    {
        return !string.IsNullOrEmpty(itemId) && itemLevels.TryGetValue(itemId, out int level) ? level : 0;
    }

    public int GetUniqueItemCountInSet(string setId)
    {
        if (string.IsNullOrEmpty(setId))
        {
            return 0;
        }

        int count = 0;

        foreach (JIN_ItemDefinition item in JIN_ItemCatalog.AllItems)
        {
            if (item.SetId == setId && GetItemLevel(item.Id) > 0)
            {
                count++;
            }
        }

        return count;
    }

    public List<JIN_OwnedItemInfo> GetOwnedItemsInSet(string setId)
    {
        List<JIN_OwnedItemInfo> result = new List<JIN_OwnedItemInfo>();

        foreach (JIN_OwnedItemInfo ownedItem in ownedItems)
        {
            if (ownedItem.Definition.SetId == setId)
            {
                result.Add(ownedItem);
            }
        }

        return result;
    }

    private void RebuildOwnedItems()
    {
        ownedItems.Clear();

        foreach (JIN_ItemDefinition item in JIN_ItemCatalog.AllItems)
        {
            int level = GetItemLevel(item.Id);

            if (level > 0)
            {
                ownedItems.Add(new JIN_OwnedItemInfo(item, level));
            }
        }
    }
}

public readonly struct JIN_OwnedItemInfo
{
    public JIN_OwnedItemInfo(JIN_ItemDefinition definition, int level)
    {
        Definition = definition;
        Level = level;
    }

    public JIN_ItemDefinition Definition { get; }
    public int Level { get; }
}
