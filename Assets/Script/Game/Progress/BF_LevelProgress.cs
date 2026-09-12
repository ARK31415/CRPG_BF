using UnityEngine;

/// <summary>
/// 关卡进度组件：维护最高解锁关卡与 Demo 完成标记，支持查询、推进与存档恢复。
/// </summary>
public class BF_LevelProgress : MonoBehaviour
{
    #region 对外接口

    public int HighestUnlockedLevel { get; private set; } = 1;
    public bool IsDemoCompleted { get; private set; }

    #endregion

    #region 进度查询

    public bool IsUnlocked(int level)
    {
        return level >= 1 && level <= HighestUnlockedLevel;
    }

    public bool IsCompleted(int level)
    {
        return level < HighestUnlockedLevel || level == 3 && IsDemoCompleted;
    }

    #endregion

    #region 进度变更与恢复

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

    #endregion
}
