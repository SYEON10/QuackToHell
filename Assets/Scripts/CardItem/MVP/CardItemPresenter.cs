using UnityEngine;

namespace CardItem.MVP
{
    public class CardItemPresenter : MonoBehaviour
    {

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

        #region 모델, 뷰 참조
        private CardItemModel cardItemModel;
        private CardItemView cardItemView;
        private void Awake()
        {
            cardItemModel = GetComponent<CardItemModel>();
            cardItemView = GetComponent<CardItemView>();
        }
        #endregion

        #region 외향


        private void CardDefData_OnValueChanged(CardDef cardDefData, int cost)
        {
            // DeckManager를 통해 로컬라이제이션된 데이터 가져오기
            string localizedName = cardDefData.cardNameKey.ToString();
            string localizedDescription = cardDefData.descriptionKey.ToString();
        
            // DeckManager가 준비되었고 데이터가 로드되었는지 확인
            if (DeckManager.Instance != null && DeckManager.Instance.CardDefinitionCount > 0)
            {
                if (DeckManager.Instance.TryGetCardDisplay(cardDefData.cardID, "ko", out var display))
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
        private CardShopPresenter cardShopPresenter;
        private void CardItemView_OnPurchaseClicked(ulong inputClientId)
        {
            CardItemData myCardItemData = cardItemModel.CardItemData.Value;
            Debug.Log("[CardItemPresenter] cardShopPresenter.TryPurchaseCard 호출");
            // TODO:CardShop에게 카드 구매 요청 : 카드 아이디, 플레이어 아이디, 카드 가격 보내주기
            cardShopPresenter = GameObject.FindAnyObjectByType<CardShopPresenter>();
            cardShopPresenter.TryPurchaseCard(myCardItemData, inputClientId);
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
    }
}
