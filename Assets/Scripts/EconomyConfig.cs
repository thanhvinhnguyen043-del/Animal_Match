using System;
using UnityEngine;

[Serializable]
public class ShopBackground
{
    public string id;
    public string displayName;
    [Min(0)] public int price;
    [Min(0)] public int requiredCompletedLevel;
    public Sprite sprite;
}

[CreateAssetMenu(fileName="EconomyConfig",menuName="Game/Economy Config")]
public class EconomyConfig : ScriptableObject
{
    [Min(0)] public int coinsPerWin=100;
    [Min(0)] public int startingCoins;
    [Min(0)] public int startingHints=3;
    [Min(0)] public int startingShuffles=3;
    [Min(1)] public int hintPrice=75;
    [Min(1)] public int shufflePrice=100;
    public ShopBackground[] backgrounds=new ShopBackground[0];
}
