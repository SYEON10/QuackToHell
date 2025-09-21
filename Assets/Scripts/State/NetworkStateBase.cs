using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 네트워크 동기화가 필요한 상태들을 위한 추상클래스
/// Player 상태 등에서 사용
/// </summary>
public abstract class NetworkStateBase : NetworkBehaviour
{
    public abstract void OnStateEnter();
    public abstract void OnStateUpdate();
    public abstract void OnStateExit();
}