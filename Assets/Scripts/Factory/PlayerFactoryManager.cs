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
    public void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        // 스폰 (리플리케이트)
        var player = Instantiate(playerPrefab, _playerSpawnPoint);
        PlayerModel playerModel= player.GetComponent<PlayerModel>();
        player.GetComponent<NetworkObject>().SpawnWithOwnership(rpcParams.Receive.SenderClientId);

        // 서버에서만 값 세팅
        // 속성: NetworkVariable로 관리 (리플리케이트)
        // 인스펙터에서 설정한대로 초기화됨
        PlayerStatusData myPlayerStateData = playerModel.PlayerStatusData.Value;
        // 닉네임만 새로 설정 (기존 닉네임에서 숫자 부분 제거 후 새 클라이언트 ID 붙이기)
        string baseNickname = myPlayerStateData.Nickname.Split('_')[0]; // "Player" 부분만 추출
        myPlayerStateData.Nickname = $"{baseNickname}_{rpcParams.Receive.SenderClientId}";
        // 게임오브젝트의 이름을 닉네임으로 변경
        player.name = myPlayerStateData.Nickname;
        // state 데이터 주입
        playerModel.PlayerStatusData.Value = myPlayerStateData;

        // appearance 데이터 초기화
        playerModel.PlayerAppearanceData.Value = new PlayerAppearanceData
        {
            ColorIndex = 0 // 기본 색상 (Red)
        };

        // 상태 주입 (모두에게 명령)
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