using UnityEngine;

/// <summary>
/// Idle상태일 때 하는 행동 정의
/// </summary>
public class PlayerIdleState : StateBase
{
    private Animator animator;
    
    private void Start()
    {
        animator = gameObject.transform.Find("Body").gameObject.GetComponent<Animator>();
    }
    
    public override void OnStateEnter()
    {
        TriggerIdleAnimation();
    }
    
    // 트리거 방식으로 애니메이션 제어
    public void TriggerIdleAnimation()
    {
        if (animator == null)
        {
            // animator가 null이면 다시 찾기
            animator = gameObject.transform.Find("Body").gameObject.GetComponent<Animator>();
        }
        
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
        }
        else
        {
            Debug.LogError("PlayerIdleState: Animator not found!");
        }
    }

    public override void OnStateExit()
    {

    }

    public override void OnStateUpdate()
    {

    }
}
