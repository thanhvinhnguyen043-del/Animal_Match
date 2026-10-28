using System;
using System.Globalization;
using UnityEngine;

[Serializable]
public class DailyReward
{
    [Min(0)] public int coins;
    [Min(0)] public int hints;
    [Min(0)] public int shuffles;
}

public struct DailyRewardStatus
{
    public int collected;
    public int day;
    public bool canClaim;
    public DateTime nextClaimAt;
}

public static class DailyRewardSchedule
{
    public const int Days=7;
    public const string DateFormat="yyyy-MM-dd";
    private static readonly DailyReward[] defaults=CreateDefaults();
    public static DailyReward[] CreateDefaults() => new[]
    {
        new DailyReward {coins=100},new DailyReward {hints=1},new DailyReward {coins=150},
        new DailyReward {shuffles=1},new DailyReward {coins=200},new DailyReward {hints=1,shuffles=1},
        new DailyReward {coins=500,hints=2,shuffles=2}
    };
    public static DailyReward Reward(int day)
    {
        var config=EconomyStore.Config;int index=Mathf.Clamp(day-1,0,Days-1);
        return config && config.dailyRewards!=null && config.dailyRewards.Length==Days && config.dailyRewards[index]!=null ? config.dailyRewards[index] : defaults[index];
    }
    public static DailyRewardStatus Status(PlayerInventory inventory,DateTime now)
    {
        bool hasDate=DateTime.TryParseExact(inventory.dailyRewardLastClaimDate,DateFormat,CultureInfo.InvariantCulture,DateTimeStyles.None,out var last);
        bool ready=!hasDate || now.Date>last.Date;
        int collected=Mathf.Clamp(inventory.dailyRewardsClaimed,0,Days);
        if(collected==Days && ready) collected=0;
        return new DailyRewardStatus {collected=collected,day=ready ? collected+1 : Mathf.Max(1,collected),canClaim=ready,
            nextClaimAt=hasDate && last.Date<DateTime.MaxValue.Date ? last.Date.AddDays(1) : DateTime.MaxValue};
    }
}
