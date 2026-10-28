using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "GameTypography", menuName = "Game/Typography")]
public class GameTypography : ScriptableObject
{
    public TMP_FontAsset font;
    private static GameTypography current;
    public static GameTypography Current => current ? current : current = Resources.Load<GameTypography>("GameTypography");
    public static bool UseText => Current && Current.font;
}
