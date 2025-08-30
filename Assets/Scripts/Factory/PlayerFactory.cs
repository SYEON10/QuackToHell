using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 생성 담당
/// </summary>

public class PlayerFactoryManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    
    // 상수 정의
    private const int DEFAULT_GOLD = 100000;
    private const float DEFAULT_MOVE_SPEED = 10f;
    
    private Transform _playerSpawnPoint;
    private void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        _playerSpawnPoint = transform;
    }


    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(string inputNickName = "Player_", PlayerJob inputPlayerJob = PlayerJob.None, ServerRpcParams rpcParams = default)
    {
        var player = Instantiate(playerPrefab, _playerSpawnPoint);
        player.name = $"{inputNickName}{rpcParams.Receive.SenderClientId}";
        player.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);
        PlayerStatusData playerStatusData = new PlayerStatusData
        {
            nickname = inputNickName + rpcParams.Receive.SenderClientId,
            job = inputPlayerJob,
            credibility = PlayerStatusData.MaxCredibility,
            spellpower = PlayerStatusData.MaxSpellpower,
            gold = DEFAULT_GOLD,
            moveSpeed = DEFAULT_MOVE_SPEED
        };
        player.GetComponent<PlayerModel>().PlayerStatusData.Value = playerStatusData;

        player.GetComponent<PlayerModel>().PlayerStateData.Value = new PlayerStateData
        {
            aliveState = PlayerLivingState.Alive,
            animationState = PlayerAnimationState.Idle
        };

        DontDestroyOnLoad(player);
    }




    #region 싱글톤
    public static PlayerFactoryManager Instance => SingletonHelper<PlayerFactoryManager>.Instance;

    private void Awake()
    {
        SingletonHelper<PlayerFactoryManager>.InitializeSingleton(this);
    }
    #endregion

}