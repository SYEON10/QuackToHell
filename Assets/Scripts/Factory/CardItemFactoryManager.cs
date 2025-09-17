using UnityEngine;
using CardItem.MVP;
using Unity.Netcode;

/// <summary>
/// 카드 아이템 생성 팩토리
/// 카드 정보를 미리 로드한 뒤에, 생성해야 할 때 사용
/// </summary>
public class CardItemFactoryManager : NetworkBehaviour
{
    #region 싱글톤
    public static CardItemFactoryManager Instance => SingletonHelper<CardItemFactoryManager>.Instance;

    private void Awake()
    {
        SingletonHelper<CardItemFactoryManager>.InitializeSingleton(this);
    }
    #endregion


    #region 카드 아이템 생성
    [Header ("Card 프리팹을 넣어주세요.")]
    public GameObject cardItemPrefab;
    
    [Header("UI References")]
    [SerializeField] private GameObject cardForSaleParent;
    [SerializeField] private GameObject cardForInventoryParent;

    public GameObject CreateCardForInventory(CardItemData cardItemData)
    {
        #region 유효한 요청인지 확인
        int requestedCardIdKey = cardItemData.cardIdKey;
        if (!DeckManager.Instance.IsValidCardIdKey(requestedCardIdKey))
        {
            Debug.LogError($"[CardItemFactoryManager] 유효하지 않은 카드 아이디 요청입니다. CardID: {cardItemData.cardIdKey}");
            return null;
        }
        #endregion

        #region 카드 생성 
        if (!DebugUtils.AssertNotNull(cardItemPrefab, "cardItemPrefab", this))
            return null;
            
        GameObject cardItemForInventory = Instantiate(cardItemPrefab, Vector3.zero, Quaternion.identity);

        //데이터 주입 (읽기 전용으로 서버 데이터 사용)
        CardItemModel cardItemModel = cardItemForInventory.GetComponent<CardItemModel>();
        if (DebugUtils.AssertNotNull(cardItemModel, "CardItemModel", this))
        {
            cardItemModel.UpdateCardStateFromServer(cardItemData);
        }

        //태그 부여
        cardItemForInventory.tag = GameTags.CardForInventory;
        
        //크기 조정
        RectTransform cardItemForSaleRectTransform = cardItemForInventory.GetComponent<RectTransform>();
        Vector2 newSize = new Vector2(GameConstants.Card.InventoryCardWidth, GameConstants.Card.InventoryCardHeight);
        cardItemForSaleRectTransform.sizeDelta = newSize;

        //Transform 설정
        cardItemForInventory.transform.localScale = Vector3.one;
        cardItemForInventory.transform.localPosition = Vector3.zero;
        #endregion

        return cardItemForInventory;
    }
  
    public void CreateTotalCardForSale(GameObject cardForSaleParent)
    {
        // 모든 클라이언트에서 UI 생성 (데이터는 서버에서 읽기)
        #region 카드생성
        for(int i=0;i < DeckManager.Instance.AllCardsOnGameData.Count; i++)
        {
            // 서버의 권위적 데이터에서 읽기 (수정하지 않음)
            CardItemData cardItemData = DeckManager.Instance.AllCardsOnGameData[i];

            GameObject cardItemForSale = Instantiate(cardItemPrefab, Vector3.zero, Quaternion.identity);

            if (!DebugUtils.AssertNotNull(cardForSaleParent, "CardForSaleParent", this))
                continue;

            cardItemForSale.transform.SetParent(cardForSaleParent.transform);

            //비활성화
            cardItemForSale.SetActive(false);

            // 데이터 주입 (읽기 전용으로 서버 데이터 사용)
            CardItemModel cardItemModel = cardItemForSale.GetComponent<CardItemModel>();
            if (DebugUtils.AssertNotNull(cardItemModel, "CardItemModel", this))
            {
                cardItemModel.UpdateCardStateFromServer(cardItemData);
            }

            //태그 부여
            cardItemForSale.tag = GameTags.CardForSale;

            //크기 조정
            RectTransform cardItemForSaleRectTransform = cardItemForSale.GetComponent<RectTransform>();
            Vector2 newSize = new Vector2(GameConstants.Card.SaleCardWidth, GameConstants.Card.SaleCardHeight);
            cardItemForSaleRectTransform.sizeDelta = newSize;

            // CardForSale 오브젝트의 이름을 CardItemId와 함께 설정
            cardItemForSale.name = $"CardForSale_{cardItemData.cardItemStatusData.cardItemID}";

            #endregion
        }
        
    }
    #endregion

   
}
