using UnityEngine;

public class PlayerDeadState : StateBase
{
    [SerializeField]
    private SpriteRenderer[] spriteRenderers;
    [SerializeField]
    private PlayerModel playerModel;
    const float MOVE_SPEED_MULTIPLIER = 1.5f;

    public override void OnStateEnter()
    {
        //1. 스프라이트 반투명
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color color = spriteRenderers[i].color;
            color.a = 0.5f; // 반투명 설정
            spriteRenderers[i].color = color;
        }
        //2. 스피드
        if (IsHost)
        {
            PlayerStatusData tempPlayerStatusData = playerModel.PlayerStatusData.Value;
            tempPlayerStatusData.MoveSpeed *= MOVE_SPEED_MULTIPLIER;
            playerModel.PlayerStatusData.Value = tempPlayerStatusData;
        }

        //3. 태그변경
        gameObject.tag = "PlayerGhost";
        //4. 레이어 변경
        gameObject.layer = LayerMask.NameToLayer("PlayerGhost");
        //5. 죽은애들끼리만 보이게
        if (IsHost)
        {
            //멀티캐스트
            playerModel.SetPlayerVisibilityForDeadPlayersClientRpc(OwnerClientId);
        }
    }

    public override void OnStateExit()
    {
    }

    public override void OnStateUpdate()
    {
    }
}
