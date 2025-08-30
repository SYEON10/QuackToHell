// PlayerModel.cs

using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 로직 관리
/// </summary>
public class PlayerModel : NetworkBehaviour
{
    private void Awake()
    {
        playerRB = gameObject.GetComponent<Rigidbody2D>();
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
        Vector2 direction = new Vector2(inputXDirection, inputYDirection).normalized;
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


    private NetworkVariable<PlayerStateData> _playerStateData = new NetworkVariable<PlayerStateData>(writePerm: NetworkVariableWritePermission.Server);
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

    // 상태 컴포넌트들 (미리 생성)
    private PlayerIdleState idleStateComponent;
    private PlayerWalkState walkStateComponent;
    private PlayerDeadState deadStateComponent;
    private PlayerAliveState aliveStateComponent;

    private State preAliveState;
    private State tempAliveState;
    private State curAliveState;
    private State preAnimationState;
    private State tempAnimationState;
    private State curAnimationState;

    private void Start()
    {
        // 미리 부착된 컴포넌트들 참조
        idleStateComponent = GetComponent<PlayerIdleState>();
        walkStateComponent = GetComponent<PlayerWalkState>();
        aliveStateComponent = GetComponent<PlayerAliveState>();
        deadStateComponent = GetComponent<PlayerDeadState>();

        // 초기 상태 설정
        SetStateByPlayerStateEnum(PlayerStateData.Value.aliveState, PlayerStateData.Value.animationState);
        ApplyStateChange();

        // 상태 변경 이벤트 등록
        PlayerStateData.OnValueChanged += (oldValue, newValue) =>
        {
            SetStateByPlayerStateEnum(newValue.aliveState, newValue.animationState);
            ApplyStateChange();
        };
    }

    private void SetStateByPlayerStateEnum(PlayerLivingState inputPlayerAliveState = PlayerLivingState.Alive, PlayerAnimationState inputPlayerAnimationState = PlayerAnimationState.Idle)
    {
        switch (inputPlayerAliveState)
        {
            case PlayerLivingState.Alive:
                SetAliveState(aliveStateComponent);
                break;
            case PlayerLivingState.Dead:
                SetAliveState(deadStateComponent);
                break;
            default:
                break;
        }
        switch (inputPlayerAnimationState)
        {
            case PlayerAnimationState.Idle:
                SetAnimationState(idleStateComponent);
                break;
            case PlayerAnimationState.Walk:
                SetAnimationState(walkStateComponent);
                break;
        }
    }

    private void SetAliveState(State state)
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

    private void SetAnimationState(State state)
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


    private void ApplyStateChange()
    {
        // 이전 AliveState의 Exit 호출
        if (preAliveState != null)
        {
            preAliveState.OnStateExit();
        }

        // 이전 AnimationState의 Exit 호출
        if (preAnimationState != null)
        {
            preAnimationState.OnStateExit();
        }

        // 현재 AliveState의 Enter 호출
        if (curAliveState != null)
        {
            curAliveState.OnStateEnter();
        }

        // 현재 AnimationState의 Enter 호출
        if (curAnimationState != null)
        {
            curAnimationState.OnStateEnter();
        }
    }
    #endregion


    
}