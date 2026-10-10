using UnityEngine;


public static class GameSession
{
    public static int SelectedLevel { get; private set; }
    public static bool HasSelectedLevel { get; private set; }

    public static void SelectLevel(int index)
    {
        SelectedLevel = index;
        HasSelectedLevel = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        SelectedLevel = 0;
        HasSelectedLevel = false;
    }
}
