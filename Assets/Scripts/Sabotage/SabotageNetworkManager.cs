using Unity.Netcode;
using UnityEngine;

public class SabotageNetworkManager : NetworkBehaviour
{
    public static SabotageNetworkManager Instance;

    [Header("시야 연출 컨트롤러 (씬에 1개)")]
    public SabotageVisualController visualController;

    [Header("사보타지 유지 시간")]
    public float sabotageDuration = 8f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 버튼에서 이 함수만 호출하면 됨.
    /// 로컬에서 서버로 요청 → 서버가 모든 클라이언트에 브로드캐스트.
    /// </summary>
    public void TryStartSabotage()
    {
        // 호스트/서버라면 바로 처리, 클라이언트면 ServerRpc 호출
        if (IsServer)
        {
            StartSabotageServer();
        }
        else
        {
            RequestSabotageServerRpc();
        }
    }

    public void TryStartSabotageFromPlayer(GameObject player)
    {
        if (player == null) return;

        var no = player.GetComponent<NetworkObject>();
        if (no == null) return;

        if (IsServer) StartSabotageServer();
        else RequestSabotageServerRpc(no.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestSabotageServerRpc(ulong playerNetId, ServerRpcParams rpcParams = default)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out var playerObj)) return;
        if (playerObj.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        StartSabotageServer();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestSabotageServerRpc(ServerRpcParams rpcParams = default)
    {
        // TODO: 쿨타임 체크, 권한 체크 넣고 싶으면 여기서
        StartSabotageServer();
    }

    void StartSabotageServer()
    {
        // 서버도 자기 화면에서 실행
        TriggerSabotageClientRpc(sabotageDuration);
    }

    [ClientRpc]
    void TriggerSabotageClientRpc(float duration)
    {
        if (visualController != null)
        {
            visualController.PlaySabotageOnce(duration);
        }
    }
}
