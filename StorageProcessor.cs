using System.Collections;
using System.Collections.Concurrent;
using SDG.Unturned;
using UnityEngine;

namespace StorageProcessing;

public class StorageProcessor : MonoBehaviour
{
    public InteractableStorage Storage = null!;
    public StorageProcess Process = null!;
    public Vector3 DropOffset;

    private ConcurrentQueue<IEnumerator> CraftRoutines = new();
    private ConcurrentQueue<Coroutine> ActiveRoutines = new();
    private bool IsWorking = false;

    public Action<bool>? ChangedCraftingState;
    private int CraftingCount = 0;

    // ushort input ID
    // byte amount required
    private ConcurrentDictionary<ushort, sbyte> RequiredAmounts = new();

    public void Start()
    {
        DontDestroyOnLoad(this);
        StorageProcessingPlugin.OnUnloaded += OnUnloaded;

        Storage.items.onItemAdded += OnItemAdded;

        foreach (ushort id in Process.InputIDs)
        {
            if (!RequiredAmounts.TryAdd(id, 1))
            {
                RequiredAmounts[id]++;
            }
        }
    }

    private void OnUnloaded()
    {
        StorageProcessingPlugin.OnUnloaded -= OnUnloaded;
        Storage.items.onItemAdded -= OnItemAdded;
        while (ActiveRoutines.TryDequeue(out Coroutine result))
        {
            StopCoroutine(result);
        }
        
        Destroy(this);
    }

    private void Update()
    {
        if (!IsWorking)
        if (CraftRoutines.TryDequeue(out IEnumerator result))
        {
            Coroutine routine = StartCoroutine(result);
            ActiveRoutines.Enqueue(routine);
        }
    }

    private void OnItemAdded(byte page, byte index, ItemJar jar)
    {
        CraftRoutines.Enqueue(TryCraft());
    }

    private IEnumerator TryCraft()
    {
        yield return null;
        IsWorking = true;

        Dictionary<ushort, sbyte> requiredAmounts = new(RequiredAmounts);
        List<byte> indexes = new();

        foreach (ushort id in RequiredAmounts.Keys)
        for (byte i = 0; i < Storage.items.items.Count; i++)
        {
            if (requiredAmounts[id] <= 0)
            {
                continue;
            }

            ItemJar jar = Storage.items.items[i];
            if (jar.item.id == id)
            {
                requiredAmounts[id]--;
                indexes.Add(i);
            }
        }

        foreach (sbyte amt in requiredAmounts.Values)
        {
            if (amt > 0)
            {
                IsWorking = false;
                yield break;
            }
        }

        byte totalRemoved = 0;
        IEnumerable<byte> ordered = indexes.OrderBy(x => x);

        foreach (byte index in ordered)
        {
            byte newIndex = (byte)(index - totalRemoved);
            Storage.items.removeItem(newIndex);
            totalRemoved++;
        }

        Vector3 point = Storage.transform.position + DropOffset;
        if (CraftingCount == 0)
        {
            ChangedCraftingState?.Invoke(true);
        }
        CraftingCount++;

        IsWorking = false;
        yield return new WaitForSeconds(Process.ProcessTime);

        CraftingCount--;
        if (CraftingCount == 0)
        {
            ChangedCraftingState?.Invoke(false);
        }

        foreach (ushort id in Process.OutputIDs)
        {
            Item item = new(id, EItemOrigin.ADMIN);
            if (!Storage.items.tryAddItem(item))
            {
                ItemManager.dropItem(item, point, false, true, false);
            }
        }
    }
}
