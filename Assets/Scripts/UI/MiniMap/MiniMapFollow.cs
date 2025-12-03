using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class MinimapFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float zOffset = -10f; // 2D 카메라 Z 위치

    // MiniMapUI에서 쓰던 태그 배열 그대로 사용
    [SerializeField]
    private string[] playerTags = { "Player", "PlayerGhost" };

    private void OnEnable()
    {
        // 씬에 들어올 때마다 로컬 플레이어를 다시 찾기
        StartCoroutine(FindLocalPlayerRoutine());
    }

    private IEnumerator FindLocalPlayerRoutine()
    {
        // 네트워크 매니저 준비될 때까지 기다리기
        while (NetworkManager.Singleton == null)
            yield return null;

        while (target == null)
        {
            // 1) LocalClient.PlayerObject 먼저 시도
            var localPlayerObj = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayerObj != null && localPlayerObj.gameObject.activeInHierarchy)
            {
                target = localPlayerObj.transform;
                Debug.Log($"[Minimap] Target set by LocalClient.PlayerObject: {target.name}");
                yield break;
            }

            // 2) 안 되면 태그 기반으로 "IsOwner == true"인 오브젝트 찾기
            target = FindOwnerByTags();
            if (target != null)
            {
                Debug.Log($"[Minimap] Target set by FindOwnerByTags: {target.name}");
                yield break;
            }

            // 아직 못 찾았으면 다음 프레임에 다시 시도
            yield return null;
        }
    }

    private Transform FindOwnerByTags()
    {
        foreach (var tag in playerTags)
        {
            var candidates = GameObject.FindGameObjectsWithTag(tag);
            foreach (var go in candidates)
            {
                var no = go.GetComponent<NetworkObject>();
                if (no != null && no.IsOwner && go.activeInHierarchy)
                    return go.transform;
            }
        }
        return null;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 2D: X,Y는 플레이어, Z는 고정
        Vector3 pos = target.position;
        pos.z = zOffset;
        transform.position = pos;
    }
}
