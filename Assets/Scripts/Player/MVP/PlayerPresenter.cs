using UnityEngine;
using Unity.Netcode;
using static PlayerView;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
/// <summary>
/// view, model 간 중재자
/// </summary>
public class PlayerPresenter : NetworkBehaviour
{
    private PlayerModel playerModel;
    private PlayerView playerView;
    //죽일 수 있는 역할일 경우에만 할당.
    private KillAbility killAbility;

    [SerializeField]
    private SpriteRenderer playerSpriteRenderer;



    // Start에서 세팅
    private void Start()
    {
        // PlayerModel, PlayerView를 컴포넌트에서 가져옴
        playerModel = GetComponent<PlayerModel>();
        playerView = GetComponent<PlayerView>();
        //TODO: 역할에따라 Ability 컴포넌트 다르게 할당 (마피아 / 시민)
        killAbility = GetComponent<KillAbility>();

        //바인딩
        playerView.OnMovementInput += PlayerView_OnMovementInput;
        playerView.OnKillTryInput += PlayerView_OnOnKillTryInput;
        playerView.OnCorpseReported += PlayerView_OnCorpseReported;

        //닉네임
        // 초기값 설정
        playerView.UpdateNickname(playerModel.PlayerStatusData.Value.Nickname);
        
        // 초기 색상 설정
        PlayerModel_OnColorChanged(playerModel.PlayerAppearanceData.Value.ColorIndex);
        // PlayerStatusData 전체의 OnValueChanged 이벤트 구독
        if (playerModel != null && playerModel.PlayerStatusData != null)
        {
            // PlayerStatusData 변경 시 닉네임 업데이트
            playerModel.PlayerStatusData.OnValueChanged += (previousValue, newValue) =>
            {
                playerView.UpdateNickname(newValue.Nickname);
            };
        }

        // PlayerAppearanceData의 ColorIndex 변경 감지
        if (playerModel != null && playerModel.PlayerAppearanceData != null)
        {
            playerModel.PlayerAppearanceData.OnValueChanged += (previousValue, newValue) =>
            {
                if (previousValue.ColorIndex != newValue.ColorIndex)
                {
                    PlayerModel_OnColorChanged(newValue.ColorIndex);
                }
            };
        }
    }

    private void PlayerView_OnCorpseReported(ulong reporterClientId)
    {
        //서버에게 처리해달라고 하기 (책임클래스는 TrialManager)
        TrialManager.Instance.TryTrialServerRpc(reporterClientId);
    }

    private void PlayerView_OnOnKillTryInput()
    {
        //TODO: 서버rpc 호출하여 Kill시도
        killAbility.Activate();
    }

    private void PlayerView_OnMovementInput(object sender, EventArgs e)
    {
        //이벤트 인자 캐스팅
        OnMovementInputEventArgs onMovementInputEventArgs = (OnMovementInputEventArgs)e;

        //model에게 방향 이벤트 전달
        playerModel.MovePlayerServerRpc(onMovementInputEventArgs.XDirection, onMovementInputEventArgs.YDirection);
    }
    
    private void PlayerModel_OnColorChanged(Int32 colorIndex)
    {
        // 모든 클라이언트에서 색상 변경 적용
        switch (colorIndex)
        {
            case 0:
                playerSpriteRenderer.color = Color.red;
                break;
            case 1:
                playerSpriteRenderer.color = Color.orange;
                break;
            case 2:
                playerSpriteRenderer.color = Color.yellow;
                break;
            case 3:
                playerSpriteRenderer.color = Color.green;
                break;
            case 4:
                playerSpriteRenderer.color = Color.blue;
                break;
            case 5:
                playerSpriteRenderer.color = Color.purple;
                break;
        }   
    }

}
