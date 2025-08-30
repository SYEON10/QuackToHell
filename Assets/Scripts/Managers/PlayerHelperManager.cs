using UnityEngine;

/// <summary>
/// 플레이어 관련 헬퍼메서드 제공하는 매니저
/// 
/// 책임:
/// - 플레이어 검색 및 조회 (클라이언트 ID로 플레이어 찾기)
/// - 플레이어 데이터 접근 헬퍼 (골드, 상태 등)
/// 
/// 주의: 실제 플레이어 데이터 수정은 하지 않음 (읽기 전용 헬퍼)
/// </summary>
public class PlayerHelperManager : MonoBehaviour
{
    #region 싱글톤
    public static PlayerHelperManager Instance => SingletonHelper<PlayerHelperManager>.Instance;

    private void Awake()
    {
        SingletonHelper<PlayerHelperManager>.InitializeSingleton(this);
    }
    #endregion

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 골드를 반환
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 골드, 플레이어를 찾지 못하면 0</returns>
    public int GetPlayerGoldByClientId(ulong clientId)
    {
        // 씬에서 모든 플레이어를 찾기
        PlayerModel[] allPlayers = FindObjectsOfType<PlayerModel>();
        
        foreach (PlayerModel player in allPlayers)
        {
            // NetworkObject의 OwnerClientId와 비교
            if (player.NetworkObject != null && player.NetworkObject.OwnerClientId == clientId)
            {
                // 해당 플레이어의 골드 반환
                return player.PlayerStatusData.Value.gold;
            }
        }
        
        // 플레이어를 찾지 못한 경우
        Debug.LogWarning($"Player with ClientId {clientId} not found in scene");
        return 0;
    }

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 PlayerModel 반환
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 PlayerModel, 찾지 못하면 null</returns>
    public PlayerModel GetPlayerByClientId(ulong clientId)
    {
        PlayerModel[] allPlayers = FindObjectsOfType<PlayerModel>();
        
        foreach (PlayerModel player in allPlayers)
        {
            if (player.NetworkObject != null && player.NetworkObject.OwnerClientId == clientId)
            {
                return player;
            }
        }
        
        Debug.LogWarning($"Player with ClientId {clientId} not found in scene");
        return null;
    }

    /// <summary>
    /// 클라이언트 ID로 플레이어를 찾아서 플레이어 게임 오브젝트 반환
    /// </summary>
    /// <param name="clientId">찾을 플레이어의 클라이언트 ID</param>
    /// <returns>플레이어의 PlayerModel, 찾지 못하면 null</returns>
    public GameObject GetPlayerGameObjectByClientId(ulong clientId)
    {
        PlayerModel[] allPlayers = FindObjectsOfType<PlayerModel>();
        
        foreach (PlayerModel player in allPlayers)
        {
            if (player.NetworkObject != null && player.NetworkObject.OwnerClientId == clientId)
            {
                return player.gameObject;
            }
        }
        
        Debug.LogWarning($"Player with ClientId {clientId} not found in scene");
        return null;
    }

    /// <summary>
    /// 현재 씬의 모든 플레이어 수 반환
    /// </summary>
    /// <returns>플레이어 수</returns>
    public int GetPlayerCount()
    {
        return FindObjectsOfType<PlayerModel>().Length;
    }
}
