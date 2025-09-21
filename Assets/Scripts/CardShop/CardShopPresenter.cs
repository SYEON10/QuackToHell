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
        clientId = NetworkManager.Singleton.LocalClientId;
    }
 
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (_view != null)
        {
            _view.OnClickLock += OnClickLock;
            _view.OnClickReRoll += OnClickReRoll;
        }

        if (IsServer)
        {
            s_serverByClient[OwnerClientId] = this;
            // 서버에서 카드 표시
            RequestDisplayCards(clientId);
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
        
        if (_model.IsLocked) return;
        if (_cooldown) return;

        StartCoroutine(RerollCooldown());
        
        _model.TryReRoll(clientId);
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

    public static void ServerSendResultTo(ulong clientId, bool success)
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer) return;
        if (s_serverByClient.TryGetValue(clientId, out CardShopPresenter presenter))
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
            presenter.OnPurchaseResult(success);
        }
    }

    #region 외부 인터페이스 (메시지 기반)
    
    /// <summary>
    /// 카드샵 잠금 요청
    /// </summary>
    public void RequestLockShop()
    {
        OnClickLock();
    }
    
    /// <summary>
    /// 카드샵 상태 조회
    /// </summary>
    public bool IsShopLocked()
    {
        return _model?.IsLocked ?? false;
    }
    
    /// <summary>
    /// 카드 표시 요청 (외부에서 호출)
    /// </summary>
    public void RequestDisplayCards(ulong clientId)
    {
        if (_model?.IsLocked == false)  
        {
            DeckManager.Instance.RequestDisplayCardsServerRpc(clientId);
        }
    }
    
    /// <summary>
    /// 리롤 요청 (외부에서 호출)
    /// </summary>
    public void RequestReroll()
    {
        if (!_cooldown && _model != null && !_model.IsLocked)
        {
            OnClickReRoll();
        }
    }

    #endregion
}
