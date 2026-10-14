using UnityEngine;

public static class LevelProgress
{
    private const string LatestKey = "Progress.LatestLevel";
    private const string UnlockedKey = "Progress.HighestUnlockedLevel";
    private const string CompletedKey = "Progress.HighestCompletedLevel";
    public static int HighestCompleted => Mathf.Max(0, PlayerPrefs.GetInt(CompletedKey, PlayerPrefs.GetInt(UnlockedKey, 0)));

    public static int Latest(int count) => Mathf.Clamp(PlayerPrefs.GetInt(LatestKey, 0), 0, HighestUnlocked(count));
    public static int HighestUnlocked(int count) => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, Mathf.Max(0, count - 1));

    public static void Started(int index)
    {
        PlayerPrefs.SetInt(LatestKey, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }

    public static void Won(int index, int count)
    {
        if(index < 0 || index >= count) return;
        PlayerPrefs.SetInt(CompletedKey, Mathf.Max(HighestCompleted, index + 1));
        int next = Mathf.Clamp(index + 1, 0, Mathf.Max(0, count - 1));
        PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(HighestUnlocked(count), next));

        PlayerPrefs.SetInt(LatestKey, next);
        PlayerPrefs.Save();
    }
}
