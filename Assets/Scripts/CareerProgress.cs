using System;
using System.Collections.Generic;
using UnityEngine;

public enum StarGoal { Automatic, Water, Stone, WaterAndStone, Combo }

[Serializable]
public class LevelChallenge
{
    [Range(.05f,.9f)] public float timeRemainingFraction=.3f;
    public StarGoal goal;
    [Min(1)] public int turnLimit=12;
    [Min(2)] public int comboTarget=3;
}

[Serializable]
public class LevelRecord
{
    public string levelId;
    public int stars,bestScore,bestCombo;
    public float bestSeconds;
    public bool hasDetails;
}

[Serializable]
public class CareerCounters
{
    public int wins,matches,water,stone,combo3,combo5,combo8,combo12,bestCombo,dailyClaims;
    public bool HasChanges => wins+matches+water+stone+combo3+combo5+combo8+combo12+dailyClaims>0 || bestCombo>0;
    private static int Sum(int a,int b)=>(int)Math.Min(int.MaxValue,(long)Math.Max(0,a)+Math.Max(0,b));
    public void Add(CareerCounters other)
    {
        wins=Sum(wins,other.wins);matches=Sum(matches,other.matches);water=Sum(water,other.water);stone=Sum(stone,other.stone);
        combo3=Sum(combo3,other.combo3);combo5=Sum(combo5,other.combo5);combo8=Sum(combo8,other.combo8);combo12=Sum(combo12,other.combo12);
        bestCombo=Math.Max(bestCombo,other.bestCombo);dailyClaims=Sum(dailyClaims,other.dailyClaims);
    }
}

[Serializable]
public class CareerProgress
{
    public bool initialized;
    public CareerCounters counters=new CareerCounters();
    public List<LevelRecord> levels=new List<LevelRecord>();
    public List<string> claimedAchievements=new List<string>();
    public List<int> claimedMilestones=new List<int>();
}

public struct LevelRunResult
{
    public int stars,bestStars;
    public bool newScore,newTime,newCombo,moreStars;
}

