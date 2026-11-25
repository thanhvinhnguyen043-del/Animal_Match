using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class ActivityPeriod
{
    public string key;
    public int[] baseline;
    public List<string> claimed=new List<string>();
}

[Serializable]
public class ActivityProgress
{
    public string firstDay,lastDay;
    public int hintsUsed,decorationsEquipped;
    public ActivityPeriod daily=new ActivityPeriod(),weekly=new ActivityPeriod(),welcome=new ActivityPeriod();
}

public class ActivityTask
{
    public string id,title,icon;
    public int metric,target,points,day;
    public LevelReward reward;
}

public static class ActivityStore
{
    private const string Format="yyyy-MM-dd";
    private static string Key(DateTime day)=>day.ToString(Format,CultureInfo.InvariantCulture);
    private static int[] Snapshot(PlayerInventory p)=>new[]{p.career.counters.wins,p.career.counters.matches,p.career.counters.water,p.career.counters.combo5,CareerStore.TotalStars(p.career),p.activities.hintsUsed,p.activities.decorationsEquipped};
    private static DateTime Parse(string key,DateTime fallback)=>DateTime.TryParseExact(key,Format,CultureInfo.InvariantCulture,DateTimeStyles.None,out var day) ? day : fallback;
    public static DateTime Today(PlayerInventory p)=>Parse(p.activities.lastDay,DateTime.Today);
    public static bool Touch(PlayerInventory p,DateTime now)
    {
        bool changed=false;
        if(p.activities==null) {p.activities=new ActivityProgress();changed=true;}
        var a=p.activities;var day=now.Date;
        if(string.IsNullOrEmpty(a.firstDay)) {a.firstDay=Key(day);changed=true;}
        var last=Parse(a.lastDay,day);if(day<last) day=last;
        if(a.lastDay!=Key(day)) {a.lastDay=Key(day);changed=true;}
        if(a.daily==null) {a.daily=new ActivityPeriod();changed=true;}
        if(a.weekly==null) {a.weekly=new ActivityPeriod();changed=true;}
        if(a.welcome==null) {a.welcome=new ActivityPeriod();changed=true;}
        var monday=day.AddDays(-(((int)day.DayOfWeek+6)%7));
        changed=Reset(a.daily,Key(day),p)||changed;changed=Reset(a.weekly,Key(monday),p)||changed;
        changed=Reset(a.welcome,a.firstDay,p)||changed;return changed;
    }
    private static bool Reset(ActivityPeriod period,string key,PlayerInventory p)
    {
        if(period.key==key && period.baseline!=null && period.baseline.Length==7 && period.claimed!=null) return false;
        period.key=key;period.baseline=Snapshot(p);period.claimed=new List<string>();return true;
    }
    public static int WelcomeDay(PlayerInventory p)=>(Today(p)-Parse(p.activities.firstDay,Today(p))).Days+1;
    public static bool WelcomeActive(PlayerInventory p)=>WelcomeDay(p)<=7;
    public static ActivityPeriod Period(PlayerInventory p,int tab)=>tab==0 ? p.activities.daily : tab==1 ? p.activities.weekly : p.activities.welcome;
    private static ActivityTask Task(string id,string title,string icon,int metric,int target,int coins,int points=25,int day=1)=>new ActivityTask {id=id,title=title,icon=icon,metric=metric,target=target,points=points,day=day,reward=new LevelReward {coins=coins}};
    public static List<ActivityTask> Tasks(int tab,int day=1)
    {
        if(tab==0) return new List<ActivityTask> {Task("visit","Visit your cafe","Daily",-1,1,50),Task("pairs","Match 40 pairs","Matches",1,40,60),Task("water","Clear 8 water tiles","Water",2,8,75),Task("wins","Win 3 levels","Levels",0,3,80)};
        if(tab==1) return new List<ActivityTask> {Task("wins","Win 12 levels","Levels",0,12,120,75),Task("pairs","Match 200 pairs","Matches",1,200,150,75),Task("decorate","Equip 3 decorations","Decorations",6,3,100,75),Task("combo","Reach combo x5 ten times","Combo",3,10,120,75)};
        int extra=day%2==0 ? day/2 : (day+1)/2;
        return new List<ActivityTask> {Task("day"+day+"-wins","Win "+day+(day==1 ? " level" : " levels"),"Levels",0,day,50+day*10,10,day),Task("day"+day+"-pairs","Match "+(day*20)+" pairs","Matches",1,day*20,40+day*10,10,day),Task("day"+day+"-extra",day%2==0 ? "Equip "+extra+(extra==1 ? " decoration" : " decorations") : "Use "+extra+(extra==1 ? " hint" : " hints"),day%2==0 ? "Decorations" : "Hint",day%2==0 ? 6 : 5,extra,60,10,day)};
    }
    public static int Value(PlayerInventory p,int tab,ActivityTask task)
    {
        if(task.metric<0) return 1;var values=Snapshot(p);var baseline=Period(p,tab).baseline;
        return Mathf.Max(0,values[task.metric]-baseline[task.metric]);
    }
    public static int Points(PlayerInventory p,int tab)
    {
        int points=0;var period=Period(p,tab);
        for(int day=1;day<=(tab==2 ? 7 : 1);day++) foreach(var task in Tasks(tab,day)) if(period.claimed.Contains(task.id)) points+=task.points;
        return points;
    }
    public static bool CanClaim(PlayerInventory p,int tab,ActivityTask task)=>!Period(p,tab).claimed.Contains(task.id) && (tab!=2 || WelcomeActive(p) && task.day<=WelcomeDay(p)) && Value(p,tab,task)>=task.target;
    public static bool Claim(int tab,string id,int day=1)
    {
        bool success=false;var task=Tasks(tab,day).Find(t=>t.id==id);if(task==null) return false;
        EconomyStore.UpdateCareer(p=>{if(!CanClaim(p,tab,task)) return false;Period(p,tab).claimed.Add(id);EconomyStore.AddReward(p,task.reward);success=true;return true;});return success;
    }
    public static int[] Milestones(int tab)=>tab==0 ? new[]{25,50,100} : tab==1 ? new[]{75,150,300} : new[]{60,120,210};
    public static LevelReward MilestoneReward(int tab,int index)=>new LevelReward {coins=index==2 ? (tab==0 ? 150 : 300) : 50*(index+1),hints=index>=1 ? 1 : 0,shuffles=index==2 && tab!=0 ? 1 : 0};
    public static bool ClaimMilestone(int tab,int index)
    {
        if(index<0 || index>2) return false;bool success=false;
        EconomyStore.UpdateCareer(p=>{var period=Period(p,tab);string id="chest"+index;if(period.claimed.Contains(id) || Points(p,tab)<Milestones(tab)[index] || tab==2 && !WelcomeActive(p)) return false;period.claimed.Add(id);EconomyStore.AddReward(p,MilestoneReward(tab,index));success=true;return true;});return success;
    }
    public static bool ClaimGift()
    {
        bool success=false;EconomyStore.UpdateCareer(p=>{if(p.activities.daily.claimed.Contains("freegift")) return false;p.activities.daily.claimed.Add("freegift");EconomyStore.AddReward(p,new LevelReward {coins=50});success=true;return true;});return success;
    }
    public static TimeSpan Remaining(PlayerInventory p,int tab,DateTime now)
    {
        var today=Today(p);var end=tab==2 ? Parse(p.activities.firstDay,today).AddDays(7) : tab==1 ? Parse(p.activities.weekly.key,today).AddDays(7) : today.AddDays(1);
        return end>now ? end-now : TimeSpan.Zero;
    }
}
