using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CardShopPresenter : NetworkBehaviour
{
    [Header("Components")]
    private CardShopView _view;
    private CardShopModel _model;
    
    [Header("Settings")]
    [SerializeField] private float rerollCooldown = 0.2f;

    private static readonly Dictionary<ulong, CardShopPresenter> s_serverByClient = new();
    private bool _cooldown;
    private ulong clientId;
    private void Awake()
    {
        _view = GetComponent<CardShopView>();
        _model = GetComponent<CardShopModel>();
            
        DebugUtils.AssertComponent(_view, "CardShopView", this);
        DebugUtils.AssertComponent(_model, "CardShopModel", this);
        //clientId = NetworkManager.Singleton.LocalClientId;
    }

    private void Start()
    {
        if (_view != null)
        {
            _view.OnClickLock += OnClickLock;
            _view.OnClickReRoll += OnClickReRoll;
            _view.OnClickX += OnClickX;
        }

        // note cba0898: 이거 왜 있는지 모르겠어영..
        /*
        if (IsServer)
        {
            s_serverByClient[OwnerClientId] = this;
            // 서버에서 카드 표시
            RequestDisplayCards(NetworkManager.Singleton.LocalClientId);
        }
        */
    }

    private void OnDestroy()
    {
        if (_view != null)
        {
            _view.OnClickLock -= OnClickLock;
            _view.OnClickReRoll -= OnClickReRoll;
            _view.OnClickX -= OnClickX;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner && _view != null)
        {
            _view.OnClickLock -= OnClickLock;
            _view.OnClickReRoll -= OnClickReRoll;
        }

        if (IsServer)
            s_serverByClient.Remove(OwnerClientId);
    }

    public void TryPurchaseCard(CardItemData card, ulong inputClientId)
    {
        ulong clientId = inputClientId == 0UL ? OwnerClientId : inputClientId;
        DeckManager.Instance.TryPurchaseCardServerRpc(card, clientId);
    }

    public void OnPurchaseResult(bool success)
    {
        if (_view == null) return;
        if(success){
             Debug.Log("[CardShopPresenter] 구매 성공");
        }
        else{
            Debug.Log("[CardShopPresenter] 구매 실패");
        }
    }

    private void OnClickLock()
    {
        _model.IsLocked = !_model.IsLocked;
        _view.SetRefreshInteractable(!_model.IsLocked);
    }
    
    private void OnClickReRoll()
    {
        
        if (_cooldown) return;

        StartCoroutine(RerollCooldown());
        
        _model.TryReRoll(NetworkManager.Singleton.LocalClientId);
    }

    private void OnClickX()
    {
        RequestCloseCardShop();
    }

    /// <summary>
    /// 서버에서 진열 결과를 받아서 UI 업데이트
    /// </summary>
    public void OnDisplayCardsResult(CardItemData[] displayedCards)
    {
        _model.UpdateDisplayFromServer(displayedCards);
    }

    private IEnumerator RerollCooldown()
    {
        _cooldown = true;
        _view.SetRefreshInteractable(false);
        yield return new WaitForSeconds(rerollCooldown);
        _view.SetRefreshInteractable(true);
        _cooldown = false;
    }


    #region 외부 인터페이스 (메시지 기반)
    
    /// <summary>
    /// 카드샵 잠금 요청 (에디터에서 마우스이벤트 연결)
    /// </summary>
    public void RequestLockShop()
    {
        OnClickLock();
    }
    
    /// <summary>
    /// 리롤 요청 (에디터에서 마우스이벤트 연결)
    /// </summary>
    public void RequestReroll()
    {
        if (!_cooldown)
        {
            OnClickReRoll();
        }
    }

    #endregion

    public void RequestShowCardShop()
    {
        _view.ToggleCardShopUI(true);
        if(_model!=null && !_model.HasDisplayedCards()){
            // 리롤 버튼과 동일한 검증 로직 사용하되, 쿨다운은 적용하지 않음
            _model.TryReRoll(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void RequestCloseCardShop()
    {
        _view.ToggleCardShopUI(false);
    }
}
