using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;
using System;

/// <summary>
/// 농장주 역할 전략
/// Kill, Sabotage 등의 Ability를 다형성으로 구현
/// </summary>
public class FarmerStrategy : IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerInput _playerInput;
    private InputActionMap _farmerActionMap;
    private InputActionMap _commonActionMap;
    
    public FarmerStrategy(PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
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
        
        // 농장주 전용 UI 활성화
        _playerPresenter.ShowFarmerUI();
    }
    
    public void HandleInput(InputAction.CallbackContext context)
    {
        // 농장주 전용 입력 처리
        string actionName = context.action.name;
        
        switch (actionName)
        {
            case GameInputs.Actions.Kill:
                TryKill();
                break;
            case GameInputs.Actions.Sabotage:
                TrySabotage();
                break;
            case GameInputs.Actions.Interact:
                TryInteract();
                break;
            case GameInputs.Actions.Report:
                TryReportCorpse();
                break;
        }
    }
    
    public void Update()
    {
        // 농장주 전용 업데이트 로직
        // 예: 특별한 효과, 타이머 등
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
    
    public void TryKill()
    {
        if (!CanKill()) return;
        
        _playerPresenter.TryKillServerRpc();
    }
    
    public void TrySabotage()
    {
        if (!CanSabotage()) return;
        
        // TODO: 사보타지 액션 구현
        Debug.Log("사보타지 액션 실행");
    }
    
    public void TryInteract()
    {
        if (!CanInteract()) return;
        
        _playerPresenter.TryInteractServerRpc();
    }
    
    public void TryReportCorpse()
    {
        if (!CanReportCorpse()) return;
        
        _playerPresenter.ReportCorpseServerRpc(_playerPresenter.OwnerClientId);
    }
    
    public bool CanKill()
    {
        // 농장주는 킬 가능
        return true;
    }
    
    public bool CanSabotage()
    {
        // 농장주는 사보타지 가능
        return true;
    }
    
    public bool CanInteract()
    {
        // 모든 역할이 상호작용 가능
        return true;
    }
    
    public bool CanReportCorpse()
    {
        // 유령이 아닌 경우에만 시체 리포트 가능
        return _playerPresenter.GetPlayerAliveState() == PlayerLivingState.Alive;
    }
    
    #endregion
}
