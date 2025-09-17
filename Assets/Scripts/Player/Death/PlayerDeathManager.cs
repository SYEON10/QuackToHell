using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어 사망 관련 로직을 담당하는 매니저
/// </summary>
public class PlayerDeathManager : NetworkBehaviour
{
    [Header("Death System")]
    [SerializeField] private GameObject corpsePrefab;
    
    private PlayerModel playerModel;
    private RoleManager roleManager;
    private float initialMoveSpeed;
    
    public void Initialize(PlayerModel model, RoleManager roleMgr, float moveSpeed)
    {
        playerModel = model;
        roleManager = roleMgr;
        initialMoveSpeed = moveSpeed;
    }
    
    /// <summary>
    /// 플레이어 사망 처리 (서버에서만 실행)
    /// </summary>
    [ServerRpc]
    public void HandlePlayerDeathServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        
        // 1. 서버에서 플레이어 상태 검증 (이미 죽었는지 확인)
        if (!DebugUtils.AssertNotNull(playerModel, "playerModel", this))
        {
            Debug.LogError("Server: PlayerModel not found");
            return;
        }
        
        if (playerModel.PlayerStateData.Value.AliveState == PlayerLivingState.Dead)
        {
            Debug.LogWarning($"Server: Player {OwnerClientId} is already dead");
            return;
        }
        
        // 2. 서버에서 권위적 정보로 상태 변경
        PlayerStateData currentState = playerModel.PlayerStateData.Value;
        currentState.AliveState = PlayerLivingState.Dead;
        playerModel.PlayerStateData.Value = currentState;
        
        // 3. 시체 프리팹 생성 (서버가 권위적 정보로 처리)
        if (DebugUtils.AssertNotNull(corpsePrefab, "corpsePrefab", this))
        {
            CreateCorpseServerRpc(transform.position, playerModel.PlayerAppearanceData.Value.ColorIndex);
        }
        
        // 4. 서버에서 유령 상태로 변경 (속도 포함)
        ChangeToGhostStateServerRpc();
        
        // 5. 죽은 플레이어에게만 유령 상태로 변경하라고 알림
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
        HandlePlayerDeathClientRpc(clientRpcParams);
        
        // 6. 모든 클라이언트에서 가시성 업데이트
        UpdateVisibilityForAllPlayersClientRpc();
    }
    
    /// <summary>
    /// 클라이언트에서 사망 처리 (죽은 플레이어에게만 전송)
    /// </summary>
    [ClientRpc]
    private void HandlePlayerDeathClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // 이 RPC는 죽은 플레이어에게만 전송되므로 IsOwner 체크 불필요
        // 유령 역할로 변경
        roleManager?.ChangeRole(PlayerJob.Ghost);
        
        // 유령 상태로 전환 (시각적 효과만)
        ChangeToGhostVisualState();
    }
    
    /// <summary>
    /// 유령 상태로 전환 (서버에서 속도 변경, 죽은 플레이어에게만 시각적 효과)
    /// </summary>
    [ServerRpc]
    public void ChangeToGhostStateServerRpc()
    {
        // 서버에서 속도 변경 (NetworkVariable로 동기화됨)
        if (DebugUtils.AssertNotNull(playerModel, "playerModel", this))
        {
            PlayerStatusData statusData = playerModel.PlayerStatusData.Value;
            statusData.moveSpeed = initialMoveSpeed * GameConstants.Player.GhostSpeedMultiplier; // 초기 속도의 1.3배로 설정
            playerModel.PlayerStatusData.Value = statusData;
        }
        
        // 죽은 플레이어에게만 시각적 효과 적용 (OwnerClientId로 제한)
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };
        ChangeToGhostVisualStateClientRpc(clientRpcParams);
    }
    
    /// <summary>
    /// 유령 시각적 상태로 전환 (클라이언트에서만 실행)
    /// </summary>
    [ClientRpc]
    private void ChangeToGhostVisualStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ChangeToGhostVisualState();
    }
    
    /// <summary>
    /// 유령 시각적 상태로 전환 (반투명화, 태그 변경, 레이어 변경)
    /// </summary>
    public void ChangeToGhostVisualState()
    {
        // 태그 변경
        gameObject.tag = GameTags.PlayerGhost;
        
        // 레이어 변경
        gameObject.layer = GameLayers.GetLayerIndex(GameLayers.PlayerGhost);
        
        // 반투명 효과 (간단한 방법)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (DebugUtils.AssertNotNull(spriteRenderer, "SpriteRenderer", this))
        {
            Color color = spriteRenderer.color;
            color.a = GameConstants.Player.GhostTransparency; // 50% 투명도
            spriteRenderer.color = color;
        }
        
        // 버튼 관련 코드 제거 (유령은 시체 리포트 불가)
    }
    
    /// <summary>
    /// 시체 생성 (서버 RPC)
    /// </summary>
    [ServerRpc]
    private void CreateCorpseServerRpc(Vector3 position, int colorIndex)
    {
        if (!DebugUtils.AssertNotNull(corpsePrefab, "corpsePrefab", this))
            return;
            
        GameObject corpse = Instantiate(corpsePrefab, position, Quaternion.identity);
        
        // 시체 색상 설정
        SpriteRenderer corpseRenderer = corpse.GetComponent<SpriteRenderer>();
        if (DebugUtils.AssertNotNull(corpseRenderer, "Corpse SpriteRenderer", this))
        {
            Color corpseColor = GetColorByIndex(colorIndex);
            corpseRenderer.color = corpseColor;
        }
        
        // 시체를 네트워크에 스폰
        if (corpse.TryGetComponent<NetworkObject>(out NetworkObject networkObject))
        {
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError("Corpse prefab doesn't have NetworkObject component!");
        }
    }
    
    /// <summary>
    /// 색상 인덱스에 따른 색상 반환
    /// </summary>
    private Color GetColorByIndex(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0: return Color.red;
            case 1: return Color.orange;
            case 2: return Color.yellow;
            case 3: return Color.green;
            case 4: return Color.blue;
            case 5: return Color.purple;
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 모든 플레이어의 가시성 업데이트
    /// </summary>
    [ClientRpc]
    public void UpdateVisibilityForAllPlayersClientRpc()
    {
        // 모든 플레이어의 가시성 업데이트
        PlayerView[] allPlayers = FindObjectsByType<PlayerView>(FindObjectsSortMode.None);
        foreach (PlayerView player in allPlayers)
        {
            if (DebugUtils.AssertNotNull(player, "PlayerView", this))
            {
                // TODO: UpdatePlayerVisibility 메서드 구현 필요
            }
        }
    }
}
