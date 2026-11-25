using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoomRewardGoal { Chapters, Stars, Collections }

[Serializable]
public class RoomCollection
{
    public string id,displayName;
    public string[] itemIds=new string[0];
}

[Serializable]
public class RoomRewardDefinition
{
    public string id,title,backgroundId,decorationId;
    public RoomRewardGoal goal;
    [Min(1)] public int target=1;
    public string Condition => goal==RoomRewardGoal.Chapters ? "Complete levels 1-"+target : goal==RoomRewardGoal.Stars ? "Earn "+target+" total stars" : "Complete "+target+" accessory collection"+(target==1 ? "" : "s");
    public Sprite Sprite
    {
        get
        {
            if(!string.IsNullOrEmpty(backgroundId)) {var room=EconomyStore.FindBackground(backgroundId);return room==null ? null : room.baseSprite ? room.baseSprite : room.sprite;}
            return EconomyStore.FindDecoration(decorationId)?.sprite;
        }
    }
}

public static class RoomProgress
{
    public static int CollectionCount(RoomCollection collection,PlayerInventory inventory)
    {
        int count=0;if(collection.itemIds==null) return count;
        foreach(var id in collection.itemIds) if(inventory.ownedDecorations.Contains(id)) count++;
        return count;
    }
    public static int CompletedCollections(PlayerInventory inventory)
    {
        int count=0;var config=EconomyStore.Config;if(!config || config.roomCollections==null) return count;
        foreach(var collection in config.roomCollections)
            if(collection!=null && collection.itemIds!=null && collection.itemIds.Length>0 && CollectionCount(collection,inventory)==collection.itemIds.Length) count++;
        return count;
    }
    public static int CompletedChapters(PlayerInventory inventory)
    {
        var database=Resources.Load<LevelDatabase>("LevelDatabase");if(!database || inventory.career==null) return 0;
        int completed=0;
        for(int i=0;i<database.levels.Count;i++)
        {
            var record=CareerStore.Record(inventory.career,CareerStore.LevelId(i,database));
            if(record==null || record.stars<1) break;
            completed++;
        }
        return completed;
    }
    public static int Value(RoomRewardGoal goal,PlayerInventory inventory)=>goal==RoomRewardGoal.Chapters ? CompletedChapters(inventory) : goal==RoomRewardGoal.Stars ? CareerStore.TotalStars(inventory.career) : CompletedCollections(inventory);
    public static bool Reconcile(PlayerInventory inventory)
    {
        bool changed=false;
        if(inventory.unlockedRoomRewards==null) {inventory.unlockedRoomRewards=new List<string>();changed=true;}
        if(inventory.newRoomRewards==null) {inventory.newRoomRewards=new List<string>();changed=true;}
        var config=EconomyStore.Config;if(!config || config.roomRewards==null || inventory.career==null) return changed;
        int chapters=CompletedChapters(inventory),stars=CareerStore.TotalStars(inventory.career);
        for(int pass=0;pass<2;pass++)
        {
            int collections=CompletedCollections(inventory);
            foreach(var reward in config.roomRewards)
            {
                if(reward==null || string.IsNullOrEmpty(reward.id) || inventory.unlockedRoomRewards.Contains(reward.id)) continue;
                int value=reward.goal==RoomRewardGoal.Chapters ? chapters : reward.goal==RoomRewardGoal.Stars ? stars : collections;
                if(value<reward.target) continue;
                var backgrounds=inventory.ownedBackgrounds;var items=inventory.ownedDecorations;
                string id=string.IsNullOrEmpty(reward.backgroundId) ? reward.decorationId : reward.backgroundId;
                if(string.IsNullOrEmpty(id) || (string.IsNullOrEmpty(reward.backgroundId) ? EconomyStore.FindDecoration(id)==null : EconomyStore.FindBackground(id)==null)) continue;
                var owned=string.IsNullOrEmpty(reward.backgroundId) ? items : backgrounds;
                if(!owned.Contains(id)) {owned.Add(id);if(!inventory.newRoomRewards.Contains(reward.id)) inventory.newRoomRewards.Add(reward.id);}
                inventory.unlockedRoomRewards.Add(reward.id);changed=true;
            }
        }
        return changed;
    }
    private static string Source(string id,bool background)
    {
        var config=EconomyStore.Config;
        if(config && config.roomRewards!=null) foreach(var reward in config.roomRewards)
            if(reward!=null && (background ? reward.backgroundId : reward.decorationId)==id) return reward.Condition+" to receive this reward.";
        return "Earn this special reward through play.";
    }
    public static string SourceForBackground(string id)=>Source(id,true);
    public static string SourceForDecoration(string id)=>AvatarFashionRewards.Find(id)!=null ? AvatarFashionRewards.Source(id) : Source(id,false);
    public static void MarkSeen()=>EconomyStore.UpdateCareer(inventory=>
    {if(inventory.newRoomRewards.Count==0) return false;inventory.newRoomRewards.Clear();return true;});
}
