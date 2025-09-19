using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

/// <summary>
/// 유령 역할 전략
/// 시체를 볼 수 있지만 리포트는 불가능
/// </summary>
public class GhostStrategy : IRoleStrategy
{
    private PlayerPresenter _playerPresenter;
    private PlayerInput _playerInput;
    private InputActionMap _commonActionMap;
    
    public GhostStrategy(PlayerPresenter playerPresenter, PlayerInput playerInput)
    {
        _playerPresenter = playerPresenter;
        _playerInput = playerInput;
    }
    
    public void Setup()
    {
        // 공통 Action Map만 활성화
        _commonActionMap = _playerInput.actions.FindActionMap(GameInputs.ActionMaps.Player);
        if (_commonActionMap != null) _commonActionMap.Enable();
        
        // 유령 전용 UI 활성화
        _playerPresenter.ShowGhostUI();
        
        // 유령 상태로 변경 (시각적 효과만)
        _playerPresenter.ChangeToGhostVisualState();
    }
    
    public void HandleInput(InputAction.CallbackContext context)
    {
        // 유령은 기본 입력만 처리
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
        // 유령 전용 업데이트 로직
        // 예: 투명도, 특별한 효과 등
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
        // 유령은 킬 불가능
        Debug.Log("유령은 킬을 사용할 수 없습니다.");
    }
    
    public void TrySabotage()
    {
        // 유령은 사보타지 불가능
        Debug.Log("유령은 사보타지를 사용할 수 없습니다.");
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
        // 유령은 킬 불가능
        return false;
    }
    
    public bool CanSabotage()
    {
        // 유령은 사보타지 불가능
        return false;
    }
    
    public bool CanInteract()
    {
        // 유령은 상호작용 불가능
        return false;
    }
    
    public bool CanReportCorpse()
    {
        // 유령은 시체 리포트 불가능
        return false;
    }
    public void TryVent() { /* 아무것도 하지 않음 */ }
    public bool CanVent() { return false; }
    
    #endregion
}
