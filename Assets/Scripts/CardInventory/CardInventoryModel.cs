using UnityEngine;
using Unity.Netcode;


public enum InventorySotringOption
{
    RecentlyAcquired,
}

public class CardInventoryModel : NetworkBehaviour
{
    #region 데이터
    // 로컬 클라이언트의 인벤토리가 소유하는 카드 정보
    private NetworkList<CardItemData> ownedCards = new NetworkList<CardItemData>();
    public NetworkList<CardItemData> OwnedCards => ownedCards;
    const int maxCardCount = 20;
    private ulong myClientId;
    private InventorySotringOption _sortingOption = InventorySotringOption.RecentlyAcquired;
    public InventorySotringOption SortingOption => _sortingOption;
    // TODO: 필요하면, cardCount 추가



    #endregion

    #region 초기화
    private void Start()
    {
        myClientId = NetworkManager.Singleton.LocalClientId;
    }
    #endregion

    #region InventoryCard 데이터 추가, 삭제 메서드
    [ServerRpc]
    public void AddOwnedCardServerRpc(CardItemData card)
    {  
        if (ownedCards.Count >= maxCardCount)
        {
            Debug.Log("카드 추가 실패: 인벤토리 가득 참");
            return;
        }
        ownedCards.Add(card);
        Debug.Log($"[CardInventoryModel] 카드 추가 성공: {card.cardIdKey}");
    }

    [ServerRpc]
    public void RemoveOwnedCardServerRpc(CardItemData card)
    {
        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (ownedCards[i].cardItemStatusData.cardItemID == card.cardItemStatusData.cardItemID)
            {
                ownedCards.RemoveAt(i);
                break;
            }
        }
        Debug.Log($"[CardInventoryModel] 카드 삭제 성공: {card.cardItemStatusData.cardItemID}");
    }
    #endregion

    #region 정렬
    //TODO: 정렬 버튼 생길 시 옵션에 따른 정렬 메서드 추가
    
    /*public void SortCardsByAcquiredTicks()
    {

        // NetworkList는 직접 정렬할 수 없으므로, 임시 리스트로 정렬 후 다시 추가
        var sortedList = new List<CardItemData>();
        foreach (var card in ownedCards)
        {
            sortedList.Add(card);
        }
        
        sortedList.Sort((a, b) => b.AcquiredTicks.CompareTo(a.AcquiredTicks));
        
        // NetworkList 업데이트
        ownedCards.Clear();
        foreach (var c in sortedList)
        {
            ownedCards.Add(c);
        }
    }*/
    
    #endregion
}
