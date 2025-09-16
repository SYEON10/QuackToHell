using UnityEngine;
using System.Collections.Generic;
using CardItem.MVP;

public class CardShopModel
{
    public bool IsLocked { get; set; }

    private enum Rarity { Common, Rare }

    private GameObject cardForSaleParent;
    private Transform rowObjectTransform;
    private Transform cardForSaleParentTransform;
    const int maximumDisplayCount = 5;
    private int totalCardCountOnMap = 0;

    public void Initiate()
    {

        cardForSaleParent = GameObject.FindWithTag("CardForSaleParent");
        cardForSaleParentTransform = cardForSaleParent.transform;
        GameObject cardShopCanvasObject = GameObject.FindWithTag("CardShop");
        GameObject cardShopPanelObject = cardShopCanvasObject.transform.GetChild(1).gameObject;
        cardShopPanelObject.SetActive(true);
        rowObjectTransform = cardShopPanelObject.transform.GetChild(1).transform;
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
        Debug.Log("[CardShopModel] RequestPurchase 실행됨");
        DeckManager.Instance.TryPurchaseCardServerRpc(card, clientId);

        /* if (IsInventoryFull(clientId, out var invMsg))
         {
             Debug.LogWarning($"[Shop] 구매 실패(인벤토리 초과): {invMsg}");
             CardShopPresenter.ServerSendResultTo(clientId, false);
             return;
         }

         if (IsDuplicateRestrictedAndOwned(card, clientId, out var dupMsg))
         {
             Debug.LogWarning($"[Shop] 구매 실패(중복 제한): {dupMsg}");
             CardShopPresenter.ServerSendResultTo(clientId, false);
             return;
         }*/
    }

    /* private bool IsInventoryFull(ulong clientId, out string msg)
     {
         // TODO: 실제 인벤토리 조회로 교체
         msg = string.Empty;
         return false; 
     }

     private bool IsDuplicateRestrictedAndOwned(CardItemData card, ulong clientId, out string msg)
     {
         // TODO: 카드 메타데이터에 중복 제한 플래그가 있다면 확인
         msg = string.Empty;
         return false; 
     }*/

    #region 카드 생성 확률 로직
    /*private const float BaseCommon = 0.8f;
    private const float BaseRare = 0.2f;
    private const float Delta = 0.3f; // 30%

    // TODO: 실제 카드DB와 연결되면 이 풀은 DB에서 가져오기.
    private static readonly int[] _fallbackCommonPool = { 10000, 20000, 30000 };
    private static readonly int[] _fallbackRarePool = { 10100, 20200 };

    private readonly System.Random _rng = new System.Random();

    // TODO: 실제 전체 인원/사망 인원은 Game/Match 매니저에서 받아오기.
    // 지금은 샘플로 0명 사망 가정(=초기 확률 유지).
    private float GetDeathRatio()
    {
        return 0f;
    }

    private (float common, float rare) ComputeRarityWeights()
    {
        var r = Mathf.Clamp01(GetDeathRatio()); // 0~1
        var rare = Mathf.Clamp(BaseRare + Delta * r, 0.5f, 0.5f);     // 0.2→최대 0.5
        var common = Mathf.Clamp(BaseCommon - Delta * r, 0.5f, 0.5f); // 0.8→최소 0.5
        return (common, rare);
    }

    private Rarity RollRarity((float common, float rare) w)
    {
        var t = _rng.NextDouble();
        return (t < w.rare) ? Rarity.Rare : Rarity.Common;
    }

    private int GetRandomCardIdByRarity(Rarity rarity)
    {
        // TODO: CardDB.GetRandomId(rarity)로 교체
        if (rarity == Rarity.Rare)
            return _fallbackRarePool[_rng.Next(_fallbackRarePool.Length)];
        return _fallbackCommonPool[_rng.Next(_fallbackCommonPool.Length)];
    }

    private List<int> RollShopCardIds(int count)
    {
        var w = ComputeRarityWeights();
        var ids = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            var r = RollRarity(w);
            ids.Add(GetRandomCardIdByRarity(r));
        }
        return ids;
    }*/
    #endregion

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
        for (int i = 0; i < cardForSaleParentTransform.childCount; i++)
        {
            Transform card = cardForSaleParentTransform.GetChild(i);
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
