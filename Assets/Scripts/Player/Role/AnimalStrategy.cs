using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

/// <summary>
/// 동물 역할 전략
/// 기본적인 이동과 상호작용만 가능
/// </summary>
public class AnimalStrategy : IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerInput _playerInput;
    private InputActionMap _commonActionMap;
    
    public AnimalStrategy(PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerPresenter = playerPresenter;
        _playerInput = playerInput;
    }
    
    public void Setup()
    {
        // 공통 Action Map만 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        if (_commonActionMap != null) _commonActionMap.Enable();
        
        // 동물 전용 UI 활성화
        _playerPresenter.ShowAnimalUI();
    }
    
    public void HandleInput(InputAction.CallbackContext context)
    {
        // 동물은 기본 입력만 처리
        string actionName = context.action.name;
        
        switch (actionName)
        {
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
        // 동물 전용 업데이트 로직
        // 예: 특별한 애니메이션, 효과 등
    }
    
    public void Cleanup()
    {
        if (_commonActionMap != null)
        {
            _commonActionMap.Disable();
        }
    }
    
    #region Ability 구현 (다형성)
    
    public void TryKill()
    {
        // 동물은 킬 불가능
        Debug.Log("동물은 킬을 사용할 수 없습니다.");
    }
    
    public void TrySabotage()
    {
        // 동물은 사보타지 불가능
        Debug.Log("동물은 사보타지를 사용할 수 없습니다.");
    }
    
    public void TryInteract()
    {
        if (!CanInteract()) return;
        
        // 상호작용 액션 실행
        _playerPresenter.TryInteractServerRpc();
    }
    
    public void TryReportCorpse()
    {
        if (!CanReportCorpse()) return;
        
        // 시체 리포트 액션 실행
        _playerPresenter.ReportCorpseServerRpc(_playerPresenter.OwnerClientId);
    }
    
    public bool CanKill()
    {
        // 동물은 킬 불가능
        return false;
    }
    
    public bool CanSabotage()
    {
        // 동물은 사보타지 불가능
        return false;
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
