using System;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class BF_SaveService : Singleton<BF_SaveService>
{
    private const int SaveVersion = 1;
    private const int SlotCount = 3;

    [SerializeField] private BF_LevelProgress _levelProgress;

    public int CurrentSlot { get; private set; }

    public bool HasSave(int slot)
    {
        return TryRead(slot, out _, false);
    }

    public BF_SaveSlotInfo GetSlotInfo(int slot)
    {
        BF_SaveSlotInfo info = new BF_SaveSlotInfo
        {
            Slot = slot,
            HasData = IsValidSlot(slot) && File.Exists(GetPath(slot))
        };

        if (!TryRead(slot, out BF_SaveData data, false))
        {
            return info;
        }

        info.IsValid = true;
        info.HighestUnlockedLevel = data.HighestUnlockedLevel;
        info.UnitCount = data.Units.Count;
        info.SavedAt = data.SavedAt;
        return info;
    }

    public bool StartNewGame(int slot, Action createInitialUnits)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService unitRuntime = BF_UnitRuntimeService.Instance;
        if (_levelProgress == null
            || inventory == null
            || unitRuntime == null
            || !IsValidSlot(slot))
        {
            return false;
        }

        _levelProgress.ResetProgress();
        inventory.ResetToDefaults();
        unitRuntime.Clear(false);

        try
        {
            createInitialUnits?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }

        CurrentSlot = slot;
        if (Save())
        {
            return true;
        }

        CurrentSlot = 0;
        return false;
    }

    public bool Load(int slot)
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService unitRuntime = BF_UnitRuntimeService.Instance;
        if (_levelProgress == null
            || inventory == null
            || unitRuntime == null
            || !TryRead(slot, out BF_SaveData data, true))
        {
            return false;
        }

        if (!inventory.CanLoadData(data.Gold, data.Inventory)
            || !unitRuntime.CanLoadUnits(data.Units))
        {
            Debug.LogWarning($"[BF] Save slot {slot} contains invalid runtime data.", this);
            return false;
        }

        _levelProgress.LoadProgress(data.HighestUnlockedLevel, data.IsDemoCompleted);
        inventory.LoadData(data.Gold, data.Inventory);
        unitRuntime.LoadUnits(data.Units);
        CurrentSlot = slot;
        Debug.Log($"[BF] Loaded save slot {slot}.", this);
        return true;
    }

    public bool Save()
    {
        if (!IsReady() || !IsValidSlot(CurrentSlot))
        {
            return false;
        }

        string path = GetPath(CurrentSlot);
        string tempPath = path + ".tmp";

        try
        {
            Directory.CreateDirectory(GetSaveFolder());
            string json = JsonUtility.ToJson(BuildData(), true);
            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, null);
            }
            else
            {
                File.Move(tempPath, path);
            }

            Debug.Log($"[BF] Saved slot {CurrentSlot}: {path}", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[BF] Failed to save slot {CurrentSlot}.", this);
            Debug.LogException(exception, this);

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            return false;
        }
    }

    public bool Delete(int slot)
    {
        if (!IsValidSlot(slot))
        {
            return false;
        }

        try
        {
            string path = GetPath(slot);
            string tempPath = path + ".tmp";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (CurrentSlot == slot)
            {
                CurrentSlot = 0;
            }

            Debug.Log($"[BF] Deleted save slot {slot}.", this);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }
    }

    private BF_SaveData BuildData()
    {
        BF_InventoryService inventory = BF_InventoryService.Instance;
        BF_UnitRuntimeService unitRuntime = BF_UnitRuntimeService.Instance;
        BF_SaveData data = new BF_SaveData
        {
            Version = SaveVersion,
            SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            HighestUnlockedLevel = _levelProgress.HighestUnlockedLevel,
            IsDemoCompleted = _levelProgress.IsDemoCompleted,
            Gold = inventory.Gold
        };

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            BF_InventoryEntry entry = inventory.Items[i];
            if (entry.Item == null || entry.Quantity <= 0)
            {
                continue;
            }

            data.Inventory.Add(new BF_InventorySaveEntry
            {
                ItemId = entry.Item.Id,
                Quantity = entry.Quantity
            });
        }

        for (int i = 0; i < unitRuntime.Units.Count; i++)
        {
            data.Units.Add(unitRuntime.Units[i].Clone());
        }

        return data;
    }

    private bool TryRead(int slot, out BF_SaveData data, bool logError)
    {
        data = null;
        if (!IsValidSlot(slot))
        {
            return false;
        }

        string path = GetPath(slot);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<BF_SaveData>(json);
            if (data == null
                || data.Version != SaveVersion
                || data.Inventory == null
                || data.Units == null)
            {
                data = null;
                if (logError)
                {
                    Debug.LogWarning($"[BF] Save slot {slot} is invalid or uses an unsupported version.", this);
                }

                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            if (logError)
            {
                Debug.LogWarning($"[BF] Cannot read save slot {slot}: {exception.Message}", this);
            }

            data = null;
            return false;
        }
    }

    private bool IsReady()
    {
        return _levelProgress != null
            && BF_InventoryService.Instance != null
            && BF_UnitRuntimeService.Instance != null;
    }

    private bool IsValidSlot(int slot)
    {
        return slot >= 1 && slot <= SlotCount;
    }

    private string GetSaveFolder()
    {
        return Path.Combine(Application.persistentDataPath, "SaveData");
    }

    private string GetPath(int slot)
    {
        return Path.Combine(GetSaveFolder(), $"save_{slot:00}.json");
    }
}
