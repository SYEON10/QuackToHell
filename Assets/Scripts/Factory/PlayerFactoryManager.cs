using UnityEngine;
using Unity.Netcode;

/// <summary>
/// 플레이어 생성 담당
/// </summary>

public class PlayerFactoryManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    
    
    private Transform _playerSpawnPoint;
    private void Start()
    {
        transform.position = new Vector3(0, 0, 0);
        _playerSpawnPoint = transform;
    }


    [ServerRpc(RequireOwnership = false)]
    public void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!DebugUtils.AssertNotNull(playerPrefab, "playerPrefab", this))
            return;
            
        GameObject player = Instantiate(playerPrefab, _playerSpawnPoint);
        PlayerModel playerModel = player.GetComponent<PlayerModel>();
        if (!DebugUtils.AssertNotNull(playerModel, "PlayerModel", this))
            return;
            
        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        if (!DebugUtils.AssertNotNull(networkObject, "NetworkObject", this))
            return;
            
        networkObject.SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        PlayerStatusData myPlayerStateData = playerModel.PlayerStatusData.Value;
        string baseNickname = myPlayerStateData.Nickname.Split('_')[0];
        myPlayerStateData.Nickname = $"{baseNickname}_{rpcParams.Receive.SenderClientId}";
        
        myPlayerStateData.job = PlayerJob.Farmer;
        
        player.name = myPlayerStateData.Nickname;
        playerModel.PlayerStatusData.Value = myPlayerStateData;

        playerModel.PlayerAppearanceData.Value = new PlayerAppearanceData
        {
            ColorIndex = 0
        };

        playerModel.PlayerStateData.Value = new PlayerStateData
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