using UnityEngine;

namespace CardItem.MVP
{
    public class CardItemPresenter : MonoBehaviour
    {
        [Header("Components")]
        private CardItemModel cardItemModel;
        private CardItemView cardItemView;
        
        [Header("References")]
        private CardShopPresenter _cardShopPresenter;
        public CardShopPresenter CardShopPresenter
        {
            set { _cardShopPresenter = value; }
        }

        private void Awake()
        {
            cardItemModel = GetComponent<CardItemModel>();
            cardItemView = GetComponent<CardItemView>();
                
            DebugUtils.AssertComponent(cardItemModel, "CardItemModel", this);
            DebugUtils.AssertComponent(cardItemView, "CardItemView", this);
        }

        private void Start()
        {
            
            //구매 클릭 이벤트 바인딩
            cardItemView.OnPurchaseClicked += CardItemView_OnPurchaseClicked;
            
            // 카드 데이터 변경 이벤트 바인딩
            cardItemModel.OnCardDataChanged += OnCardItemDataChanged;

            //외향 초기화
            UpdateCardAppearance(cardItemModel.CardItemData);
        }


        #region 외향
        /// <summary>
        /// 카드 외관을 업데이트
        /// </summary>
        private void UpdateCardAppearance(CardItemData cardData)
        {
            CardDef cardDef = cardData.cardDef;
            CardStatusData statusData = cardData.cardItemStatusData;
            
            // 로컬라이제이션 처리
            string localizedName = cardDef.cardNameKey.ToString();
            string localizedDescription = cardDef.descriptionKey.ToString();
            
            if (DeckManager.Instance != null && DeckManager.Instance.CardDefinitionCount > 0)
            {
                if (DeckManager.Instance.TryGetCardDisplay(cardDef.cardID, "ko", out CardDisplay display))
                {
                    localizedName = display.name;
                    localizedDescription = display.description;
                }
            }
            
            // 모든 외관 요소 한 번에 설정
            cardItemView.SetCardItemNameAppearence(localizedName, cardDef.tier);
            cardItemView.SetCardItemImageAppearence(cardDef.tier, cardDef.type);
            cardItemView.SetCardTypeAppearence(cardDef.mapRestriction, cardDef.type);
            cardItemView.SetCardDefinitionAppearence(localizedDescription);
            cardItemView.SetCardCharacteristicAppearence(statusData.cost, cardDef.type, cardDef.mapRestriction);
            cardItemView.SetCardForSaleAppearence(statusData.price);
            cardItemView.SetCardItemIdAppearence(statusData.cardItemID);
        }



        
        #endregion

        #region 구매 클릭 입력 이벤트 전달
        private void CardItemView_OnPurchaseClicked(ulong inputClientId)
        {
            CardItemData myCardItemData = cardItemModel.CardItemData;
            // CardShop에게 카드 구매 요청
            if (DebugUtils.AssertNotNull(_cardShopPresenter, "CardShopPresenter", this))
            {
                _cardShopPresenter.TryPurchaseCard(myCardItemData, inputClientId);
            }
        }

        /// <summary>
        /// 카드 데이터 변경 시 호출되는 이벤트 핸들러
        /// </summary>
        private void OnCardItemDataChanged(CardItemData previousValue, CardItemData newValue)
        {
            // 상태가 변경되었을 때 UI 업데이트
            if (previousValue.cardItemStatusData.state != newValue.cardItemStatusData.state)
            {
                UpdateCardAppearance(newValue);            
            }
        }
        #endregion

        #region 외부 인터페이스 (메시지 기반)
        
        /// <summary>
        /// 카드 구매 요청
        /// </summary>
        public void RequestPurchase(ulong clientId = 0)
        {
            CardItemView_OnPurchaseClicked(clientId);
        }
        
        /// <summary>
        /// 카드 데이터 업데이트 요청
        /// </summary>
        public void RequestUpdateCardData(CardItemData newData)
        {
            if (cardItemModel != null)
            {
                cardItemModel.SetCardData(newData);
            }
        }
        
        /// <summary>
        /// 카드 상태 조회
        /// </summary>
        public CardItemData GetCardData()
        {
            return cardItemModel?.CardItemData ?? default(CardItemData);
        }
        
        /// <summary>
        /// 카드 가격 조회
        /// </summary>
        public int GetCardPrice()
        {
            return cardItemModel?.CardItemData.cardItemStatusData.price ?? 0;
        }
        
        /// <summary>
        /// 카드 구매 가능 여부 조회
        /// </summary>
        public bool IsPurchasable()
        {
            CardItemData cardData = GetCardData();
            return cardData.cardItemStatusData.state == CardItemState.None;
        }
        
        #endregion
    }
}
