using System;
using System.Collections.Generic;
using UnityEngine;

public enum BoosterType { Hint, Shuffle }

[Serializable]
public class PlayerInventory
{
    public int coins;
    public int hints;
    public int shuffles;
    public List<string> ownedBackgrounds=new List<string>();
    public string selectedBackground="garden";
}

public struct LevelReward
{
    public int coins;
    public int hints;
    public int shuffles;
    public string DisplayText()
    {
        string text="+"+coins+" COIN";
        if(hints>0) text+="   +"+hints+" HINT";
        if(shuffles>0) text+="   +"+shuffles+" SHUFFLE";
        return text;
    }
}

public static class EconomyStore
{
    public const string SaveKey="Economy.Profile.v1";
    public static event Action InventoryChanged;
    public static EconomyConfig Config => Resources.Load<EconomyConfig>("EconomyConfig");
    public static PlayerInventory Inventory => Load();

    private static PlayerInventory Load()
    {
        PlayerInventory inventory=null;
        if(PlayerPrefs.HasKey(SaveKey))
        {
            try { inventory=JsonUtility.FromJson<PlayerInventory>(PlayerPrefs.GetString(SaveKey)); }
            catch(ArgumentException) { }
        }
        if(inventory==null)
        {
            var config=Config;
            inventory=new PlayerInventory {coins=config ? config.startingCoins : 0,hints=config ? config.startingHints : 3,shuffles=config ? config.startingShuffles : 3};
            Sanitize(inventory);
            Save(inventory,false);
        }
        else Sanitize(inventory);
        return inventory;
    }
    private static void Sanitize(PlayerInventory inventory)
    {
        inventory.coins=Mathf.Max(0,inventory.coins);
        inventory.hints=Mathf.Max(0,inventory.hints);
        inventory.shuffles=Mathf.Max(0,inventory.shuffles);
        if(inventory.ownedBackgrounds==null) inventory.ownedBackgrounds=new List<string>();
        if(!inventory.ownedBackgrounds.Contains("garden")) inventory.ownedBackgrounds.Add("garden");
        if(!inventory.ownedBackgrounds.Contains(inventory.selectedBackground) || FindBackground(inventory.selectedBackground)==null)
            inventory.selectedBackground="garden";
    }
    private static void Save(PlayerInventory inventory,bool notify=true)
    {
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(inventory)); PlayerPrefs.Save();
        if(notify) InventoryChanged?.Invoke();
    }
    private static int Add(int value,int amount) => (int)Math.Min(int.MaxValue,(long)value+Mathf.Max(0,amount));
#if UNITY_EDITOR
    public static PlayerInventory AdminAdjustInventory(int coins,int hints,int shuffles)
    {
        var inventory=Load();
        inventory.coins=AdminClamp((long)inventory.coins+coins);
        inventory.hints=AdminClamp((long)inventory.hints+hints);
        inventory.shuffles=AdminClamp((long)inventory.shuffles+shuffles);
        Save(inventory);
        return inventory;
    }
    private static int AdminClamp(long value) => (int)Math.Max(0,Math.Min(int.MaxValue,value));
    public static PlayerInventory AdminResetBackgrounds()
    {
        var inventory=Load();
        inventory.ownedBackgrounds.Clear();
        inventory.ownedBackgrounds.Add("garden");
        inventory.selectedBackground="garden";
        Save(inventory);
        return inventory;
    }
#endif
    public static LevelReward RewardWin(int hints,int shuffles)
    {
        var inventory=Load();
        var reward=new LevelReward {coins=Config ? Mathf.Max(0,Config.coinsPerWin) : 100,hints=Mathf.Max(0,hints),shuffles=Mathf.Max(0,shuffles)};
        inventory.coins=Add(inventory.coins,reward.coins);
        inventory.hints=Add(inventory.hints,reward.hints);
        inventory.shuffles=Add(inventory.shuffles,reward.shuffles);
        Save(inventory);
        return reward;
    }
    public static bool TryUseBooster(BoosterType type)
    {
        var inventory=Load();
        if(type==BoosterType.Hint)
        {
            if(inventory.hints==0) return false;
            inventory.hints--;
        }
        else
        {
            if(inventory.shuffles==0) return false;
            inventory.shuffles--;
        }
        Save(inventory); return true;
    }
    public static bool TryBuyBooster(BoosterType type,out string message)
    {
        var inventory=Load(); var config=Config;
        int cost=type==BoosterType.Hint ? (config ? config.hintPrice : 75) : (config ? config.shufflePrice : 100);
        cost=Mathf.Max(1,cost);
        if(inventory.coins<cost) { message="Not enough coins to buy this item."; return false; }
        if((type==BoosterType.Hint ? inventory.hints : inventory.shuffles)==int.MaxValue)
        { message="Your inventory is full."; return false; }
        inventory.coins-=cost;
        if(type==BoosterType.Hint) inventory.hints++; else inventory.shuffles++;
        Save(inventory);
        message=type==BoosterType.Hint ? "Purchased 1 hint!" : "Purchased 1 shuffle!";
        return true;
    }
    public static ShopBackground FindBackground(string id)
    {
        var config=Config;
        if(config && config.backgrounds!=null)
            foreach(var background in config.backgrounds)
                if(background!=null && background.id==id) return background;
        return null;
    }
    public static bool IsBackgroundUnlocked(ShopBackground item) => item!=null && LevelProgress.HighestCompleted>=Mathf.Max(0,item.requiredCompletedLevel);
    public static bool TryBuyBackground(string id,out string message)
    {
        var item=FindBackground(id); var inventory=Load();
        if(item==null || !item.sprite) { message="This background is unavailable."; return false; }
        if(inventory.ownedBackgrounds.Contains(id)) { message="You already own this background."; return false; }
        if(!IsBackgroundUnlocked(item)) { message="Complete level "+item.requiredCompletedLevel+" to unlock this background for purchase."; return false; }
        int cost=Mathf.Max(0,item.price);
        if(inventory.coins<cost) { message="Not enough coins to buy this background."; return false; }
        inventory.coins-=cost; inventory.ownedBackgrounds.Add(id); Save(inventory);
        message="Background purchased! Tap USE to equip it."; return true;
    }
    public static bool TryEquipBackground(string id)
    {
        var inventory=Load(); var item=FindBackground(id);
        if(item==null || !item.sprite || !inventory.ownedBackgrounds.Contains(id)) return false;
        inventory.selectedBackground=id; Save(inventory); return true;
    }
}
