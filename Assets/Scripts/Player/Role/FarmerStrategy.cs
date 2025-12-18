using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using UnityEngine.PlayerLoop;

/// <summary>
/// 농장주 역할 전략
/// Kill, Sabotage 등의 Ability를 다형성으로 구현
/// </summary>
public class FarmerStrategy : NetworkBehaviour, IRoleStrategy
{
    // UI용 이벤트
    public event Action<bool, ulong> OnCanKillUIResultReceived;
    public event Action OnKillSuccess;
    
    private PlayerPresenter _playerPresenter;
    private PlayerModel _playerModel;
    private PlayerView _playerView;
    private PlayerInput _playerInput;
    private InputActionMap _farmerActionMap;
    private InputActionMap _commonActionMap;
    
    
    [SerializeField] private float killCooltimeMax = 20f;
    private float killCooltimer = 0f;
    private bool canKill = false;
    

    
    public void Initialize(PlayerModel playerModel, PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerModel = playerModel;
        _playerView = playerModel.GetComponent<PlayerView>();
        _playerPresenter = playerPresenter;
        _playerInput = playerInput;
    }

    
    public void Setup()
    {
        // Action Map 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        _farmerActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Farmer);
        
        if (_commonActionMap != null) _commonActionMap.Enable();
        if (_farmerActionMap != null) _farmerActionMap.Enable();
    }
   
    
    public void Update()
    {
        // 농장주 전용 업데이트 로직
        // 예: 특별한 효과, 타이머 등
        killCooltimer+= Time.deltaTime;
        if(killCooltimer >= killCooltimeMax)
        {
            killCooltimer = 0f;
            canKill = true;
        }
    }
    
    // 0. 외부 인터페이스
    public void Kill(ulong targetNetworkObjectId)
    {
        CanKillServerRpc(targetNetworkObjectId);
    }

    // 1. Can으로 조건검사: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void CanKillServerRpc(ulong targetNetworkObjectId, bool checkForUI = false, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        PlayerModel targetPlayerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(targetNetworkObjectId);
    
        bool result = false;
    
        if (targetPlayerModel.GetPlayerJob() == PlayerJob.Farmer)
        {
            result = false;
        }
        else if (canKill==false)
        {
            result = false;
        }
        else if (targetPlayerModel.GetPlayerAliveState() != PlayerLivingState.Alive)
        {
            result = false;
        }
        else
        {
            result = true;
        }
    
        CanKillResultClientRpc(result, targetNetworkObjectId, checkForUI, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }
    
    

    // 2. 결과를 전송: ClientRpc
    [ClientRpc]
    public void CanKillResultClientRpc(bool canKill, ulong targetNetworkObjectId, bool checkForUI = false, ClientRpcParams rpcParams = default)
    {
        OnCanKillUIResultReceived?.Invoke(canKill, targetNetworkObjectId);
    
        if (canKill == false) return;
        if (checkForUI == true) return;
    
        KillServerRpc(targetNetworkObjectId);
    }


    // 3. 실제 작업 수행: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void KillServerRpc(ulong targetNetworkObjectId)
    {
        PlayerModel targetPlayerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(targetNetworkObjectId);
        targetPlayerModel.HandlePlayerDeathServerRpc();
        
        //TODO: SkillButton한테 Kill성공했다고 invoke
        OnKillSuccess?.Invoke();
        ConsumeKillCooldownClientRpc();
    }
    
    
    public void Cleanup()
    {
        // 입력 이벤트 구독 해제
        if (_farmerActionMap != null)
        {
            _farmerActionMap.Disable();
        }
        
        if (_commonActionMap != null)
        {
            _commonActionMap.Disable();
        }
    }
    
    #region Ability 구현 (다형성)

    public void TryVent()
    {
        
    }
   


  
    [ClientRpc]
    private void ConsumeKillCooldownClientRpc(){
        killCooltimer = 0f;
        canKill = false;
    }

    public void Savotage()
    {
        CanSavotageServerRpc();
    }


    [ServerRpc(RequireOwnership = false)]
    public void SavotageServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("사보타지 아직 미구현");
    }

    public void Interact(string targetTag)
    {
        CanInteractServerRpc(targetTag);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CanInteractServerRpc(string targetTag, ServerRpcParams rpcParams = default)
    {
        bool result = false;
        //인터랙트 가능한 태그가 아니면 fasle
        if (targetTag == GameTags.Vent || targetTag == GameTags.Interactable ||
            targetTag == GameTags.ConvocationOfTrial || targetTag == GameTags.MiniGame)
        {
            result = true;
        }
        
        
        // 요청한 클라이언트 ID 가져오기
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
    
        // 해당 클라이언트에게만 결과 전송
        CanInteractResultClientRpc(result, targetTag, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
        
    }

    [ClientRpc]
    public void CanInteractResultClientRpc(bool canInteract, string targetTag, ClientRpcParams rpcParams = default)
    {
        if (!canInteract) return;
    
        InteractServerRpc(targetTag);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc(string targetTag, ServerRpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        switch (targetTag)
        {
            //벤트
            case GameTags.Vent:
                //벤트타기
                TryVent();
                break;
            //미니게임
            case  GameTags.MiniGame:
                //미니게임 상호작용
                
                break;
            //재판소집
            case  GameTags.ConvocationOfTrial:
                //재판소집
                TrialManager.Instance.TryTrialServerRpc(sender);
                break;
        }
    }


    public void ReportCorpse(ulong targetNetworkObjectId)
    {
        CanReportServerRpc(targetNetworkObjectId);
    }
    

    
    
    public bool CanInteract()
    {
        // 모든 역할이 상호작용 가능
        return true;
    }
    

    

    // 1. Can으로 조건검사: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void CanReportServerRpc(ulong corpseNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        CanReportResultClientRpc(true, corpseNetworkObjectId, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    // 2. 결과를 전송: ClientRpc
    [ClientRpc]
    public void CanReportResultClientRpc(bool canReport, ulong corpseNetworkObjectId, ClientRpcParams rpcParams = default)
    {
        if (canReport==false)
        {
            return;
        }
        Debug.Log($"시체신고 가능여부={canReport}: Server Rpc호출");
        ReportServerRpc(corpseNetworkObjectId);
    }

    // 3. 실제 작업 수행: ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void ReportServerRpc(ulong targetNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong reporterClientId = rpcParams.Receive.SenderClientId;
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }
    

    [ServerRpc(RequireOwnership = false)]
    public void CanSavotageServerRpc(ServerRpcParams rpcParams = default)
    {
        //TODO: 사보타지 조건구현
        //farmer는 사보타지 가능
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        CanSavotageResultClientRpc(true, new ClientRpcParams 
        { 
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requesterClientId } } 
        });
    }

    [ClientRpc]
    public void CanSavotageResultClientRpc(bool canSabotage, ClientRpcParams rpcParams = default)
    {
        if (canSabotage==false)
        {
            return;
        }
        Debug.Log($"사보타지 가능여부={canSabotage}: Server Rpc호출");
        SavotageServerRpc();
    }

    
    
    #endregion
}
