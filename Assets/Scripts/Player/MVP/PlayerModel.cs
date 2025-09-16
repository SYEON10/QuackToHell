// PlayerModel.cs
using System;
using System.IO;
using System.Numerics;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 로직 관리
/// </summary>
public class PlayerModel : NetworkBehaviour
{
    private PlayerIdleState idleStateComponent;
    private PlayerWalkState walkStateComponent;
    private PlayerDeadState deadStateComponent;
    private PlayerAliveState aliveStateComponent;

    private void Awake()
    {
        playerRB = gameObject.GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // 미리 부착된 컴포넌트들 참조
        idleStateComponent = GetComponent<PlayerIdleState>();
        walkStateComponent = GetComponent<PlayerWalkState>();
        aliveStateComponent = GetComponent<PlayerAliveState>();
        deadStateComponent = GetComponent<PlayerDeadState>();

        // 초기 상태 설정
        SetAnimationStateByEnum(PlayerStateData.Value.AnimationState);
        SetAliveStateByEnum(PlayerStateData.Value.AliveState);
        ApplyAliveStateChange();
        ApplyAnimationStateChange();

        // 상태 변경 이벤트 등록 - 개별 처리
        PlayerStateData.OnValueChanged += (oldValue, newValue) =>
        {
            // AliveState가 변경된 경우만 처리
            if (oldValue.AliveState != newValue.AliveState)
            {
                SetAliveStateByEnum(newValue.AliveState);
                ApplyAliveStateChange();
            }

            // AnimationState가 변경된 경우만 처리
            if (oldValue.AnimationState != newValue.AnimationState)
            {
                SetAnimationStateByEnum(newValue.AnimationState);
                ApplyAnimationStateChange();
            }
        };
    }

    private void Update()
    {
        if (curAliveState != null)
        {
            curAliveState.OnStateUpdate();
        }
        if (curAnimationState != null)
        {
            curAnimationState.OnStateUpdate();
        }
    }

    #region 플레이어 움직임
    private Rigidbody2D playerRB;

    [Rpc(SendTo.Server)]
    public void MovePlayerServerRpc(int inputXDirection, int inputYDirection)
    {
        UnityEngine.Vector2 direction = new UnityEngine.Vector2(inputXDirection, inputYDirection).normalized;
        playerRB.linearVelocity = direction * PlayerStatusData.Value.moveSpeed;
        if (inputXDirection != 0 || inputYDirection != 0)
        {
            var newStateData = PlayerStateData.Value;
            newStateData.animationState = PlayerAnimationState.Walk;
            PlayerStateData.Value = newStateData;
        }
        else
        {
            var newStateData = PlayerStateData.Value;
            newStateData.animationState = PlayerAnimationState.Idle;
            PlayerStateData.Value = newStateData;
        }
    }
    #endregion


