using UnityEngine;
using Unity.Netcode;

// Vent, Minigame 등 IInteractable 구현체에 붙여서 사용
[DisallowMultipleComponent]
public sealed class InteractionHighlighterPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject highlightObject;     // 하이라이트 오브젝트(자식)
    [SerializeField] private MonoBehaviour interactable;     // (옵션) 직접 할당. 비우면 GetComponent로 찾음
    [SerializeField] private Transform playerTransform;     // 플레이어 Transform 참조

    // 내부 캐시
    private IInteractable _ia;
    private Transform _playerTr;

    void Awake()
    {
        if (interactable == null) interactable = GetComponent<MonoBehaviour>();
        _ia = interactable as IInteractable;

        // 처음엔 비어있을 수 있음 → Update에서 재탐색
        TryFindLocalPlayer();
        SafeSet(false);
    }

    void Update()
    {
        // 1) 로컬 플레이어/인터랙터가 비었으면 계속 재탐색
        if (_playerTr == null) TryFindLocalPlayer();
        if (_ia == null)
        {
            if (interactable == null) interactable = GetComponent<MonoBehaviour>();
            _ia = interactable as IInteractable;
        }

        // 2) 조건 체크 불가면 끔
        if (_playerTr == null || _ia == null || highlightObject == null)
        {
            SafeSet(false);
            return;
        }

        // 3) CanInteract로만 on/off 결정 (거리/점유판정은 IInteractable이 처리)
        bool on = false;
        try
        {
            on = _ia.CanInteract(_playerTr.gameObject);
        }
        catch { on = false; }

        SafeSet(on);
    }

    private void SafeSet(bool v)
    {
        if (highlightObject && highlightObject.activeSelf != v)
            highlightObject.SetActive(v);
    }

    private void TryFindLocalPlayer()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsClient)
        {
            NetworkObject playerObject = nm.SpawnManager?.GetLocalPlayerObject();
            if (playerObject != null) { _playerTr = playerObject.transform; return; }

            // Fallback: 내 소유 & "Player" 태그
            foreach (NetworkObject networkObject in nm.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject != null && networkObject.OwnerClientId == nm.LocalClientId && networkObject.CompareTag("Player"))
                {
                    _playerTr = networkObject.transform;
                    return;
                }
            }
        }

        // 마지막 Fallback: SerializeField로 할당된 플레이어 Transform 사용
        if (_playerTr == null && playerTransform != null)
        {
            _playerTr = playerTransform;
        }
    }
}
