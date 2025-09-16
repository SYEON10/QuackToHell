using UnityEngine;

public class CardItemSoldState : StateBase
{
    public override void OnStateEnter()
    {
        Debug.Log("[CardItemSoldState] OnStateEnter 호출");
        Debug.Log($"[CardItemSoldState] GameObject: {gameObject.name}");
        Debug.Log($"[CardItemSoldState] Parent: {gameObject.transform.parent?.name}");
        Debug.Log($"[CardItemSoldState] Parent Tag: {gameObject.transform.parent?.tag}");
        
        // 카드가 상점에서 진열된 상태인지 확인
        if (this.gameObject.transform.parent != null && 
            this.gameObject.transform.parent.CompareTag("CardForSale"))
        {
            Debug.Log("[CardItemSoldState] 팔려서 본인 파괴");
            // 약간의 지연을 두고 파괴 (UI 업데이트 완료 후)
            Destroy(gameObject, 0.1f);
        }
        else
        {
            Debug.LogWarning("[CardItemSoldState] 파괴 조건을 만족하지 않음 - Parent가 null이거나 CardForSale 태그가 아님");
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {
        
    }
}
