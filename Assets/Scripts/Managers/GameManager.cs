using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// 게임 전체를 관리하는 중앙 매니저
/// 
/// 책임:
/// - 게임 상태 및 씬 관리 (씬 전환, 게임 시작/종료)
/// - 플레이어 골드 관리 (차감, 증가, 검증)
/// - 게임 규칙 및 밸런스 관리
/// - 전역 이벤트 및 시스템 간 조율
/// - 게임 데이터 저장/로드 관리
/// 
/// 주의: 플레이어 개별 데이터는 PlayerManager를 통해 접근
/// </summary>
public class GameManager : NetworkBehaviour
{
    #region 싱글톤
    public static GameManager Instance => SingletonHelper<GameManager>.Instance;

    private void Awake()
    {
        SingletonHelper<GameManager>.InitializeSingleton(this);
    }
    #endregion

    private void Start()
    {
        //persistent씬에서 시작해서 바로 로비씬으로 전환
        SceneManager.LoadScene(GameScenes.Lobby, LoadSceneMode.Single);
    }

    /// <summary>
    /// 서버에서 특정 클라이언트의 골드를 차감하는 RPC
    /// </summary>
    /// <param name="clientId">골드를 차감할 클라이언트 ID</param>
    /// <param name="amount">차감할 골드 양</param>
    [ServerRpc]
    public void DeductPlayerGoldServerRpc(ulong clientId, int amount, ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 서버에서 권위적 정보로 클라이언트 ID 검증
        if (clientId != requesterClientId)
        {
            Debug.LogError($"Server: Unauthorized gold deduction attempt. Requested: {clientId}, Actual: {requesterClientId}");
            return;
        }
        
        //플레이어 골드차감
        PlayerModel player = PlayerHelperManager.Instance.GetPlayerModelByClientId(clientId);
        if (!DebugUtils.AssertNotNull(player, "PlayerModel", this))
            return;
            
        PlayerStatusData currentStatus = player.PlayerStatusData.Value;
        currentStatus.gold -= amount;
        player.PlayerStatusData.Value = currentStatus;
    }
}
