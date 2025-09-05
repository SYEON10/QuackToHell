using UnityEngine;

public class CardItemSoldState : StateBase
{
    public override void OnStateEnter()
    {
        Debug.Log("[CardItemSoldState] OnStateEnter 호출");
        if (this.gameObject.transform.parent.CompareTag("CardForSale"))
        {
            Debug.Log("[CardItemSoldState] 팔려서 본인 파괴");
            Destroy(gameObject);
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {
        
    }
}
