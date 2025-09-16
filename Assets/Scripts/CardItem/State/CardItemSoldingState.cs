using UnityEngine;

public class CardItemSoldingState : StateBase
{
    public override void OnStateEnter()
    {
        Debug.Log("[CardItemSoldingState] OnStateEnter 호출 - 카드가 진열 상태가 되었습니다.");
    }

    public override void OnStateUpdate()
    {
        // 진열 상태에서 필요한 업데이트 로직
    }

    public override void OnStateExit()
    {
        Debug.Log("[CardItemSoldingState] OnStateExit 호출 - 카드가 진열 상태에서 벗어났습니다.");
    }
}
