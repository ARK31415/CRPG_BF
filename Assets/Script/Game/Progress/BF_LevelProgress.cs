using UnityEngine;

public class BF_LevelProgress : MonoBehaviour
{
    public int HighestUnlockedLevel { get; private set; } = 1;
    public bool IsDemoCompleted { get; private set; }

    public bool IsUnlocked(int level)
    {
        return level >= 1 && level <= HighestUnlockedLevel;
    }

    public bool IsCompleted(int level)
    {
        return level < HighestUnlockedLevel || level == 3 && IsDemoCompleted;
    }

    public void CompleteLevel(int level)
    {
        if (level < 1 || level > 3)
        {
            return;
        }

        if (level == 3)
        {
            IsDemoCompleted = true;
            return;
        }

        HighestUnlockedLevel = Mathf.Max(HighestUnlockedLevel, level + 1);
    }

    public void ResetProgress()
    {
        HighestUnlockedLevel = 1;
        IsDemoCompleted = false;
    }

    public void LoadProgress(int highestUnlockedLevel, bool isDemoCompleted)
    {
        HighestUnlockedLevel = Mathf.Clamp(highestUnlockedLevel, 1, 3);
        IsDemoCompleted = isDemoCompleted;
    }
}
