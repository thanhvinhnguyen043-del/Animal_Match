using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;

public sealed class OnlineLeaderboard
{
    public const string BoardId="animal-match-001";
    private const string PendingKey="Leaderboard.Pending.v1";
    public static readonly OnlineLeaderboard Instance=new OnlineLeaderboard();
    [Serializable] private class Pending {public int score;public bool hasScore;public string playerId;}
    private Task connection;
    private bool uploading;
    public string LastStatus {get;private set;}="";
    public bool IsUploading=>uploading;
    public async Task Connect()
    {
        if(connection==null) connection=Initialize();
        try {await connection;} catch {connection=null;throw;}
    }
    private async Task Initialize()
    {
        if(UnityServices.State!=ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName("production"));
        if(!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    public void QueueWin(string levelId,int score)
    {
        if(levelId!=BoardId || score<0) return;
        var pending=Load();pending.score=pending.hasScore ? Math.Max(pending.score,score) : score;pending.hasScore=true;Save(pending);
        _=Flush();
    }
    public async Task Flush()
    {
        if(uploading) return;uploading=true;
        try
        {
            var pending=Load();if(!pending.hasScore) return;
            await Connect();string player=AuthenticationService.Instance.PlayerId;
            pending=Load();
            if(!string.IsNullOrEmpty(pending.playerId) && pending.playerId!=player)
            {LastStatus="Pending score belongs to another account.";return;}
            while(pending.hasScore)
            {
                pending.playerId=player;Save(pending);int submitted=pending.score;
                await LeaderboardsService.Instance.AddPlayerScoreAsync(BoardId,submitted);
                pending=Load();if(pending.score<=submitted) {pending.hasScore=false;Save(pending);}
            }
            LastStatus="Best score synced.";
        }
        catch(Exception) {LastStatus="Score saved on this device. Open Leaderboard to retry syncing.";}
        finally {uploading=false;}
    }
    private static Pending Load()
    {
        try {return JsonUtility.FromJson<Pending>(PlayerPrefs.GetString(PendingKey,"{}")) ?? new Pending();}
        catch {return new Pending();}
    }
    private static void Save(Pending pending) {PlayerPrefs.SetString(PendingKey,JsonUtility.ToJson(pending));PlayerPrefs.Save();}
}
