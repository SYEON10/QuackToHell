using UnityEngine;

namespace CardItem.MVP
{
    public class CardItemPresenter : MonoBehaviour
    {
        [Header("Components")]
        private CardItemModel cardItemModel;
        private CardItemView cardItemView;
        
        [Header("References")]
        [SerializeField] private CardShopPresenter cardShopPresenter;

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
            cardItemModel.CardItemData.OnValueChanged += OnCardItemDataChanged;

            //외향 초기화
            CardItemData cardItemData = cardItemModel.CardItemData.Value;
            CardDef cardItemDef = cardItemData.cardDef;
            CardStatusData cardStatusData = cardItemData.cardItemStatusData;
            CardDefData_OnValueChanged(cardItemDef, cardStatusData.cost);
            CardItemStatusData_OnValueChanged(cardStatusData, cardItemDef.type, cardItemDef.mapRestriction);

            //카드 판매 가격 초기화
            cardItemView.SetCardForSaleAppearence(cardStatusData.price);
            //카드 아이템 id 초기화
            cardItemView.SetCardItemIdAppearence(cardStatusData.cardItemID);
        }

        #region 외향


        private void CardDefData_OnValueChanged(CardDef cardDefData, int cost)
        {
            // DeckManager를 통해 로컬라이제이션된 데이터 가져오기
            string localizedName = cardDefData.cardNameKey.ToString();
            string localizedDescription = cardDefData.descriptionKey.ToString();
        
            // DeckManager가 준비되었고 데이터가 로드되었는지 확인
            if (DeckManager.Instance != null && DeckManager.Instance.CardDefinitionCount > 0)
            {
                if (DeckManager.Instance.TryGetCardDisplay(cardDefData.cardID, "ko", out CardDisplay display))
                {
                    localizedName = display.name;
                    localizedDescription = display.description;
                }
            }
        
            cardItemView.SetCardItemNameAppearence(localizedName, cardDefData.tier);
            cardItemView.SetCardItemImageAppearence(cardDefData.tier, cardDefData.type);
            cardItemView.SetCardTypeAppearence(cardDefData.mapRestriction, cardDefData.type);
            cardItemView.SetCardDefinitionAppearence(localizedDescription);
            cardItemView.SetCardCharacteristicAppearence(cost, cardDefData.type, cardDefData.mapRestriction);
        }
        private void CardItemStatusData_OnValueChanged(CardStatusData cardItemStatusData, TypeEnum type, int map_Restriction)
        {
            cardItemView.SetCardCharacteristicAppearence(cardItemStatusData.cost, type, map_Restriction);
            cardItemView.SetCardForSaleAppearence(cardItemStatusData.price);
            cardItemView.SetCardItemIdAppearence(cardItemStatusData.cardItemID);
        }
        #endregion

        #region 구매 클릭 입력 이벤트 전달
        private void CardItemView_OnPurchaseClicked(ulong inputClientId)
        {
            CardItemData myCardItemData = cardItemModel.CardItemData.Value;
            // CardShop에게 카드 구매 요청
            if (DebugUtils.AssertNotNull(cardShopPresenter, "CardShopPresenter", this))
            {
                cardShopPresenter.TryPurchaseCard(myCardItemData, inputClientId);
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
                CardItemStatusData_OnValueChanged(newValue.cardItemStatusData, newValue.cardDef.type, newValue.cardDef.mapRestriction);
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
                cardItemModel.CardItemData.Value = newData;
            }
        }
        
        /// <summary>
        /// 카드 상태 조회
        /// </summary>
        public CardItemData GetCardData()
        {
            return cardItemModel?.CardItemData.Value ?? default(CardItemData);
        }
        
        /// <summary>
        /// 카드 가격 조회
        /// </summary>
        public int GetCardPrice()
        {
            return cardItemModel?.CardItemData.Value.cardItemStatusData.price ?? 0;
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
