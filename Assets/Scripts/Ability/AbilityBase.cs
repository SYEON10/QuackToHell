using Unity.Netcode;
using UnityEngine;



/// <summary>
/// 모든 어빌리티가 상속받을 부모 클래스입니다. 어빌리티의 공통 기능인 쿨타임 관리 로직이 포함되어 있습니다.
/// 모든 어빌리티의 기반이 될 추상 클래스
///  NetworkBehaviour를 상속받아 RPC 등을 사용할 수 있도록 함
/// </summary>
public abstract class AbilityBase : NetworkBehaviour
{
    [Header("Ability Settings")]
    [SerializeField]
    [Tooltip("어빌리티의 쿨타임 (초)")]
    private float _cooldown = 5f;

    // 현재 쿨타임을 동기화하기 위한 NetworkVariable
    private NetworkVariable<float> _cooldownTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 어빌리티의 소유자 (이 어빌리티를 사용하는 플레이어)
    protected PlayerModel Owner { get; private set; }

    protected virtual void Awake()
    {
        // 컴포넌트가 부착된 게임오브젝트에서 PlayerModel을 찾아 Owner로 설정
        Owner = GetComponent<PlayerModel>();
    }

    public override void OnNetworkSpawn()
    {
        // 쿨타임 타이머 UI 업데이트 등을 위해 값 변경 시 이벤트 구독 (클라이언트에서)
        if (IsClient)
        {
            _cooldownTimer.OnValueChanged += (prev, current) =>
            {
                // TODO: 쿨타임 UI 업데이트 로직 연결
            };
        }
    }

    private void Update()
    {
        // 서버에서만 쿨타임 감소 로직을 실행
        if (IsServer && _cooldownTimer.Value > 0)
        {
            _cooldownTimer.Value -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 어빌리티가 현재 사용 가능한지 확인합니다.
    /// </summary>
    public bool IsReady()
    {
        return _cooldownTimer.Value <= 0;
    }

    /// <summary>
    /// 서버에서 쿨타임을 설정합니다. 어빌리티를 사용한 직후 호출해야 합니다.
    /// </summary>
    protected void StartCooldown()
    {
        if (IsServer)
        {
            _cooldownTimer.Value = _cooldown;
        }
    }

    /// <summary>
    /// 이 어빌리티의 핵심 동작입니다. 자식 클래스에서 반드시 구현해야 합니다.
    /// </summary>
    public abstract void Activate();
}