public static class CareerStore
{
    public static string LevelId(int index,LevelDatabase database=null)
    {
        if(!database) database=Resources.Load<LevelDatabase>("LevelDatabase");
        return database && index>=0 && index<database.levels.Count && !string.IsNullOrEmpty(database.levels[index].id) ? database.levels[index].id : "animal-match-"+(index+1).ToString("000");
    }
    public static LevelRecord Record(CareerProgress career,string id)=>career.levels.Find(r=>r.levelId==id);
    public static int TotalStars(CareerProgress career)
    {
        int total=0;foreach(var record in career.levels) total+=Mathf.Clamp(record.stars,0,3);return total;
    }
    public static int PerfectLevels(CareerProgress career) {int total=0;foreach(var record in career.levels) if(record.stars==3) total++;return total;}
    public static bool Sanitize(PlayerInventory inventory)
    {
        bool changed=false;
        if(inventory.career==null) {inventory.career=new CareerProgress();changed=true;}
        var career=inventory.career;
        if(career.counters==null) {career.counters=new CareerCounters();changed=true;}
        if(career.levels==null) {career.levels=new List<LevelRecord>();changed=true;}
        if(career.claimedAchievements==null) {career.claimedAchievements=new List<string>();changed=true;}
        if(career.claimedMilestones==null) {career.claimedMilestones=new List<int>();changed=true;}
        if(!career.initialized)
        {
            var database=Resources.Load<LevelDatabase>("LevelDatabase");
            int completed=Mathf.Min(LevelProgress.HighestCompleted,database ? database.levels.Count : 0);
            for(int index=0;index<completed;index++)
            {
                string id=LevelId(index,database);
                if(Record(career,id)==null) career.levels.Add(new LevelRecord {levelId=id,stars=1});
            }
            career.counters.wins=Math.Max(career.counters.wins,completed);
            career.counters.dailyClaims=Math.Max(career.counters.dailyClaims,Mathf.Clamp(inventory.dailyRewardsClaimed,0,7));
            career.initialized=true;changed=true;
        }
        return changed;
    }
    public static LevelRunResult Commit(CareerCounters pending,GridManager grid=null,bool won=false)
    {
        var result=new LevelRunResult();
        if(!won && !pending.HasChanges) return result;
        EconomyStore.UpdateCareer(inventory=>
        {
            var career=inventory.career;career.counters.Add(pending);
            if(!won || !grid) return;
            var game=grid.Session;string id=LevelId(grid.levelIndex,grid.levelDatabase);
            var record=Record(career,id);
            if(record==null) {record=new LevelRecord {levelId=id};career.levels.Add(record);}
            result.stars=game.EarnedStars;result.moreStars=result.stars>record.stars;
            result.newScore=!record.hasDetails || game.Score>record.bestScore;
            result.newTime=!record.hasDetails || game.ElapsedSeconds<record.bestSeconds-.001f;
            result.newCombo=!record.hasDetails || game.PeakCombo>record.bestCombo;
            record.stars=Math.Max(record.stars,result.stars);result.bestStars=record.stars;
            record.bestScore=Math.Max(record.bestScore,game.Score);record.bestCombo=Math.Max(record.bestCombo,game.PeakCombo);
            if(result.newTime) record.bestSeconds=game.ElapsedSeconds;
            record.hasDetails=true;
        });
        return result;
    }
    public static bool Claim(string id,out LevelReward reward)
    {
        var item=AchievementCatalog.All.Find(a=>a.id==id);var result=default(LevelReward);bool claimed=false;
        if(item==null) {reward=result;return false;}
        EconomyStore.UpdateCareer(inventory=>
        {
            if(inventory.career.claimedAchievements.Contains(id) || AchievementCatalog.Value(item.metric,inventory)<item.target) return false;
            inventory.career.claimedAchievements.Add(id);result=item.reward;EconomyStore.AddReward(inventory,result);claimed=true;return true;
        });
        reward=result;return claimed;
    }
    public static bool ClaimMilestone(out LevelReward reward)
    {
        var result=default(LevelReward);bool claimed=false;
        EconomyStore.UpdateCareer(inventory=>
        {
            foreach(var milestone in AchievementCatalog.Milestones)
            {
                if(inventory.career.claimedMilestones.Contains(milestone.target)) continue;
                if(inventory.career.claimedAchievements.Count<milestone.target) return false;
                inventory.career.claimedMilestones.Add(milestone.target);result=milestone.reward;
                EconomyStore.AddReward(inventory,result);claimed=true;return true;
            }
            return false;
        });
        reward=result;return claimed;
    }
}

public sealed class ProgressTracker : IDisposable
{
    private readonly GridManager grid;
    private readonly GameManager game;
    private CareerCounters pending=new CareerCounters();
    public ProgressTracker(GridManager owner)
    {
        grid=owner;game=owner.Session;game.MatchResolved+=OnMatch;game.TileBroken+=OnBroken;
        game.LevelStarted+=Flush;game.StateChanged+=OnState;
    }
    private void OnMatch(int points,float time,Vector2 position)
    {
        pending.matches++;pending.bestCombo=Math.Max(pending.bestCombo,game.Combo);
        if(game.Combo==3) pending.combo3++;
        if(game.Combo==5) pending.combo5++;
        if(game.Combo==8) pending.combo8++;
        if(game.Combo==12) pending.combo12++;
    }
    private void OnBroken(TileState state) {if(state==TileState.Ice) pending.water++;else if(state==TileState.Barrier) pending.stone++;}
    private void OnState(GameState state) {if(state==GameState.Menu || state==GameState.Paused) Flush();}
    public void Flush() {if(!pending.HasChanges) return;CareerStore.Commit(pending);pending=new CareerCounters();}
    public LevelRunResult Finish(bool won)
    {
        if(won) pending.wins++;
        var result=CareerStore.Commit(pending,grid,won);pending=new CareerCounters();return result;
    }
    public void Dispose()
    {
        Flush();game.MatchResolved-=OnMatch;game.TileBroken-=OnBroken;game.LevelStarted-=Flush;game.StateChanged-=OnState;
    }
}
