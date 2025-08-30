using Unity.Netcode;
using static PlayerView;
using System;

/// <summary>
/// view, model 간 중재자
/// </summary>
public class PlayerPresenter : NetworkBehaviour
{
    private PlayerModel playerModel;
    private PlayerView playerView;

    // Start에서 세팅
    private void Start()
    {
        // PlayerModel, PlayerView를 컴포넌트에서 가져옴
        playerModel = GetComponent<PlayerModel>();
        playerView = GetComponent<PlayerView>();

        playerView.OnMovementInput += PlayerView_OnMovementInput;

        //닉네임
        // 초기값 설정
        playerView.UpdateNickname(playerModel.PlayerStatusData.Value.nickname);
        // PlayerStatusData 전체의 OnValueChanged 이벤트 구독
        if (playerModel != null && playerModel.PlayerStatusData != null)
        {
            // PlayerStatusData 변경 시 닉네임 업데이트
            playerModel.PlayerStatusData.OnValueChanged += (previousValue, newValue) =>
            {
                playerView.UpdateNickname(newValue.nickname);
            };
        }
    }



    private void PlayerView_OnMovementInput(object sender, EventArgs e)
    {
        //이벤트 인자 캐스팅
        OnMovementInputEventArgs onMovementInputEventArgs = (OnMovementInputEventArgs)e;

        //model에게 방향 이벤트 전달
        playerModel.MovePlayerServerRpc(onMovementInputEventArgs.XDirection, onMovementInputEventArgs.YDirection);
    }
}
