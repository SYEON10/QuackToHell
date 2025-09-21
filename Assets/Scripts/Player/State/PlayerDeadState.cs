using UnityEngine;

public class PlayerDeadState : NetworkStateBase
{
    [SerializeField]
    private SpriteRenderer[] spriteRenderers;
    [SerializeField]
    private PlayerModel playerModel;
    const float MOVE_SPEED_MULTIPLIER = 1.5f;

    public override void OnStateEnter()
    {
        // PlayerPresenter.ChangeToGhostVisualState()에서 처리하므로 제거
        // 투명화, 태그 변경, 레이어 변경, 가시성 업데이트는 모두 PlayerPresenter에서 처리
        
        // 이 State는 더 이상 사용하지 않음
        // 모든 유령 관련 처리는 PlayerPresenter에서 통합 관리
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }
}
