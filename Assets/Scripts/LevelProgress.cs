using UnityEngine;

public static class LevelProgress
{
    private const string LatestKey = "Progress.LatestLevel";
    private const string UnlockedKey = "Progress.HighestUnlockedLevel";

    public static int Latest(int count) => Mathf.Clamp(PlayerPrefs.GetInt(LatestKey, 0), 0, HighestUnlocked(count));
    public static int HighestUnlocked(int count) => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 0), 0, Mathf.Max(0, count - 1));

    public static void Started(int index)
    {
        PlayerPrefs.SetInt(LatestKey, Mathf.Max(0, index));
        PlayerPrefs.Save();
    }

    public static void Won(int index, int count)
    {
        int next = Mathf.Clamp(index + 1, 0, Mathf.Max(0, count - 1));
        PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(HighestUnlocked(count), next));
        // Continue to the next level after a win, even when returning home first.
        PlayerPrefs.SetInt(LatestKey, next);
        PlayerPrefs.Save();
    }
}
