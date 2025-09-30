using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using QuickCmd;

public class LobbyTest : MonoBehaviour
{
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    
    //로비목록 업데이트 감지 이벤트
    public event EventHandler<List<Lobby>> OnLobbyListChanged; 
    private async void Start()
    {
        try
        {
            //익명로그인
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartbeatTimer+=Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;
                //이 로비 아직 살아있습니다
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                Debug.Log("Sending heartbeat ping to " + hostLobby.Id);
            }
        }
    }

    [Command]
    public async void CreateLobby(string lobbyName, bool isPrivate, int maxPlayer)
    {
        try
        {
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                //호스트정보
                Player = GetPlayer(),
                //대기열에 보여줄 로비 정보
                Data= new Dictionary<string, DataObject>
                {
                    //방 이름
                    {"RoomName", new DataObject(DataObject.VisibilityOptions.Public, lobbyName)},
                    //Max 플레이어 수
                    {"MaxPlayerCount", new DataObject(DataObject.VisibilityOptions.Public, maxPlayer.ToString())}
                }
            };
            
            //로비생성 요청
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptions);
            
            hostLobby = lobby;
            joinedLobby = lobby;
            
            Debug.Log($"Lobby Created! {lobby.Name}, {lobby.MaxPlayers}, {lobby.LobbyCode}");
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void ListLobbies()
    {
        try
        {
            //TODO: 검색 상세조건 기획 나올 시 QueryLobbiesAsync의 인자로 추가하기
            //QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log($"Lobbies found: {queryResponse.Results.Count}");
            //로비목록 UI를 업데이트하기위해 이벤트 invoke
            OnLobbyListChanged?.Invoke(this, queryResponse.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    
    [Command]
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            // 참여 시 나의 플레이어 정보를 설정하는 옵션.
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = _joinedLobby;
            
            Debug.Log($"Joined Lobby with code: {lobbyCode}");
            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private Player GetPlayer()
    {
        string playerId = AuthenticationService.Instance.PlayerId;
        //로비시스템의 플레이어객체임. 인게임 플레이어 객체와 혼동x
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerId) }
            }
        };
    }

    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log($"Players in Lobby: {lobby.Name}, {lobby.MaxPlayers}, {lobby.LobbyCode}");
        foreach (var player in lobby.Players)
        {
            Debug.Log($"Player: {player.Data["PlayerName"].Value}");
        }
    }
}
