using UnityEngine;

public static class AvatarFashionRewards
{
    public static readonly string[] Ids={"fashion-chef","fashion-sakura","fashion-beret","fashion-sailor","fashion-crown","fashion-nightcap"};
    public static readonly string[] Names={"Cafe Chef","Sakura Bow","Coffee Beret","Sailor Cap","Star Crown","Dreamy Nightcap"};
    public static readonly string[] Assets={"Chef","Sakura","Beret","Sailor","Crown","Nightcap"};
    public static readonly string[] Conditions={"Collect check-in gifts on 3 days","Collect check-in gifts on 7 days","Claim the 300-point weekly chest","Claim 120 welcome event points","Earn 45 total stars","Claim the 10-achievement bonus chest"};
    private static DecorationItem[] items;
    private static Material cutout;
    public static void Style(UnityEngine.UI.Image image)
    {
        if(!image) return;
        if(!image.sprite || System.Array.IndexOf(Assets,image.sprite.name)<0) {image.material=null;return;}
        if(!cutout) {var shader=Resources.Load<Shader>("AvatarFashion/FashionCutout");if(!shader) return;cutout=new Material(shader) {hideFlags=HideFlags.HideAndDontSave};}
        image.material=cutout;
    }
    public static DecorationItem[] Items
    {
        get
        {
            if(items==null)
            {
                items=new DecorationItem[Ids.Length];
                for(int i=0;i<items.Length;i++) items[i]=new DecorationItem {id=Ids[i],displayName=Names[i],slot=DecorationSlot.Hats,rewardOnly=true,sprite=Resources.Load<Sprite>("AvatarFashion/"+Assets[i])};
            }
            return items;
        }
    }
    public static DecorationItem Find(string id) {int i=System.Array.IndexOf(Ids,id);return i<0 ? null : Items[i];}
    public static string Source(string id) {int i=System.Array.IndexOf(Ids,id);return i<0 ? "" : Conditions[i];}
    public static bool Reconcile(PlayerInventory p)
    {
        int days=Mathf.Max(p.dailyRewardsClaimed,p.career.counters.dailyClaims);
        bool[] earned={days>=3,days>=7,p.activities.weekly.claimed.Contains("chest2"),p.activities.welcome.claimed.Contains("chest1"),CareerStore.TotalStars(p.career)>=45,p.career.claimedMilestones.Contains(10)};
        bool changed=false;
        for(int i=0;i<Ids.Length;i++) if(earned[i] && !p.ownedDecorations.Contains(Ids[i])) {p.ownedDecorations.Add(Ids[i]);changed=true;}
        return changed;
    }
}
