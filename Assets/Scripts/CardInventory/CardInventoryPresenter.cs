using UnityEngine;
using Unity.Netcode;

public class CardInventoryPresenter : MonoBehaviour
{
    [Header("Components")]
    private CardInventoryModel _cardInventoryModel;
    private CardInventoryView _cardInventoryView;

    private void Awake()
    {
        _cardInventoryModel = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(NetworkManager.Singleton.LocalClientId).GetComponent<CardInventoryModel>();
        DebugUtils.AssertComponent(_cardInventoryModel, "CardInventoryModel", this);
        _cardInventoryView = GetComponent<CardInventoryView>();
        DebugUtils.AssertComponent(_cardInventoryView, "CardInventoryView", this);
    }

    private void Start()
    {
        DebugUtils.AssertNotNull(_cardInventoryModel, "CardInventoryModel", this);
        DebugUtils.AssertNotNull(_cardInventoryView, "CardInventoryView", this);

        //초기 뷰 업데이트
        _cardInventoryView.UpdateInventoryView(_cardInventoryModel.OwnedCards);
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        PlayerPresenter playerPresenter=  PlayerHelperManager.Instance.GetPlayerPresenterByClientId(localClientId);
        _cardInventoryView.UpdatePlayerGold(playerPresenter.GetGold());
        //TODO: 정렬기능 추가되면, 정렬 enum에 따라 다른 정렬 함수 호출
        /*switch (_cardInventoryModel.SortingOption)
        {
            case InventorySotringOption.RecentlyAcquired:
                _cardInventoryModel.SortCardsByAcquiredTicks();
                break;
            default:
                break;
        }*/

        PlayerModel playerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId);
        Debug.Assert(playerModel != null);
        playerModel.PlayerStatusData.OnValueChanged += OnPlayerStatusChanged;

        _cardInventoryModel.OwnedCards.OnListChanged += CardInventoryModel_OwnedCardsOnListChanged;

        _cardInventoryView.OnShowCardShopClicked += OnClickShowCardShop;
        _cardInventoryView.OnCloseInventoryClicked += OnClickCloseInventory;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && PlayerHelperManager.Instance != null)
        {
            PlayerModel playerModel = PlayerHelperManager.Instance.GetPlayerModelByClientId(NetworkManager.Singleton.LocalClientId);
            if (playerModel != null)
            {
                playerModel.PlayerStatusData.OnValueChanged -= OnPlayerStatusChanged;
            }
        }

        if (_cardInventoryModel != null)
        {
            _cardInventoryModel.OwnedCards.OnListChanged -= CardInventoryModel_OwnedCardsOnListChanged;
        }

        if (_cardInventoryView != null)
        {
            _cardInventoryView.OnShowCardShopClicked -= OnClickShowCardShop;
            _cardInventoryView.OnCloseInventoryClicked -= OnClickCloseInventory;
        }
    }

    private void CardInventoryModel_OwnedCardsOnListChanged(NetworkListEvent<CardItemData> changeEvent)
    {
        // note cba0898: 가지고 있는 _cardInventoryView를 써도 될 것 같습니다
        //view 업데이트 함수 호출
        CardInventoryView cardInventoryView = gameObject.GetComponent<CardInventoryView>();
        cardInventoryView?.UpdateInventoryView(_cardInventoryModel.OwnedCards);
    }

    private void OnClickShowCardShop()
    {
        if (SceneManager.GetActiveScene().name != GameScenes.Village)
        {
            Debug.Log("CardShop not available in this scene");
            return;
        }

        CardShopPresenter cardShop = FindFirstObjectByType<CardShopPresenter>();
        DebugUtils.AssertNotNull(cardShop, "CardShopPresenter", this);
        cardShop.RequestShowCardShop();
    }

    private void OnClickCloseInventory()
    {
        DebugUtils.AssertNotNull(_cardInventoryView, "CardInventoryView", this);
        _cardInventoryView.CloseInventory();
    }

    #region 외부 인터페이스 (메시지 기반)
    
    /// <summary>
    /// 인벤토리 새로고침 요청
    /// </summary>
    public void RequestRefreshInventory()
    {
        if (_cardInventoryView != null && _cardInventoryModel != null)
        {
            _cardInventoryView.UpdateInventoryView(_cardInventoryModel.OwnedCards);
        }
    }
    
    /// <summary>
    /// 카드 정렬 요청
    /// </summary>
    public void RequestSortCards(InventorySotringOption sortingOption)
    {
        if (_cardInventoryModel != null)
        {
            // TODO: 정렬 로직 구현 (SortingOption이 읽기 전용이므로 다른 방식으로 처리 필요)
            // _cardInventoryModel.SortingOption = sortingOption; // 읽기 전용이므로 제거
            RequestRefreshInventory();
        }
    }
    
    /// <summary>
    /// 소유한 카드 수 조회
    /// </summary>
    public int GetOwnedCardCount()
    {
        return _cardInventoryModel?.OwnedCards.Count ?? 0;
    }
    
    /// <summary>
    /// 특정 카드 소유 여부 조회
    /// </summary>
    public bool HasCard(int cardId)
    {
        if (_cardInventoryModel?.OwnedCards == null) return false;
        
        foreach (CardItemData card in _cardInventoryModel.OwnedCards)
        {
            if (card.cardItemStatusData.cardItemID == cardId)
                return true;
        }
        return false;
    }

    public bool IsInventoryMaximum()
    {
        if (_cardInventoryModel.OwnedCards.Count == GameConstants.Card.maxCardCount)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 플레이어 상태 변경 시 호출되는 메서드
    /// </summary>
    private void OnPlayerStatusChanged(PlayerStatusData previousValue, PlayerStatusData newValue)
    {
        // 골드가 실제로 변경되었을 때만 UI 업데이트
        if (previousValue.gold != newValue.gold)
        {
            Debug.Assert(_cardInventoryView != null);
            _cardInventoryView.UpdatePlayerGold(newValue.gold);
        }
    }
    
    #endregion
}
