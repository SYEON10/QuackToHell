// KillAbility.cs
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class KillAbility : AbilityBase
{
    [SerializeField]
    [Tooltip("살해 범위를 표시하는 CircleCollider2D")]
    private CircleCollider2D killRangeCollider;
    private List<CapsuleCollider2D> killablePlayerColliders = new List<CapsuleCollider2D>();
    
    // 가장 가까운 대상 찾기용 변수들
    private PlayerModel closestTarget;
    private float closestDistance;
    private Vector2 center;

    protected override void Awake()
    {
        base.Awake();
        
        // CircleCollider2D 자동 찾기 및 설정
        if (killRangeCollider == null)
        {
            killRangeCollider = GetComponent<CircleCollider2D>();
        }
        
        if (killRangeCollider != null)
        {
            killRangeCollider.isTrigger = true;
        }
    }
    
    // PlayerView에서 살해 버튼을 눌렀을 때 이 메서드를 호출
    public override void Activate()
    {
        // 클라이언트에서 사용 가능 여부 1차 확인 (UI 등 빠른 피드백용)
        if (!IsReady())
        {
            Debug.Log("Kill Ability is on cooldown.");
            return;
        }

        // 클라이언트에서 살해 대상을 찾는 로직
        PlayerModel target = FindKillableTarget();

        if (target != null)
        {
            Debug.Log($"Found killable target: {target.PlayerStatusData.Value.Nickname}");
            // 서버에 살해 시도 RPC 전송
            CmdAttemptKillServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId);
        }
        else
        {
            Debug.Log("No killable target found in range.");
        }
    }






    // Fix for CS0230: Ensure the foreach loop specifies both the type and identifier for the iteration variable.
    private PlayerModel FindKillableTarget()
    {
        //TODO: 로컬 말고 원형 중 가장 가까운 상대 플레이어로
        return gameObject.GetComponent<PlayerModel>();
    }
    
    /// <summary>
    /// 대상이 살해 가능한지 확인합니다.
    /// </summary>
    /// <param name="target">살해 대상으로 확인할 PlayerModel</param>
    /// <returns>살해 가능하면 true, 그렇지 않으면 false</returns>
    private bool IsKillableTarget(PlayerModel target)
    {
        // 대상이 null이면 살해 불가능
        if (target == null) return false;
        
        // 대상이 살아있는지 확인 (PlayerLivingState.Alive인지 체크)
        if (target.PlayerStateData.Value.AliveState != PlayerLivingState.Alive) return false;
        
        // 자기 자신은 살해 불가능
        if (target == Owner) return false;
        
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdAttemptKillServerRpc(ulong victimNetworkObjectId)
    {
        // 서버에서 쿨타임 최종 확인 (필수)
        if (!IsReady()) return;

        // 대상 찾기 및 유효성 검증 로직
        NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(victimNetworkObjectId, out var victimObject);
        if (victimObject != null)
        {
            var victimModel = victimObject.GetComponent<PlayerModel>();

            // 살해 로직 실행
            victimModel.Die();

            // ★★★ 부모의 쿨타임 시작 메서드 호출! ★★★
            StartCooldown();
        }
    }
}