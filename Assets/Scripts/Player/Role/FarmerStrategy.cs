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

    public void TryVent()
    {
        if (!CanVent()) return;
        
        // 벤트 근처 확인
        if (HasVentNearby())
        {
            _playerPresenter.TryVentServerRpc();
        }
    }
    public bool CanVent()
    {
        // 농장주만 벤트 사용 가능
        return _playerPresenter.GetPlayerAliveState() == PlayerLivingState.Alive;
    }

    private bool HasVentNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.Vent))
            {
                return true;
            }
        }
        return false;
    }

    
    public void TryKill()
    {
        if (!CanKill()) return;
        
        _playerPresenter.TryKillServerRpc();
    }
    
    public void TrySabotage()
    {
        if (!CanSabotage()) return;
        
        // TODO: 사보타지 액션 구현
        Debug.Log("사보타지 시도");
        
    }
    
    public void TryInteract()
    {
        if (!CanInteract()) return;
        
        // 1단계: 상호작용 오브젝트 확인
        if (HasInteractableObjectsNearby())
        {
            _playerPresenter.TryInteractServerRpc(); // 기존 로직
        }
        else
        {
            // 2단계: 상호작용 오브젝트 없음 → 사보타지
            TrySabotage(); 
        }
    }

    public bool HasInteractableObjectsNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.MiniGame) ||
                collider.CompareTag(GameTags.RareCardShop) ||
                collider.CompareTag(GameTags.Exit) ||
                collider.CompareTag(GameTags.Teleport) ||
                collider.CompareTag(GameTags.Vent))
            {
                return true;
            }
        }
        return false;
    }
    
    public void TryReportCorpse()
    {
        if (!CanReportCorpse()) return;
        
        // 시체 또는 재판소집 오브젝트 근처 확인
        if (HasCorpseNearby())
        {
            _playerPresenter.ReportCorpseServerRpc(_playerPresenter.OwnerClientId);
        }
        if(HasTrialConvocationNearby()){
            _playerPresenter.TryTrialServerRpc(_playerPresenter.OwnerClientId);
        }
    }
    

    private bool HasCorpseNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.PlayerCorpse))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasTrialConvocationNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_playerPresenter.transform.position, 1.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag(GameTags.ConvocationOfTrial))
            {
                return true;
            }
        }
        return false;
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
