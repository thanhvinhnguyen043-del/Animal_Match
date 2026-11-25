using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAvatarProfile
{
    public string selected="fox",hat="";
    public int rosterVersion;
    public List<string> unlocked=new List<string> {"fox"};
}

public static class PlayerAvatarStore
{
    public static readonly string[] Ids={"fox","cat","bunny","bear","panda"};
    public static readonly string[] Names={"Sunny Fox","Cafe Cat","Blossom Bunny","Honey Bear","Cocoa Panda"};
    public static readonly int[] RequiredLevels={0,-1,-1,-1,-1};
    public static readonly string[] UnlockConditions={"Available from the start","Claim Day 7 check-in","Claim all 210 welcome event points","Earn 30 total stars","Complete levels 1-20"};
    public static string Condition(string id) {int i=Index(id);return i<0 ? "LOCKED" : UnlockConditions[i];}
    public static int Index(string id)=>Array.IndexOf(Ids,id);
    public static bool Reconcile(PlayerInventory inventory,int completed)
    {
        bool changed=false;
        if(inventory.avatar==null) {inventory.avatar=new PlayerAvatarProfile();changed=true;}
        var profile=inventory.avatar;
        if(profile.unlocked==null) {profile.unlocked=new List<string>();changed=true;}
        if(profile.rosterVersion<2)
        {
            profile.unlocked.RemoveAll(id=>id!="fox");profile.rosterVersion=2;changed=true;
        }
        if(profile.unlocked.RemoveAll(id=>Index(id)<0)>0) changed=true;
        for(int i=0;i<Ids.Length;i++) if(RequiredLevels[i]>=0 && completed>=RequiredLevels[i] && !profile.unlocked.Contains(Ids[i])) {profile.unlocked.Add(Ids[i]);changed=true;}
        bool[] earned={true,inventory.dailyRewardsClaimed>=7 || (inventory.career!=null && inventory.career.counters!=null && inventory.career.counters.dailyClaims>=7),(inventory.activities!=null && inventory.activities.welcome!=null && inventory.activities.welcome.claimed!=null && inventory.activities.welcome.claimed.Contains("chest2")),(inventory.career!=null && inventory.career.levels!=null && CareerStore.TotalStars(inventory.career)>=30),RoomProgress.CompletedChapters(inventory)>=20};
        for(int i=0;i<Ids.Length;i++) if(earned[i] && !profile.unlocked.Contains(Ids[i])) {profile.unlocked.Add(Ids[i]);changed=true;}
        if(Index(profile.selected)<0 || !profile.unlocked.Contains(profile.selected)) {profile.selected="fox";changed=true;}
        if(profile.hat==null) {profile.hat="";changed=true;}
        if(profile.hat.Length>0 && !CanWear(inventory,profile.hat)) {profile.hat="";changed=true;}
        return changed;
    }
    public static bool CanWear(PlayerInventory inventory,string hat)
    {
        if(string.IsNullOrEmpty(hat)) return true;
        var item=EconomyStore.FindDecoration(hat);
        return item!=null && item.sprite && item.slot==DecorationSlot.Hats && inventory.ownedDecorations.Contains(hat);
    }
    public static bool Save(string id,string hat)
    {
        bool saved=false;
        EconomyStore.UpdateCareer(p=>
        {
            if(Index(id)<0 || !p.avatar.unlocked.Contains(id) || !CanWear(p,hat)) return false;
            p.avatar.selected=id;p.avatar.hat=hat ?? "";saved=true;return true;
        });
        return saved;
    }
}
