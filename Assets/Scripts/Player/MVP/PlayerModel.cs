// PlayerModel.cs
using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// 로직 관리
/// </summary>
public class PlayerModel : NetworkBehaviour
{

    [Header("Network Synchronized Data")]
    private NetworkVariable<FixedString64Bytes> _playerTag = new NetworkVariable<FixedString64Bytes>(
        GameTags.Player, // 기본값
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> PlayerTag => _playerTag;

    private PlayerIdleState idleStateComponent;
    private PlayerWalkState walkStateComponent;
    private PlayerDeadState deadStateComponent;
    private PlayerAliveState aliveStateComponent;

    private ulong clientId;

    public ulong ClientId
    {
        get { return clientId; }
        set
        {
            if (!IsHost) return;
            clientId = value;
        }
    }

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
        Vector2 direction = new Vector2(inputXDirection, inputYDirection).normalized;
        playerRB.linearVelocity = direction * PlayerStatusData.Value.moveSpeed;
        if (inputXDirection != 0 || inputYDirection != 0)
        {
            PlayerStateData newStateData = PlayerStateData.Value;
            newStateData.animationState = PlayerAnimationState.Walk;
            PlayerStateData.Value = newStateData;
        }
        else
        {
            PlayerStateData newStateData = PlayerStateData.Value;
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

    private NetworkStateBase preAliveState;
    private NetworkStateBase tempAliveState;
    private NetworkStateBase curAliveState;
    private NetworkStateBase preAnimationState;
    private NetworkStateBase tempAnimationState;
    private NetworkStateBase curAnimationState;

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

    private void SetAliveState(NetworkStateBase state)
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

    private void SetAnimationState(NetworkStateBase state)
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
    public void ChangeColorServerRpc(Int32 colorIndex, ulong clientId, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized color change attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        PlayerAppearanceData tempAppearanceData = _playerAppearanceData.Value;
        tempAppearanceData.ColorIndex = colorIndex;
        _playerAppearanceData.Value = tempAppearanceData;
    }
    #endregion

    #region Die
    /// <summary>
    /// 플레이어 사망 처리 (서버에서만 호출되어야 함)
    /// 클라이언트는 이 메서드를 직접 호출할 수 없음
    /// </summary>
    [ServerRpc]
    public void DieServerRpc(ServerRpcParams rpcParams = default)
    {
        // 서버에서만 상태 변경 (권위적 정보)
        PlayerStateData newStateData = PlayerStateData.Value;
        newStateData.AliveState = PlayerLivingState.Dead;
        PlayerStateData.Value = newStateData;
    }
    #endregion

    #region 역할 변경
    public void ChangeRole(PlayerJob newRole){
        PlayerStatusData newStatusData = PlayerStatusData.Value;
        newStatusData.job = newRole;
        PlayerStatusData.Value = newStatusData;
    }
    #endregion

}