using System.Collections.Generic;
using UnityEngine;

public enum AchievementMetric { Stars, Levels, PerfectLevels, Wins, Matches, Water, Stone, Combo3, Combo5, Combo8, Combo12, BestCombo, Decorations, Backgrounds, DailyClaims }

public sealed class AchievementDefinition
{
    public string id,title,description,category;
    public AchievementMetric metric;
    public int target;
    public LevelReward reward;
}

public sealed class AchievementMilestone
{
    public int target;
    public LevelReward reward;
}

public static class AchievementCatalog
{
    public static readonly List<AchievementDefinition> All=Create();
    public static readonly AchievementMilestone[] Milestones=
    {
        Chest(5,200,1,0),Chest(10,300,0,1),Chest(20,500,2,1),
        Chest(35,750,2,2),Chest(50,1000,3,3),Chest(63,1500,5,5)
    };
    private static AchievementMilestone Chest(int target,int coins,int hints,int shuffles)=>new AchievementMilestone {target=target,reward=new LevelReward {coins=coins,hints=hints,shuffles=shuffles}};
    private static List<AchievementDefinition> Create()
    {
        var list=new List<AchievementDefinition>(63);
        Add(list,AchievementMetric.Stars,"STARS","STAR COLLECTOR","Earn {0} total best stars.",3,10,20,30,45,60);
        Add(list,AchievementMetric.Levels,"STARS","ADVENTURER","Complete {0} different levels.",1,5,10,15,20);
        Add(list,AchievementMetric.PerfectLevels,"STARS","PERFECT FINISH","Get 3 stars on {0} different levels.",1,5,10,20);
        Add(list,AchievementMetric.Wins,"PLAY","VICTORY LAP","Win {0} games. Replays count.",5,20,50,100);
        Add(list,AchievementMetric.Matches,"PLAY","MATCH MAKER","Make {0} successful matches.",25,100,250,500,1000);
        Add(list,AchievementMetric.Water,"PLAY","WATER BREAKER","Break {0} water covers.",5,25,75,150);
        Add(list,AchievementMetric.Stone,"PLAY","STONE BREAKER","Break {0} stone covers.",5,25,75,150);
        Add(list,AchievementMetric.Combo3,"COMBOS","GREAT STREAK","Reach combo x3 in {0} streaks.",1,10,30,100);
        Add(list,AchievementMetric.Combo5,"COMBOS","EXCELLENT STREAK","Reach combo x5 in {0} streaks.",1,5,20,50);
        Add(list,AchievementMetric.Combo8,"COMBOS","AMAZING STREAK","Reach combo x8 in {0} streaks.",1,5,15,30);
        Add(list,AchievementMetric.Combo12,"COMBOS","PERFECT STREAK","Reach combo x12 in {0} streaks.",1,3,10,20);
        Add(list,AchievementMetric.BestCombo,"COMBOS","COMBO MASTER","Reach a best combo of x{0}.",5,10,15,20);
        Add(list,AchievementMetric.Decorations,"COLLECTION","ROOM STYLIST","Own {0} different accessories.",1,5,10,20);
        Add(list,AchievementMetric.Backgrounds,"COLLECTION","CAFE EXPLORER","Own {0} different backgrounds.",3,5,7);
        Add(list,AchievementMetric.DailyClaims,"COLLECTION","DAILY FRIEND","Collect daily gifts on {0} days.",3,7,14,30);
        return list;
    }
    private static void Add(List<AchievementDefinition> list,AchievementMetric metric,string category,string title,string description,params int[] targets)
    {
        for(int index=0;index<targets.Length;index++)
            list.Add(new AchievementDefinition {id=metric.ToString().ToLowerInvariant()+"-"+targets[index],metric=metric,target=targets[index],category=category,
                title=title+" "+(index+1),description=string.Format(description,targets[index]),
                reward=new LevelReward {coins=50*(index+1),hints=index%2==1 ? 1 : 0,shuffles=index>=2 ? 1 : 0}});
    }
    public static int Value(AchievementMetric metric,PlayerInventory inventory)
    {
        var career=inventory.career;var stats=career.counters;
        switch(metric)
        {
            case AchievementMetric.Stars:return CareerStore.TotalStars(career);
            case AchievementMetric.Levels:return career.levels.Count;
            case AchievementMetric.PerfectLevels:return CareerStore.PerfectLevels(career);
            case AchievementMetric.Wins:return stats.wins;
            case AchievementMetric.Matches:return stats.matches;
            case AchievementMetric.Water:return stats.water;
            case AchievementMetric.Stone:return stats.stone;
            case AchievementMetric.Combo3:return stats.combo3;
            case AchievementMetric.Combo5:return stats.combo5;
            case AchievementMetric.Combo8:return stats.combo8;
            case AchievementMetric.Combo12:return stats.combo12;
            case AchievementMetric.BestCombo:return stats.bestCombo;
            case AchievementMetric.Decorations:return inventory.ownedDecorations.Count;
            case AchievementMetric.Backgrounds:return inventory.ownedBackgrounds.Count;
            default:return stats.dailyClaims;
        }
    }
}
