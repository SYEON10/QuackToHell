using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 역할별 행동 전략 인터페이스
/// State 패턴과 유사하게 의존성 주입 방식으로 관리
/// </summary>
public interface IRoleStrategy
{
    /// <summary>
    /// 역할별 초기 설정 (Action Map, UI 등)
    /// </summary>
    void Setup();
    
    /// <summary>
    /// 역할별 입력 처리
    /// </summary>
    void HandleInput(InputAction.CallbackContext context);
    
    /// <summary>
    /// 역할별 업데이트 로직
    /// </summary>
    void Update();
    
    /// <summary>
    /// 역할별 정리 작업
    /// </summary>
    void Cleanup();
    
    // Ability 메서드들 (모든 역할이 구현해야 함)
    void TryKill();
    void TrySabotage();
    void TryInteract();
    void TryReportCorpse();
    bool CanKill();
    bool CanSabotage();
    bool CanInteract();
    bool CanReportCorpse();
}