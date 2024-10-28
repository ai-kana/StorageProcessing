using Rocket.API;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Action = System.Action;

namespace StorageProcessing;

public class StorageProcessingConfiguration : IRocketPluginConfiguration
{
    public List<ProcessData> Processes = new();

    public void LoadDefaults()
    {
        Processes = [new()];
    }
}

public class StorageProcessingPlugin : RocketPlugin<StorageProcessingConfiguration>
{
    public static StorageProcessingPlugin Instance {get; private set;} = null!;
    public static StorageProcessingConfiguration Config => Instance.Configuration.Instance;

    public static event Action? OnUnloaded;

    private static bool Reloaded = false;

    protected override void Load()
    {
        base.Load();
        Instance = this;

        if (Reloaded)
        {
            OnPostLoaded(0);
        }

        Level.onPostLevelLoaded += OnPostLoaded;
    }

    private void OnPostLoaded(int level)
    {
        BarricadeManager.onBarricadeSpawned += OnSpawned;

        foreach (BarricadeRegion region in BarricadeManager.BarricadeRegions)
        foreach (BarricadeDrop drop in region.drops)
        {
            OnSpawned(region, drop);
        }

        Reloaded = true;
    }

    protected override void Unload()
    {
        base.Unload();
        Instance = null!;

        OnUnloaded?.Invoke();

        BarricadeManager.onBarricadeSpawned -= OnSpawned;
        Level.onPostLevelLoaded -= OnPostLoaded;
    }

    private void OnSpawned(BarricadeRegion region, BarricadeDrop drop)
    {
        ProcessData? processData = Config.Processes.FirstOrDefault(x => x.ID == drop.asset.id);
        if (processData == null)
        {
            return;
        }

        if (drop.interactable is not InteractableStorage storage)
        {
            return;
        }

        foreach (StorageProcess process in processData.Processes)
        {
            StorageProcessor processor = drop.model.gameObject.AddComponent<StorageProcessor>();
            processor.DropOffset = processData.DropOffset.ToVector3();
            processor.Storage = storage;
            processor.Process = process;
        }
    }
}
