using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class JIN_SetSynergyController : MonoBehaviour
{
    [SerializeField]
    private JIN_PlayerItemInventory inventory;

    private readonly List<JIN_SetProgress> cachedProgress = new List<JIN_SetProgress>();

    public event Action<JIN_SetSynergyController> SynergyChanged;

    public IReadOnlyList<JIN_SetProgress> Progress => cachedProgress;

    private void Awake()
    {
        ResolveReferences();
        RebuildProgress();
    }

    private void OnEnable()
    {
        Subscribe();
        RebuildProgress();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Configure(JIN_PlayerItemInventory newInventory)
    {
        if (inventory == newInventory)
        {
            return;
        }

        Unsubscribe();
        inventory = newInventory;
        Subscribe();
        RebuildProgress();
        SynergyChanged?.Invoke(this);
    }

    public JIN_SetProgress GetProgress(string setId)
    {
        foreach (JIN_SetProgress progress in cachedProgress)
        {
            if (progress.Definition.Id == setId)
            {
                return progress;
            }
        }

        return new JIN_SetProgress(JIN_ItemCatalog.GetSetOrNull(setId), 0);
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = GetComponent<JIN_PlayerItemInventory>();
        }
    }

    private void Subscribe()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= HandleInventoryChanged;
        inventory.InventoryChanged += HandleInventoryChanged;
    }

    private void Unsubscribe()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged(JIN_PlayerItemInventory changedInventory)
    {
        RebuildProgress();
        SynergyChanged?.Invoke(this);
    }

    private void RebuildProgress()
    {
        cachedProgress.Clear();

        // TFT처럼 세트 단계는 보유한 서로 다른 아이템 개수로 계산한다.
        foreach (JIN_SetDefinition set in JIN_ItemCatalog.AllSets)
        {
            int ownedCount = inventory != null ? inventory.GetUniqueItemCountInSet(set.Id) : 0;
            cachedProgress.Add(new JIN_SetProgress(set, ownedCount));
        }
    }
}
