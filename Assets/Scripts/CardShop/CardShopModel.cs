using UnityEngine;
using System.Collections.Generic;
using CardItem.MVP;

public class CardShopModel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject cardForSaleParent;
    [SerializeField] private Transform rowObjectTransform;
    [SerializeField] private Transform cardForSaleParentTransform;
    
    [Header("Shop Data")]
    public bool IsLocked { get; set; }

    private enum Rarity { Common, Rare }
    const int maximumDisplayCount = 5;
    private int totalCardCountOnMap = 0;

    private void Start()
    {
        cardForSaleParentTransform = cardForSaleParent.transform;
        
        // 패널 활성화/비활성화
        GameObject cardShopPanelObject = rowObjectTransform.parent.gameObject;
        cardShopPanelObject.SetActive(true);
        cardShopPanelObject.SetActive(false);
        
        CardItemFactoryManager.Instance.CreateTotalCardForSale(cardForSaleParent);
        totalCardCountOnMap = cardForSaleParent.transform.childCount;
    }

    public void RequestPurchase(CardItemData card, ulong clientId)
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("[CardShopModel] DeckManager.Instance is null");
            return;
        }
        DeckManager.Instance.TryPurchaseCardServerRpc(card, clientId);

    }

    #region 카드 목록 새로고침
    public bool TryReRoll()
    {
        if (IsLocked) return false;

        // 새로 뿌리기
        DisplayCardForSale();

        return true;
    }


    public void DisplayCardForSale()
    {
        // 초기화 시에만 로컬에서 실행 (서버 동기화 대기)
        // 실제 진열은 서버 RPC를 통해 처리됨
        ClearCurrentDisplay();
    }

    /// <summary>
    /// 서버에서 진열 결과를 받아서 UI 업데이트
    /// </summary>
    public void UpdateDisplayFromServer(CardItemData[] displayedCards)
    {
        // 기존 진열된 카드들 정리
        ClearCurrentDisplay();

        // 서버에서 선택된 카드들을 진열
        foreach (var cardData in displayedCards)
        {
            // 해당 카드 오브젝트 찾기
            Transform cardObject = FindCardObjectByCardItemId(cardData.cardItemStatusData.cardItemID);
            if (cardObject != null)
            {
                // Row로 이동하여 진열
                cardObject.SetParent(rowObjectTransform, false);
                cardObject.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 현재 진열된 카드들을 정리
    /// </summary>
    private void ClearCurrentDisplay()
    {
        if (rowObjectTransform.childCount > 0)
        {
            int childCount = rowObjectTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = rowObjectTransform.GetChild(0);
                child.SetParent(cardForSaleParentTransform, false);
                child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// CardItemID로 카드 오브젝트 찾기
    /// </summary>
    private Transform FindCardObjectByCardItemId(int cardItemId)
    {
        if (cardForSaleParentTransform == null) return null;
        
        for (int i = 0; i < cardForSaleParentTransform.childCount; i++)
        {
            Transform card = cardForSaleParentTransform.GetChild(i);
            if (card == null) continue;
            
            CardItemModel cardItemModel = card.GetComponent<CardItemModel>();
            if (cardItemModel != null && 
                cardItemModel.CardItemData.Value.cardItemStatusData.cardItemID == cardItemId)
            {
                return card;
            }
        }
        return null;
    }
    #endregion
}