    #region 플레이어 데이터
    [SerializeField]
    [Header("*플레이어 상태 데이터 = 서버만 write 가능*")]
    private NetworkVariable<PlayerStatusData> _playerStatusData = new NetworkVariable<PlayerStatusData>(
        writePerm: NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerStatusData> PlayerStatusData
    {
        get { return _playerStatusData; }
        set { _playerStatusData = value; }
    }

    //플레이어 외형 데이터
    private NetworkVariable<PlayerAppearanceData> _playerAppearanceData = new NetworkVariable<PlayerAppearanceData>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerAppearanceData> PlayerAppearanceData
    {
        get { return _playerAppearanceData; }
        set { _playerAppearanceData = value; }
    }


    //상태에 따라 행동
    //상태 주입: State를 상속받은 클래스의 인스턴스를 주입받음
    private NetworkVariable<PlayerStateData> _playerStateData { get; set; } = new NetworkVariable<PlayerStateData>(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<PlayerStateData> PlayerStateData
    {
        get { return _playerStateData; }
        set
        {
            _playerStateData = value;
        }
    }
    #endregion

    #region 플레이어 상태

    private StateBase preAliveState;
    private StateBase tempAliveState;
    private StateBase curAliveState;
    private StateBase preAnimationState;
    private StateBase tempAnimationState;
    private StateBase curAnimationState;

    // AliveState만 처리하는 메서드
    private void SetAliveStateByEnum(PlayerLivingState aliveState)
    {
        switch (aliveState)
        {
            case PlayerLivingState.Alive:
                SetAliveState(aliveStateComponent);
                break;
            case PlayerLivingState.Dead:
                SetAliveState(deadStateComponent);
                break;
        }
    }

    // AnimationState만 처리하는 메서드
    private void SetAnimationStateByEnum(PlayerAnimationState animationState)
    {
        switch (animationState)
        {
            case PlayerAnimationState.Idle:
                SetAnimationState(idleStateComponent);
                break;
            case PlayerAnimationState.Walk:
                SetAnimationState(walkStateComponent);
                break;
        }
    }

    private void SetAliveState(StateBase state)
    {
        tempAliveState = curAliveState;
        curAliveState = state;
        preAliveState = tempAliveState;

        // 이전 상태 비활성화
        if (preAliveState != null)
        {
            preAliveState.enabled = false;
        }

        // 현재 상태 활성화
        if (curAliveState != null)
        {
            curAliveState.enabled = true;
        }
    }

    private void SetAnimationState(StateBase state)
    {
        tempAnimationState = curAnimationState;
        curAnimationState = state;
        preAnimationState = tempAnimationState;

        // 이전 상태 비활성화
        if (preAnimationState != null)
        {
            preAnimationState.enabled = false;
        }

        // 현재 상태 활성화
        if (curAnimationState != null)
        {
            curAnimationState.enabled = true;
        }
    }


    // AliveState 변경 적용
    private void ApplyAliveStateChange()
    {
        // 이전 AliveState의 Exit 호출
        if (preAliveState != null)
        {
            preAliveState.OnStateExit();
        }

        // 현재 AliveState의 Enter 호출
        if (curAliveState != null)
        {
            curAliveState.OnStateEnter();
        }
    }

    // AnimationState 변경 적용
    private void ApplyAnimationStateChange()
    {
        // 이전 AnimationState의 Exit 호출
        if (preAnimationState != null)
        {
            preAnimationState.OnStateExit();
        }

        // 현재 AnimationState의 Enter 호출
        if (curAnimationState != null)
        {
            curAnimationState.OnStateEnter();
        }
    }
    #endregion

    #region 색깔 변경

    [ServerRpc]
    public void ChangeColorServerRpc(Int32 colorIndex, ulong clientId)
    {
        var tempAppearanceData = _playerAppearanceData.Value;
        tempAppearanceData.ColorIndex = colorIndex;
        _playerAppearanceData.Value = tempAppearanceData;
        Debug.Log($"Color changed to {colorIndex} for client {clientId}");
    }
    #endregion

    #region Die
    public void Die()
    {
        var newStateData = PlayerStateData.Value;
        newStateData.AliveState = PlayerLivingState.Dead;
        PlayerStateData.Value = newStateData;
        Debug.Log($"Player{NetworkManager.Singleton.LocalClientId} Alive State is {PlayerStateData.Value.AliveState.ToString()}");
        CorpseFactory.Instance.CreateCorpseServerRpc(this.gameObject.transform.position, this.gameObject.transform.rotation, PlayerAppearanceData.Value);
    }

    [ClientRpc]
    public void SetPlayerVisibilityForDeadPlayersClientRpc(ulong corpseClientId)
    {
        //실행중인 애가 살아있는 상태일때만
        if (PlayerStateData.Value.AliveState == PlayerLivingState.Alive)
        {
            GameObject deadPlayer = PlayerHelperManager.Instance.GetPlayerGameObjectByClientId(corpseClientId);
            //스프라이트 찾아서, 투명화하기
            SpriteRenderer[] spriteRenderers = deadPlayer.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteRenderers[i].enabled = false;
            }
            //닉네임 찾아서, 투명화하기
            TextMeshProUGUI nicknameTMP = deadPlayer.GetComponentInChildren<TextMeshProUGUI>();
            nicknameTMP.enabled = false;
        }
    }
    #endregion
}