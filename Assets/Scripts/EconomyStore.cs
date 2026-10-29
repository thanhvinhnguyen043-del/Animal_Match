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
    public string selectedBackground=EconomyStore.DefaultBackgroundId;
    public List<string> ownedDecorations=new List<string>();
    public List<RoomLayout> roomLayouts=new List<RoomLayout>();
    public int dailyRewardsClaimed;
    public string dailyRewardLastClaimDate;
    public CareerProgress career=new CareerProgress();
    public List<string> unlockedRoomRewards=new List<string>();
    public List<string> newRoomRewards=new List<string>();
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
    public const string DefaultBackgroundId="cafe";
    public static event Action InventoryChanged;
    private static EconomyConfig config;
    public static EconomyConfig Config => config ? config : config=Resources.Load<EconomyConfig>("EconomyConfig");
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
        else if(Sanitize(inventory)) Save(inventory,false);
        return inventory;
    }
    private static bool Sanitize(PlayerInventory inventory)
    {
        bool changed=inventory.coins<0 || inventory.hints<0 || inventory.shuffles<0;
        inventory.coins=Mathf.Max(0,inventory.coins);
        inventory.hints=Mathf.Max(0,inventory.hints);
        inventory.shuffles=Mathf.Max(0,inventory.shuffles);
        if(inventory.ownedBackgrounds==null) { inventory.ownedBackgrounds=new List<string>(); changed=true; }
        if(inventory.ownedDecorations==null) { inventory.ownedDecorations=new List<string>(); changed=true; }
        if(inventory.roomLayouts==null) { inventory.roomLayouts=new List<RoomLayout>(); changed=true; }
        for(int index=inventory.ownedBackgrounds.Count-1;index>=0;index--)
            if(inventory.ownedBackgrounds[index]=="garden" || inventory.ownedBackgrounds[index]=="sunset")
            { inventory.ownedBackgrounds.RemoveAt(index); changed=true; }
        if(!inventory.ownedBackgrounds.Contains(DefaultBackgroundId))
        { inventory.ownedBackgrounds.Add(DefaultBackgroundId); changed=true; }
        if(!inventory.ownedBackgrounds.Contains(inventory.selectedBackground) || FindBackground(inventory.selectedBackground)==null)
        { inventory.selectedBackground=DefaultBackgroundId; changed=true; }
        changed=CareerStore.Sanitize(inventory) || changed;
        return RoomProgress.Reconcile(inventory) || changed;
    }
    private static void Save(PlayerInventory inventory,bool notify=true)
    {
        RoomProgress.Reconcile(inventory);
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(inventory)); PlayerPrefs.Save();
        if(notify) InventoryChanged?.Invoke();
    }
    private static int Add(int value,int amount) => (int)Math.Min(int.MaxValue,(long)value+Mathf.Max(0,amount));
    internal static void AddReward(PlayerInventory inventory,LevelReward reward)
    {
        inventory.coins=Add(inventory.coins,reward.coins);inventory.hints=Add(inventory.hints,reward.hints);inventory.shuffles=Add(inventory.shuffles,reward.shuffles);
    }
    internal static void UpdateCareer(Action<PlayerInventory> change) {var inventory=Load();change(inventory);Save(inventory);}
    internal static void UpdateCareer(Func<PlayerInventory,bool> change) {var inventory=Load();if(change(inventory)) Save(inventory);}
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
        inventory.ownedBackgrounds.Add(DefaultBackgroundId);
        inventory.selectedBackground=DefaultBackgroundId;
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
    public static DailyRewardStatus DailyStatus => DailyRewardSchedule.Status(Load(),DateTime.Now);
    public static bool TryClaimDailyReward(out LevelReward reward,out string message) => ClaimDailyReward(DateTime.Now,out reward,out message);
    private static bool ClaimDailyReward(DateTime now,out LevelReward reward,out string message)
    {
        var inventory=Load();var status=DailyRewardSchedule.Status(inventory,now);reward=default;
        if(!status.canClaim) {message="Today's reward is already collected. Come back on the next day!";return false;}
        var item=DailyRewardSchedule.Reward(status.day);
        reward=new LevelReward {coins=Mathf.Max(0,item.coins),hints=Mathf.Max(0,item.hints),shuffles=Mathf.Max(0,item.shuffles)};
        inventory.coins=Add(inventory.coins,reward.coins);inventory.hints=Add(inventory.hints,reward.hints);inventory.shuffles=Add(inventory.shuffles,reward.shuffles);
        inventory.dailyRewardsClaimed=status.day;inventory.dailyRewardLastClaimDate=now.ToString(DailyRewardSchedule.DateFormat,System.Globalization.CultureInfo.InvariantCulture);
        inventory.career.counters.dailyClaims++;
        Save(inventory);message="Day "+status.day+" collected! "+reward.DisplayText();return true;
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
        if(item.rewardOnly) {message=RoomProgress.SourceForBackground(id);return false;}
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
    public static DecorationItem FindDecoration(string id)
    {
        var config=Config;
        if(config && config.decorations!=null)
            foreach(var item in config.decorations) if(item!=null && item.id==id) return item;
        return null;
    }
    public static bool TryBuyDecoration(string id,out string message)
    {
        var item=FindDecoration(id);var inventory=Load();
        if(item==null || !item.sprite) {message="This decoration is unavailable.";return false;}
        if(inventory.ownedDecorations.Contains(id)) {message="Already in your inventory.";return false;}
        if(item.rewardOnly) {message=RoomProgress.SourceForDecoration(id);return false;}
        int cost=Mathf.Max(0,item.price);
        if(inventory.coins<cost) {message="Not enough coins.";return false;}
        inventory.coins-=cost;inventory.ownedDecorations.Add(id);Save(inventory);
        message="Purchased! Open DECORATE to place it.";return true;
    }
    public static RoomLayout GetRoomLayout(string backgroundId)
    {
        return GetRoomLayout(Load(),backgroundId);
    }
    public static RoomLayout GetRoomLayout(PlayerInventory inventory,string backgroundId)
    {
        var result=new RoomLayout {backgroundId=backgroundId};
        var saved=inventory.roomLayouts.Find(r=>r!=null && r.backgroundId==backgroundId);
        if(saved!=null && saved.items!=null)
            foreach(var item in saved.items)
            {
                var definition=item==null ? null : FindDecoration(item.itemId);
                if(definition==null || !definition.sprite || definition.slot==DecorationSlot.None || !inventory.ownedDecorations.Contains(item.itemId)) continue;
                result.items.RemoveAll(p=>FindDecoration(p.itemId).slot==definition.slot);
                result.items.Add(SlotPlacement(item.itemId,backgroundId));
            }
        return result;
    }
    public static DecorationPlacement SlotPlacement(string id,string backgroundId)
    {
        var definition=FindDecoration(id);var anchor=DecorationSlots.Anchor(backgroundId,definition.slot);
        return new DecorationPlacement {itemId=id,x=anchor.position.x,y=anchor.position.y,scale=1,flipped=false};
    }
    public static bool SaveRoomLayout(RoomLayout layout)
    {
        return layout!=null && SaveDecorationRooms(null,new[]{layout});
    }
    public static bool SaveDecorationRooms(string selectedBackground,IEnumerable<RoomLayout> layouts)
    {
        if(layouts==null) return false;
        var inventory=Load();
        if(selectedBackground!=null && (!inventory.ownedBackgrounds.Contains(selectedBackground) || FindBackground(selectedBackground)==null)) return false;
        var clean=new List<RoomLayout>();var ids=new HashSet<string>();
        foreach(var layout in layouts)
        {
            var room=CleanRoomLayout(inventory,layout);if(room==null || !ids.Add(room.backgroundId)) return false;clean.Add(room);
        }
        foreach(var room in clean) {inventory.roomLayouts.RemoveAll(r=>r!=null && r.backgroundId==room.backgroundId);inventory.roomLayouts.Add(room);}
        if(selectedBackground!=null) inventory.selectedBackground=selectedBackground;
        Save(inventory);return true;
    }
    private static RoomLayout CleanRoomLayout(PlayerInventory inventory,RoomLayout layout)
    {
        if(layout==null || !inventory.ownedBackgrounds.Contains(layout.backgroundId) || FindBackground(layout.backgroundId)==null) return null;
        var clean=new RoomLayout {backgroundId=layout.backgroundId};
        if(layout.items!=null) foreach(var item in layout.items)
        {
            var definition=item==null ? null : FindDecoration(item.itemId);
            if(definition==null || !definition.sprite || definition.slot==DecorationSlot.None || !inventory.ownedDecorations.Contains(item.itemId) ||
                clean.items.Exists(p=>FindDecoration(p.itemId).slot==definition.slot)) return null;
            clean.items.Add(SlotPlacement(item.itemId,layout.backgroundId));
        }
        return clean;
    }
}
